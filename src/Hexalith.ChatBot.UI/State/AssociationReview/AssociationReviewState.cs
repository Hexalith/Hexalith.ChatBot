namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed record AssociationReviewState(
    bool IsLoading,
    bool IsSubmitting,
    AssociationReviewModel? Review,
    string? SelectedCandidateId,
    string DecisionNote,
    string CorrectionRationale,
    string? ErrorCode,
    string? ValidationErrorCode)
{
    public AssociationCandidateModel? SelectedCandidate
        => Review?.Candidates.FirstOrDefault(candidate => string.Equals(candidate.ProjectId, SelectedCandidateId, StringComparison.Ordinal));
}
