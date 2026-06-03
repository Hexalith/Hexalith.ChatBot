using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Builds the prioritized, grouped <c>pending-approval</c> operational-queue rows from the approval truth source
/// (Story 7.8). It reuses the Story 7.5 <see cref="AdminQueueSummaryProjectionItem"/>/<see cref="AdminQueueSummaryProjector"/>
/// ordering surface — feeding the computed <see cref="ApprovalPriorityResult"/> through <c>PriorityScore</c>/
/// <c>PriorityExplanation</c> and the group fingerprint through the group-header fields — so there is no second sort
/// path. Only <see cref="ApprovalStatus.Pending"/> items enter the queue; decided/terminal approvals are excluded.
/// </summary>
internal static class ApprovalQueueItemBuilder
{
    public const string QueueRef = "queue:pending-approval";

    /// <summary>
    /// Maps a single pending approval to its prioritized/grouped queue row, or returns <see langword="null"/> for a
    /// decided/terminal approval (excluded from the prioritized pending queue).
    /// </summary>
    public static AdminQueueSummaryProjectionItem? TryBuild(
        ApprovalEventView view,
        ApprovalPriorityWeights weights,
        ISystemClock clock)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(clock);

        if (!ApprovalPriorityScorer.IsPending(view))
        {
            return null;
        }

        DateTimeOffset now = clock.UtcNow;
        ApprovalPriorityResult priority = ApprovalPriorityScorer.Evaluate(view, weights, now);
        int ageSeconds = AgeSeconds(view.RequestedAtUtc, now);

        return new AdminQueueSummaryProjectionItem(
            QueueRef: QueueRef,
            ItemRef: view.StableItemId,
            Status: OperationalQueueFamilies.ToWireValue(OperationalQueueFamily.PendingApproval),
            OwnerClass: "operations",
            Health: ChatBotHealthStatus.Healthy,
            AgeSeconds: ageSeconds,
            QueueFamily: OperationalQueueFamily.PendingApproval,
            Risk: RiskProxy(view.RiskClass ?? RiskClass.None),
            NextAction: "review",
            FreshnessTimestampUtc: view.RequestedAtUtc?.ToUniversalTime(),
            SourceVersion: view.SourceVersion,
            PriorityScore: priority.Score,
            PriorityExplanation: priority.Explanation,
            GroupKey: priority.GroupKey,
            GroupRequesterRef: view.RequesterId is { Length: > 0 } requester ? "requester:" + requester : null,
            GroupCommandRef: view.CommandName is { Length: > 0 } command ? "command:" + command : null,
            GroupProjectRef: "project:" + view.ProjectId,
            // NFR44 runbook-real diagnostic context, populated from the genuinely-carried approval/spine context.
            // The approval event view has no explicit prior-lifecycle-state field, so the originating event kind is
            // used as the last-transition from-marker; the actor and timestamp come from the request context.
            CorrelationId: view.CorrelationId,
            TenantRef: view.TenantId,
            LastTransitionFromState: LastTransitionFromState(view.EventKind),
            LastTransitionActor: view.RequesterId ?? view.DecisionActorId,
            LastTransitionTimestampUtc: view.RequestedAtUtc ?? view.OccurredAtUtc);
    }

    private static string LastTransitionFromState(ApprovalEventKind eventKind)
        => eventKind switch
        {
            ApprovalEventKind.Request => "request",
            ApprovalEventKind.Decision => "decision",
            ApprovalEventKind.Outcome => "outcome",
            _ => "request",
        };

    private static int AgeSeconds(DateTimeOffset? requestedAtUtc, DateTimeOffset now)
    {
        if (requestedAtUtc is not { } requested)
        {
            return 0;
        }

        double seconds = (now.ToUniversalTime() - requested.ToUniversalTime()).TotalSeconds;
        return seconds <= 0 ? 0 : (int)Math.Min(seconds, int.MaxValue);
    }

    private static string RiskProxy(RiskClass riskClass)
        => riskClass switch
        {
            RiskClass.Blocked => "critical",
            RiskClass.High => "high",
            RiskClass.Medium => "medium",
            RiskClass.Low => "low",
            _ => "low",
        };
}
