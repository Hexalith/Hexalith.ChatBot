using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// Deterministic, clock-injected reviewer-backlog aggregation + threshold engine (Story 7.10, NFR46, NFR2). Given the
/// tenant-bound open-approval queue snapshot, the candidate tenant-admin recipient set, the tenant ref, the correlation
/// id, the configured threshold, and an injected "now", it groups open items by reviewer (<c>AssigneeRef</c>), counts
/// the open items per <c>(tenant × reviewer)</c> pair, computes the oldest open item's age, and produces a backlog alert
/// for each reviewer whose open count <em>strictly exceeds</em> the threshold.
/// <para>
/// Mirrors the Story 7.7 <see cref="EscalationPolicyEvaluator"/> discipline exactly: static, pure, no wall-clock access,
/// deterministic given the injected clock. The alert is an <em>aggregate</em> signal — the synthesized
/// <see cref="NotificationStateEvent"/> carries a null <c>ItemProjectRef</c>, so <see cref="NotificationRoutingResolver"/>
/// resolves it to the redacted form (item ref dropped, indistinguishable from safe-not-found, NFR2). The tenant ref comes
/// from the authenticated binding; reviewer/item/queue/correlation refs are keying/aggregation inputs only. Items with no
/// <c>AssigneeRef</c> are not attributed to any reviewer (no phantom backlog). Open count excludes terminal/decided/
/// resolved items, mirroring the escalation terminal exclusion and the <see cref="ApprovalPriorityScorer"/> statuses.
/// </para>
/// </summary>
internal static class ReviewerBacklogEvaluator
{
    /// <summary>The safe reason code carried by a fired backlog alert (open count strictly exceeded the threshold).</summary>
    public const string BacklogThresholdReasonCode = "reviewer_backlog_threshold_exceeded";

    /// <summary>The aggregate queue ref for a backlog alert — never an item-specific or per-project ref.</summary>
    private const string BacklogQueueRef = "queue:reviewer-backlog";

    /// <summary>
    /// The terminal/decided/resolved status tokens excluded from the open count. Normalized (case-insensitive,
    /// hyphen-stripped) so both lifecycle (<c>Skipped</c>) and approval (<c>RevisionRequested</c>/<c>revision-requested</c>)
    /// spellings are caught. Mirrors the Story 7.7 escalation exclusion and the Story 7.8 <c>ApprovalPriorityScorer</c>
    /// terminal statuses.
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

    public static IReadOnlyList<ReviewerBacklogAlert> Evaluate(
        IReadOnlyList<AdminQueueSummaryProjectionItem> items,
        IReadOnlyList<NotificationRecipientCandidate> candidates,
        string tenantRef,
        string correlationId,
        ReviewerBacklogThreshold threshold,
        ISystemClock clock)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(threshold);
        ArgumentNullException.ThrowIfNull(clock);

        // Trust boundary: an out-of-bounds threshold never raises the NFR46 cap — fall back to the safe default (25).
        int effectiveThreshold = (threshold.IsWithinBounds ? threshold : ReviewerBacklogThreshold.SafeDefault).OpenItemThreshold;

        DateTimeOffset now = clock.UtcNow;

        // Group open items by reviewer, keyed strictly by AssigneeRef. Unassigned items create no reviewer backlog.
        // Deterministic ordering by reviewer ref so the alert set is stable given the same snapshot + clock.
        IEnumerable<IGrouping<string, AdminQueueSummaryProjectionItem>> byReviewer = items
            .Where(IsOpen)
            .Where(static item => !string.IsNullOrWhiteSpace(item.AssigneeRef))
            .GroupBy(static item => item.AssigneeRef!, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal);

        List<ReviewerBacklogAlert> alerts = [];
        foreach (IGrouping<string, AdminQueueSummaryProjectionItem> reviewerItems in byReviewer)
        {
            int backlogDepth = reviewerItems.Count();

            // Strictly-greater-than: exactly-at-threshold does NOT alert; the next item crosses (mirrors the escalation
            // age-threshold `>` semantics).
            if (backlogDepth <= effectiveThreshold)
            {
                continue;
            }

            string reviewerRef = reviewerItems.Key;
            int oldestItemAgeSeconds = reviewerItems.Max(item => EffectiveAgeSeconds(item, now));

            // Aggregate, redacted event: ItemProjectRef = null → the resolver yields MetadataRedacted (item ref dropped).
            // The aggregate ItemRef/QueueRef are safe, content-free backlog tokens — never an item-specific or project ref.
            NotificationStateEvent stateEvent = new(
                tenantRef,
                NotificationStateClass.ApprovalPending,
                $"reviewer-backlog:{reviewerRef}",
                BacklogQueueRef,
                BacklogThresholdReasonCode,
                correlationId,
                now,
                ItemProjectRef: null);

            // Reuse the FR72 routing engine: synthesize the tenant-admin route and resolve audience + redaction through
            // NotificationRoutingResolver — no second authority/visibility path. The reviewer is never the recipient;
            // the tenant admin is alerted so the bottleneck surfaces to someone who can rebalance it.
            NotificationRoutingChangeSet routing = new(
            [
                new NotificationRoutingEntry(
                    NotificationStateClass.ApprovalPending,
                    AdminScope.SeeOnly,
                    AdminRole.TenantAdmin,
                    NotificationChannel.InApp),
            ]);

            foreach (NotificationDelivery delivery in NotificationRoutingResolver.Resolve(stateEvent, routing, candidates))
            {
                alerts.Add(new ReviewerBacklogAlert(delivery, reviewerRef, backlogDepth, oldestItemAgeSeconds, effectiveThreshold));
            }
        }

        return alerts;
    }

    /// <summary>An item is "open" when it is a non-terminal/non-resolved member of the pending-approval queue family.</summary>
    private static bool IsOpen(AdminQueueSummaryProjectionItem item)
        => item.QueueFamily == OperationalQueueFamily.PendingApproval && !IsTerminalOrResolved(item);

    private static bool IsTerminalOrResolved(AdminQueueSummaryProjectionItem item)
        => item.IsTerminal ||
            (!string.IsNullOrWhiteSpace(item.Status) && TerminalStatusTokens.Contains(Normalize(item.Status)));

    private static int EffectiveAgeSeconds(AdminQueueSummaryProjectionItem item, DateTimeOffset now)
    {
        // Age is server-measured in UTC against the injected clock — copies EscalationPolicyEvaluator.EffectiveAgeSeconds.
        // Prefer the freshness timestamp (recomputed at evaluation time); fall back to the projector's server-measured
        // AgeSeconds. Never client/item-supplied time; a future timestamp clamps to 0.
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
