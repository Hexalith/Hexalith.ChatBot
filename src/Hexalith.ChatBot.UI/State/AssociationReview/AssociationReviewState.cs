namespace Hexalith.ChatBot.UI.State.AssociationReview;

/// <summary>
/// Which operation produced the current <see cref="AssociationReviewState.ErrorCode"/>. A failed read and a
/// failed write are opposite messages: telling a reviewer their submission did not complete when nothing was
/// ever submitted invites a duplicate command.
/// </summary>
public enum AssociationReviewErrorScope
{
    None = 0,
    Load = 1,
    Submit = 2,
}

/// <summary>Identity of the last accepted governed command, so the surface can show what it accepted.</summary>
public sealed record AssociationOperationIdentity(
    string CommandId,
    string CorrelationId,
    string? TaskId,
    string LifecycleState);

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
    public AssociationCandidateModel? SelectedCandidate
        => Review?.Candidates.FirstOrDefault(candidate => string.Equals(candidate.ProjectId, SelectedCandidateId, StringComparison.Ordinal));

    /// <summary>Gets a value indicating whether a decision is awaiting explicit confirmation.</summary>
    public bool HasPendingDecision => !string.IsNullOrWhiteSpace(PendingDecisionCode);
}
