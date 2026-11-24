namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record AssociationReviewState(
    bool IsLoading,
    bool IsSubmitting,
    AssociationReviewModel? Review,
    string? SelectedCandidateId,
    string DecisionNote,
    string CorrectionRationale,
    string? ErrorCode,
    string? ValidationErrorCode,
    AssociationReviewErrorScope ErrorScope = AssociationReviewErrorScope.None,
    string? PendingDecisionCode = null,
    AssociationOperationIdentity? LastAcceptedOperation = null)
{
    /// <summary>Gets the exact load action that established the current route.</summary>
    internal LoadAssociationReviewAction? CurrentLoad { get; init; }

    public AssociationCandidateModel? SelectedCandidate
        => Review?.Candidates.FirstOrDefault(candidate => string.Equals(candidate.ProjectId, SelectedCandidateId, StringComparison.Ordinal));

    /// <summary>Gets a value indicating whether a decision is awaiting explicit confirmation.</summary>
    public bool HasPendingDecision => !string.IsNullOrWhiteSpace(PendingDecisionCode);
}
