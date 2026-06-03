using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Pure, deterministic evaluator for the NFR43 <c>approval-queue age</c> alert threshold (Story 8.4, AC3). Given the
/// tenant-bound queue snapshot and an injected "now", it fires a single aggregate metadata-only
/// <see cref="OperationalAlertPayload"/> per tenant when any non-terminal <see cref="OperationalQueueFamily.PendingApproval"/>
/// item's server-measured age is at or above the threshold (default 172,800 s ≈ 2 business days). At most one alert
/// fires per pass regardless of how many items exceed the threshold (deduplicated per tenant). Reuses the
/// <see cref="ReviewerBacklogEvaluator"/> open/terminal and server-measured-age logic; returns <see langword="null"/>
/// when no qualifying item exceeds the threshold.
/// </summary>
internal static class ApprovalQueueAgeAlertEvaluator
{
    /// <summary>The 2-business-day threshold as a conservative 48-calendar-hour UTC approximation (NFR43).</summary>
    public const int BusinessDayAlertThresholdSeconds = 172800;

    public const string ReasonCode = "approval_queue_age_threshold_exceeded";
    public const string OwnerRole = AdminRoles.OperationsAdmin;
    public const string NextSafeAction = "review-approval-queue";

    /// <summary>
    /// Terminal/decided/resolved status tokens excluded from the open scan. Normalized (case-insensitive,
    /// hyphen-stripped), mirroring <see cref="ReviewerBacklogEvaluator"/>.
    /// </summary>
    private static readonly IReadOnlySet<string> TerminalStatusTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "resolved",
        Normalize(ApprovalStatus.Approved.ToString()),
        Normalize(ApprovalStatus.Rejected.ToString()),
        Normalize(ApprovalStatus.RevisionRequested.ToString()),
        Normalize(ApprovalStatus.Cancelled.ToString()),
        Normalize(ApprovalStatus.Executed.ToString()),
        Normalize(ApprovalStatus.Failed.ToString()),
        Normalize(LifecycleStates.Skipped),
    };

    public static OperationalAlertPayload? Evaluate(
        IReadOnlyList<AdminQueueSummaryProjectionItem> items,
        string tenantRef,
        string correlationId,
        DateTimeOffset nowUtc,
        int thresholdSeconds = BusinessDayAlertThresholdSeconds)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        bool anyOverThreshold = items
            .Where(IsOpen)
            .Any(item => EffectiveAgeSeconds(item, nowUtc) >= thresholdSeconds);

        if (!anyOverThreshold)
        {
            return null;
        }

        return new OperationalAlertPayload(
            OperatorAlertKind.ApprovalQueueAgeBreached,
            $"tenant:{tenantRef}",
            OwnerRole,
            NextSafeAction,
            ReasonCode,
            tenantRef,
            correlationId,
            nowUtc.ToUniversalTime());
    }

    private static bool IsOpen(AdminQueueSummaryProjectionItem item)
        => item.QueueFamily == OperationalQueueFamily.PendingApproval && !IsTerminalOrResolved(item);

    private static bool IsTerminalOrResolved(AdminQueueSummaryProjectionItem item)
        => item.IsTerminal ||
            (!string.IsNullOrWhiteSpace(item.Status) && TerminalStatusTokens.Contains(Normalize(item.Status)));

    private static int EffectiveAgeSeconds(AdminQueueSummaryProjectionItem item, DateTimeOffset now)
    {
        // Server-measured UTC age against the injected clock; a future timestamp clamps to 0 (mirrors
        // ReviewerBacklogEvaluator.EffectiveAgeSeconds). Never client/item-supplied time.
        if (item.FreshnessTimestampUtc is { } freshness)
        {
            double seconds = (now - freshness.ToUniversalTime()).TotalSeconds;
            return seconds <= 0 ? 0 : (int)Math.Min(seconds, int.MaxValue);
        }

        return Math.Max(0, item.AgeSeconds);
    }

    private static string Normalize(string status)
        => status.Replace("-", string.Empty, StringComparison.Ordinal);
}
