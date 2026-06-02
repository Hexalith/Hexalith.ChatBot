using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// A tenant-bound unresolved queue item paired with its safe per-resource authority key (or <see langword="null"/>
/// for an aggregate item with no item-specific context). The authority key comes from the tenant-bound queue
/// snapshot, never from restricted content.
/// </summary>
internal sealed record EscalationQueueItem(
    AdminQueueSummaryProjectionItem Item,
    string? ItemProjectRef = null);

/// <summary>
/// Deterministic, clock-injected escalation evaluation engine (FR73, FR59, NFR2). Given the unresolved queue-item
/// snapshot, the active escalation-policy map, the candidate recipient set, and an injected "now", it determines
/// which items breach <c>age &gt; threshold OR severity &gt;= threshold</c> for their <c>(state-class × scope)</c>
/// and produces the escalation deliveries — reusing the existing <see cref="NotificationRoutingResolver"/> audience +
/// <see cref="NotificationContentVisibility"/> redaction path and the metadata-only <see cref="INotificationSink"/>
/// seam, NOT a second authority or delivery path. Terminal and resolved items never escalate. Pure given the clock —
/// no wall-clock access.
/// </summary>
internal static class EscalationPolicyEvaluator
{
    /// <summary>Threshold semantics: age is breached strictly-greater-than; severity is breached at-or-above.</summary>
    public const string AgeBreachReasonCode = "escalation_age_threshold_exceeded";

    public const string SeverityBreachReasonCode = "escalation_severity_threshold_met";

    public static IReadOnlyList<EscalationDelivery> Evaluate(
        IReadOnlyList<EscalationQueueItem> items,
        EscalationPolicyChangeSet policy,
        IReadOnlyList<NotificationRecipientCandidate> candidates,
        string tenantRef,
        string correlationId,
        ISystemClock clock)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(clock);

        // Trust boundary: only a schema-valid escalation map fires (fail-closed on undeclared values / out-of-range thresholds).
        if (!EscalationPolicySchema.Validate(policy).IsValid)
        {
            return [];
        }

        DateTimeOffset now = clock.UtcNow;
        List<EscalationDelivery> escalations = [];

        foreach (EscalationQueueItem queueItem in items)
        {
            AdminQueueSummaryProjectionItem item = queueItem.Item;

            // Terminal and resolved items never escalate.
            if (IsTerminalOrResolved(item))
            {
                continue;
            }

            NotificationStateClass stateClass = EscalationStateClassMap.Map(item);
            EscalationSeverity severity = EscalationSeverities.FromRisk(item.Risk);
            int ageSeconds = EffectiveAgeSeconds(item, now);

            foreach (EscalationPolicyEntry entry in policy.Entries)
            {
                if (entry.StateClass != stateClass)
                {
                    continue;
                }

                bool ageBreach = ageSeconds > entry.AgeThresholdSeconds;
                bool severityBreach = EscalationSeverities.MeetsOrExceeds(severity, entry.SeverityThreshold);
                if (!ageBreach && !severityBreach)
                {
                    continue;
                }

                EscalationBreachReason breachReason = ageBreach
                    ? EscalationBreachReason.AgeThreshold
                    : EscalationBreachReason.SeverityThreshold;
                string reasonCode = ageBreach ? AgeBreachReasonCode : SeverityBreachReasonCode;

                // Reuse the FR73 routing engine: synthesize the routing entry from the escalation target role + channel
                // and resolve audience + per-resource redaction through NotificationRoutingResolver — no second path.
                NotificationRoutingChangeSet routing = new(
                [
                    new NotificationRoutingEntry(stateClass, entry.Scope, entry.EscalationTargetRole, entry.EscalationChannel),
                ]);

                NotificationStateEvent stateEvent = new(
                    tenantRef,
                    stateClass,
                    item.ItemRef,
                    item.QueueRef,
                    reasonCode,
                    correlationId,
                    now,
                    queueItem.ItemProjectRef);

                foreach (NotificationDelivery delivery in NotificationRoutingResolver.Resolve(stateEvent, routing, candidates))
                {
                    escalations.Add(new EscalationDelivery(delivery, breachReason, severity, ageSeconds, entry.AgeThresholdSeconds));
                }
            }
        }

        return escalations;
    }

    private static bool IsTerminalOrResolved(AdminQueueSummaryProjectionItem item)
        => item.IsTerminal ||
            string.Equals(item.Status, "resolved", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.Status, LifecycleStates.Rejected, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.Status, LifecycleStates.Skipped, StringComparison.OrdinalIgnoreCase);

    private static int EffectiveAgeSeconds(AdminQueueSummaryProjectionItem item, DateTimeOffset now)
    {
        // Age is server-measured in UTC against the injected clock. Prefer the freshness timestamp (recomputed at
        // evaluation time); fall back to the projector's server-measured AgeSeconds. Never client/item-supplied time.
        if (item.FreshnessTimestampUtc is { } freshness)
        {
            double seconds = (now - freshness.ToUniversalTime()).TotalSeconds;
            return seconds <= 0 ? 0 : (int)Math.Min(seconds, int.MaxValue);
        }

        return Math.Max(0, item.AgeSeconds);
    }
}
