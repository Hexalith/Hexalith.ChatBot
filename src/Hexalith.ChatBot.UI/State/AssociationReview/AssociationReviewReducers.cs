using Fluxor;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

public static class AssociationReviewReducers
{
    [ReducerMethod]
    public static AssociationReviewState ReduceLoad(AssociationReviewState state, LoadAssociationReviewAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        // Clear everything that belonged to the association we are leaving. Keeping Review/SelectedCandidateId
        // would render the previous association's candidates, evidence, and lifecycle under the new id until
        // the fetch lands; keeping DecisionNote would submit a note authored for the previous association.
        bool sameAssociation = string.Equals(state.Review?.AssociationId, action.AssociationId, StringComparison.Ordinal);

        return state with
        {
            IsLoading = true,
            Review = sameAssociation ? state.Review : null,
            SelectedCandidateId = sameAssociation ? state.SelectedCandidateId : null,
            DecisionNote = sameAssociation ? state.DecisionNote : string.Empty,
            CorrectionRationale = sameAssociation ? state.CorrectionRationale : string.Empty,
            PendingDecisionCode = null,
            LastAcceptedOperation = sameAssociation ? state.LastAcceptedOperation : null,
            ErrorCode = null,
            ErrorScope = AssociationReviewErrorScope.None,
            ValidationErrorCode = null,
        };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceLoaded(AssociationReviewState state, AssociationReviewLoadedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        return state with
        {
            IsLoading = false,
            Review = action.Review,
            SelectedCandidateId = ReconcileSelection(action.Review, state.SelectedCandidateId),
            ErrorCode = null,
            ErrorScope = AssociationReviewErrorScope.None,
            ValidationErrorCode = null,
        };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceFailed(AssociationReviewState state, AssociationReviewFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsLoading = false,
            ErrorCode = action.ErrorCode,
            ErrorScope = AssociationReviewErrorScope.Load,
        };
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
    public static AssociationReviewState ReduceCorrectionRationale(AssociationReviewState state, UpdateAssociationCorrectionRationaleAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { CorrectionRationale = action.CorrectionRationale, ValidationErrorCode = null };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceDecisionRequested(AssociationReviewState state, RequestAssociationDecisionAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        // Opening the confirmation must not start a submission. IsSubmitting stays false until the reviewer
        // confirms, so nothing durable is in flight while the confirmation is on screen.
        return state with
        {
            PendingDecisionCode = action.DecisionCode,
            ValidationErrorCode = null,
            ErrorCode = null,
            ErrorScope = AssociationReviewErrorScope.None,
        };
    }

    [ReducerMethod(typeof(CancelAssociationDecisionAction))]
    public static AssociationReviewState ReduceDecisionCancelled(AssociationReviewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with { PendingDecisionCode = null };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceDecisionRejected(AssociationReviewState state, AssociationDecisionValidationRejectedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsSubmitting = false,
            PendingDecisionCode = null,
            ValidationErrorCode = action.ValidationErrorCode,
        };
    }

    [ReducerMethod(typeof(ConfirmAssociationDecisionAction))]
    public static AssociationReviewState ReduceSubmitStarted(AssociationReviewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with
        {
            IsSubmitting = true,
            ValidationErrorCode = null,
            ErrorCode = null,
            ErrorScope = AssociationReviewErrorScope.None,
        };
    }

    [ReducerMethod(typeof(SubmitAssociationCorrectionAction))]
    public static AssociationReviewState ReduceCorrectionSubmitStarted(AssociationReviewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with
        {
            IsSubmitting = true,
            ValidationErrorCode = null,
            ErrorCode = null,
            ErrorScope = AssociationReviewErrorScope.None,
        };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceSubmitted(AssociationReviewState state, AssociationDecisionSubmittedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsSubmitting = false,
            PendingDecisionCode = null,
            Review = action.Result.Review,
            SelectedCandidateId = ReconcileSelection(action.Result.Review, state.SelectedCandidateId),

            // The note belonged to the decision that was just recorded. Carrying it forward would silently
            // reuse it on the next decision.
            DecisionNote = string.Empty,
            LastAcceptedOperation = new AssociationOperationIdentity(
                action.Result.CommandId,
                action.Result.CorrelationId,
                action.Result.TaskId,
                action.Result.LifecycleState),
            ErrorCode = null,
            ErrorScope = AssociationReviewErrorScope.None,
            ValidationErrorCode = null,
        };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceSubmitFailed(AssociationReviewState state, AssociationDecisionSubmitFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsSubmitting = false,
            PendingDecisionCode = null,
            ErrorCode = action.ErrorCode,
            ErrorScope = AssociationReviewErrorScope.Submit,
        };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceCorrectionRejected(AssociationReviewState state, AssociationCorrectionValidationRejectedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { IsSubmitting = false, ValidationErrorCode = action.ValidationErrorCode };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceCorrectionSubmitted(AssociationReviewState state, AssociationCorrectionSubmittedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsSubmitting = false,
            Review = action.Result.Review,
            SelectedCandidateId = ReconcileSelection(action.Result.Review, state.SelectedCandidateId),
            CorrectionRationale = string.Empty,
            LastAcceptedOperation = new AssociationOperationIdentity(
                action.Result.CommandId,
                action.Result.CorrelationId,
                action.Result.TaskId,
                action.Result.LifecycleState),
            ErrorCode = null,
            ErrorScope = AssociationReviewErrorScope.None,
            ValidationErrorCode = null,
        };
    }

    [ReducerMethod]
    public static AssociationReviewState ReduceCorrectionFailed(AssociationReviewState state, AssociationCorrectionSubmitFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            IsSubmitting = false,
            ErrorCode = action.ErrorCode,
            ErrorScope = AssociationReviewErrorScope.Submit,
        };
    }

    /// <summary>
    /// Keeps the reviewer's selection across a refresh when it is still offered, and drops it when it is not,
    /// so <see cref="AssociationReviewState.SelectedCandidate"/> can never resolve to null while
    /// <c>SelectedCandidateId</c> still names a candidate that disappeared. A single candidate is only
    /// auto-selected when it is actually decidable - auto-selecting an evidence-incomplete candidate would be
    /// the hidden auto-association the UX contract forbids.
    /// </summary>
    private static string? ReconcileSelection(AssociationReviewModel review, string? selectedCandidateId)
    {
        if (selectedCandidateId is { } existing
            && review.Candidates.Any(candidate => string.Equals(candidate.ProjectId, existing, StringComparison.Ordinal)))
        {
            return existing;
        }

        return review.Candidates is [{ RequiredEvidenceComplete: true } only]
            && review.DisabledActionReasonCodes.Count == 0
                ? only.ProjectId
                : null;
    }
}
