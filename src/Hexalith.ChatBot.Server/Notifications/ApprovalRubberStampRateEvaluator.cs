using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// Deterministic, clock-injected rubber-stamp-rate evaluation engine (Story 7.11, NFR46/FR41/NFR2). Given a tenant-bound
/// approval-decision snapshot, the tenant ref (from the authenticated binding), the correlation id, and an injected
/// "now", it computes — server-side, deterministic and pure — the share of qualifying approvals decided within
/// <see cref="RubberStampRateObservable.RubberStampLatencySeconds"/> over the
/// <see cref="RubberStampRateObservable.RollingWindow"/>, both per tenant and per <c>(tenant × reviewer)</c>, and
/// evaluates the tenant-level FR41 tuning-revisit condition.
/// <para>
/// Mirrors the Story 7.7 <see cref="EscalationPolicyEvaluator"/> / Story 7.9 <see cref="NotificationThrottleEvaluator"/>
/// discipline exactly: static, pure, no wall-clock access, deterministic given the injected clock. The rolling-window
/// math copies <see cref="NotificationThrottleEvaluator.CountInTrailingWindow"/> (<c>age &gt;= 0 &amp;&amp; age &lt;
/// window</c>, keyed on <c>DecidedAtUtc</c>, future ignored); the latency clamp copies the
/// <see cref="EscalationPolicyEvaluator"/> server-measured-duration discipline (clamp ≥ 0, future/skewed → 0). The
/// denominator is restricted to <see cref="ApprovalDecisionKind.Approve"/> decisions against
/// <see cref="AiActionRiskClass.ApprovalRequired"/> actions (rejections / revision-requests / cancellations / low-risk
/// excluded). The FR41 crossing is strictly-greater-than via <strong>exact integer arithmetic</strong> — never a
/// <see cref="double"/> compare. A zero/degenerate denominator never triggers and never divides by zero.
/// </para>
/// </summary>
internal static class ApprovalRubberStampRateEvaluator
{
    /// <summary>The safe reason code carried by a fired FR41 approval-tuning revisit (tenant rubber-stamp fraction &gt; 15 %).</summary>
    public const string TuningRevisitReasonCode = "approval_tuning_revisit_triggered";

    public static ApprovalRubberStampRateObservation Evaluate(
        IReadOnlyList<ApprovalDecisionSample> decisions,
        string tenantRef,
        string correlationId,
        ISystemClock clock)
    {
        ArgumentNullException.ThrowIfNull(decisions);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(clock);

        DateTimeOffset now = clock.UtcNow;

        // Denominator filter (AC1/AC2): in-window [0, 7d) keyed on DecidedAtUtc, an Approved decision against an
        // approval-required action. Rejections / revision-requests / cancellations / low-risk are excluded.
        List<ApprovalDecisionSample> qualifying = [];
        foreach (ApprovalDecisionSample decision in decisions)
        {
            if (decision.DecisionKind == ApprovalDecisionKind.Approve &&
                decision.AiRiskClass == AiActionRiskClass.ApprovalRequired &&
                IsInWindow(decision.DecidedAtUtc, now))
            {
                qualifying.Add(decision);
            }
        }

        int approvalTotal = qualifying.Count;
        int rubberStampCount = qualifying.Count(IsRubberStamp);

        // Per-(tenant × reviewer) breakdown for diagnosis (AC1/AC4): null/blank DecisionActorId is excluded from
        // per-reviewer attribution (no phantom reviewer) but still counts in the tenant aggregate above. Deterministic
        // reviewer-ref order so the observation is stable given the same snapshot + clock.
        List<ReviewerRubberStampRate> perReviewer = qualifying
            .Where(static d => !string.IsNullOrWhiteSpace(d.ReviewerRef))
            .GroupBy(static d => d.ReviewerRef!, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .Select(static group => new ReviewerRubberStampRate(group.Key, group.Count(IsRubberStamp), group.Count()))
            .ToList();

        // FR41 tenant-level crossing (AC3): strictly-greater-than via exact integer arithmetic —
        // rubberStampCount × 100 > 15 × totalApprovals (equivalently fraction > 0.15). Exactly 15.000 % does not fire.
        // A zero denominator (0/0 / empty / no-qualifying window) never triggers and never divides by zero (AC4).
        bool triggered = approvalTotal > 0 &&
            ((long)rubberStampCount * 100) > ((long)RubberStampRateObservable.FatigueFractionPercent * approvalTotal);

        // Exact integer-floor permille (no lossy double); the exact rational is the count + total pair carried alongside.
        int permille = approvalTotal == 0 ? 0 : (int)((long)rubberStampCount * 1000 / approvalTotal);

        return new ApprovalRubberStampRateObservation(
            tenantRef,
            correlationId,
            rubberStampCount,
            approvalTotal,
            permille,
            triggered,
            perReviewer);
    }

    /// <summary>
    /// A decision is in-window when its server-measured age (<c>now − DecidedAtUtc</c>, UTC) falls strictly inside the
    /// trailing 7-day window — copies <see cref="NotificationThrottleEvaluator.CountInTrailingWindow"/>. A decision
    /// exactly <see cref="RubberStampRateObservable.RollingWindow"/> old is outside; future-dated decisions are ignored.
    /// </summary>
    private static bool IsInWindow(DateTimeOffset decidedAtUtc, DateTimeOffset now)
    {
        TimeSpan age = now - decidedAtUtc.ToUniversalTime();
        return age >= TimeSpan.Zero && age < RubberStampRateObservable.RollingWindow;
    }

    /// <summary>
    /// A decision is rubber-stamp when its server-measured latency is <strong>strictly less than</strong>
    /// <see cref="RubberStampRateObservable.RubberStampLatencySeconds"/> (4.999 s counts, 5.000 s does not).
    /// </summary>
    private static bool IsRubberStamp(ApprovalDecisionSample decision)
        => EffectiveLatencySeconds(decision) < RubberStampRateObservable.RubberStampLatencySeconds;

    /// <summary>
    /// Decision latency = <c>DecidedAtUtc − RequestedAtUtc</c>, server-measured in UTC and <strong>clamped ≥ 0</strong>
    /// — copies the <see cref="EscalationPolicyEvaluator"/> clamp discipline. A non-positive or future-skewed pair
    /// clamps to 0 s; never trusts client-supplied time beyond the server-stamped timestamps.
    /// </summary>
    private static double EffectiveLatencySeconds(ApprovalDecisionSample decision)
    {
        double seconds = (decision.DecidedAtUtc.ToUniversalTime() - decision.RequestedAtUtc.ToUniversalTime()).TotalSeconds;
        return seconds <= 0 ? 0 : seconds;
    }
}
