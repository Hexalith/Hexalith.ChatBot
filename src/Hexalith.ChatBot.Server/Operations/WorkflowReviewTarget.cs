namespace Hexalith.ChatBot.Server.Operations;

/// <summary>Owner-bound metadata needed to rejoin the original operation after an authorized retry.</summary>
public sealed record WorkflowReviewTarget(string AssociationId, string IntakeId, string Phase, string[]? IndexedUnits = null)
{
    internal bool Matches(WorkflowReviewTarget? other) => other is not null && AssociationId == other.AssociationId &&
        IntakeId == other.IntakeId && Phase == other.Phase && (IndexedUnits ?? []).SequenceEqual(other.IndexedUnits ?? [], StringComparer.Ordinal);
}
