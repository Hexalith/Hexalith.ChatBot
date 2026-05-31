using Fluxor;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

public static class AssociationReviewReducers
{
    [ReducerMethod(typeof(LoadAssociationReviewAction))]
    public static AssociationReviewState ReduceLoad(AssociationReviewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with { IsLoading = true, ErrorCode = null, ValidationErrorCode = null };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceLoaded(AssociationReviewState state, AssociationReviewLoadedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        string? selected = action.Review.Candidates.Count == 1
            ? action.Review.Candidates[0].ProjectId
            : state.SelectedCandidateId is { } existing
                && action.Review.Candidates.Any(candidate => string.Equals(candidate.ProjectId, existing, StringComparison.Ordinal))
                    ? existing
                    : null;

        return state with
        {
            IsLoading = false,
            Review = action.Review,
            SelectedCandidateId = selected,
            ErrorCode = null,
            ValidationErrorCode = null,
        };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceFailed(AssociationReviewState state, AssociationReviewFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { IsLoading = false, ErrorCode = action.ErrorCode };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceSelected(AssociationReviewState state, SelectAssociationCandidateAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { SelectedCandidateId = action.CandidateId, ValidationErrorCode = null };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceDecisionNote(AssociationReviewState state, UpdateAssociationDecisionNoteAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { DecisionNote = action.DecisionNote, ValidationErrorCode = null };
    }

    [ReducerMethod]
    public static AssociationReviewState ReducePreviewRejected(AssociationReviewState state, AssociationDecisionPreviewRejectedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { IsSubmitting = false, ValidationErrorCode = action.ValidationErrorCode };
    }

    [ReducerMethod(typeof(PreviewAssociationDecisionAction))]
    public static AssociationReviewState ReduceSubmitStarted(AssociationReviewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with { IsSubmitting = true, ValidationErrorCode = null, ErrorCode = null };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceSubmitted(AssociationReviewState state, AssociationDecisionSubmittedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsSubmitting = false,
            Review = action.Result.Review,
            ErrorCode = null,
            ValidationErrorCode = null,
        };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceSubmitFailed(AssociationReviewState state, AssociationDecisionSubmitFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { IsSubmitting = false, ErrorCode = action.ErrorCode };
    }
}
