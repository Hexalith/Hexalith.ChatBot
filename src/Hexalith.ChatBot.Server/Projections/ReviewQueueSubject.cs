using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>Retains the original facts even when an earlier delivery arrives after its decision.</summary>
internal sealed record ReviewQueueSubject(ReviewQueueTransition Current, ReviewQueueTransition[] History)
{
    // This is derived from the retained approval event when a family is read.
    [JsonIgnore]
    internal AdminQueueSummaryProjectionItem? ApprovalPriority { get; init; }

    public static ReviewQueueSubject Add(ReviewQueueSubject? existing, ReviewQueueTransition fact)
    {
        if (existing is null) { return new(fact, [fact]); }
        if (existing.History.Any(item => item.StreamId == fact.StreamId && item.Version == fact.Version)) { return existing; }
        ReviewQueueTransition[] history = [.. existing.History, fact];
        // Only compare sequence numbers within a producing stream. A superseding decision
        // from another source is causal evidence even when its stream has a lower head.
        ReviewQueueTransition current = history.GroupBy(item => item.StreamId, StringComparer.Ordinal)
            .Select(stream => stream.MaxBy(item => item.Version)!)
            .Where(item => item.FailedStream is null || history.Where(f => f.StreamId == item.FailedStream).Max(f => f.Version) == item.FailedVersion)
            .OrderByDescending(item => item.Terminal)
            .ThenBy(item => item.RetainedApproval)
            .ThenByDescending(item => item.At)
            .ThenBy(item => item.StreamId, StringComparer.Ordinal)
            .First();
        return new(current, Chronological(history));
    }

    // Merge ordered stream prefixes by effective timestamp. Within each producer the version is authoritative,
    // even when an owner supplies an effective time earlier than the previous transition.
    internal static ReviewQueueTransition[] Chronological(IEnumerable<ReviewQueueTransition> facts)
    {
        var streams = facts.GroupBy(f => f.StreamId, StringComparer.Ordinal).Select(g => new Queue<ReviewQueueTransition>(g.OrderBy(f => f.Version))).ToList();
        List<ReviewQueueTransition> ordered = [];
        while (streams.Count > 0)
        {
            var next = streams.OrderBy(q => q.Peek().At).ThenBy(q => q.Peek().StreamId, StringComparer.Ordinal).First();
            ordered.Add(next.Dequeue());
            if (next.Count == 0) { streams.Remove(next); }
        }
        return ordered.ToArray();
    }

    internal string Revision => ProjectConversationStableId.Hash(System.Text.Json.JsonSerializer.Serialize(
        History.OrderBy(f => f.StreamId, StringComparer.Ordinal).ThenBy(f => f.Version).Select(f => new { f.StreamId, f.Version, f.OperationId })));

    internal static bool SameSnapshot(IReadOnlyList<ReviewQueueSubject> first, IReadOnlyList<ReviewQueueSubject> second)
    {
        // Facts are immutable, and history only grows. Index order may vary after concurrent inserts.
        static IEnumerable<(string, int)> Signature(IReadOnlyList<ReviewQueueSubject> items)
            => items.Select(s => (s.Current.ItemRef, s.History.Length)).OrderBy(pair => pair.ItemRef, StringComparer.Ordinal);
        return Signature(first).SequenceEqual(Signature(second));
    }

    public AdminQueueSummaryProjectionItem ToItem(DateTimeOffset now)
    {
        ReviewQueueTransition current = Current;
        var ordered = Chronological(History);
        var lastClosed = Array.FindLastIndex(ordered, f => f.Terminal);
        DateTimeOffset entered = ordered.Skip(lastClosed + 1).FirstOrDefault(f => f.OpensWork)?.At ?? current.At;
        return new("queue:" + OperationalQueueFamilies.ToWireValue(current.Family), current.ItemRef,
            current.State, current.OwnerRole, ChatBotHealthStatus.Healthy,
            (int)Math.Clamp((now - entered).TotalSeconds, 0, int.MaxValue), current.Family,
            Risk: current.Risk ?? "unknown", Confidence: current.Confidence ?? 0, AssigneeRef: current.Assignee,
            NextAction: current.NextAction, RetryCount: current.RetryCount, IsTerminal: current.Terminal,
            PriorityScore: ApprovalPriority?.PriorityScore ?? 0,
            PriorityExplanation: ApprovalPriority?.PriorityExplanation ?? "stable-order",
            GroupKey: ApprovalPriority?.GroupKey,
            GroupRequesterRef: ApprovalPriority?.GroupRequesterRef,
            GroupCommandRef: ApprovalPriority?.GroupCommandRef,
            GroupProjectRef: current.IdentityProject is { } project ? "project:" + project : null, FreshnessTimestampUtc: current.At, OwnerRole: current.OwnerRole, MailboxRef: current.Mailbox,
            FailureState: current.Reason, SourceVersion: current.Version, Revision: Revision, CorrelationId: current.CorrelationId,
            TenantRef: current.TenantId, LastTransitionActor: current.Actor, LastTransitionTimestampUtc: current.At,
            LastTransitionFromState: History.LastOrDefault(item => item.StreamId == current.StreamId && item.Version < current.Version)?.State);
    }
}
