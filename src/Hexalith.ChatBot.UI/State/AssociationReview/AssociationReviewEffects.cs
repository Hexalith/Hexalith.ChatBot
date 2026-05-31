using Fluxor;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.Services;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

public sealed class AssociationReviewEffects(AssociationReviewService service, IState<AssociationReviewState>? state = null)
{
    public const string GenericFailureCode = "association-review-unavailable";

    private readonly AssociationReviewService _service = service ?? throw new ArgumentNullException(nameof(service));
    private readonly IState<AssociationReviewState>? _state = state;

    [EffectMethod]
    public async Task HandleLoadAsync(LoadAssociationReviewAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        try
        {
            AssociationReviewModel review = await _service
                .GetAssociationReviewAsync(action.AssociationId)
                .ConfigureAwait(false);
            dispatcher.Dispatch(new AssociationReviewLoadedAction(review));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            dispatcher.Dispatch(new AssociationReviewFailedAction(SafeFailureCode(problem.Result?.Code)));
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new AssociationReviewFailedAction(GenericFailureCode));
        }
    }

    [EffectMethod]
    public async Task HandlePreviewAsync(PreviewAssociationDecisionAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        AssociationReviewState? current = _state?.Value;
        if (current?.Review is null)
        {
            dispatcher.Dispatch(new AssociationDecisionPreviewRejectedAction(GenericFailureCode));
            return;
        }

        if (string.Equals(action.DecisionCode, "choose-candidate", StringComparison.Ordinal) &&
            current.SelectedCandidate is null)
        {
            dispatcher.Dispatch(new AssociationDecisionPreviewRejectedAction("candidate-required"));
            return;
        }

        try
        {
            AssociationDecisionSubmitResult result = await _service
                .SubmitDecisionAsync(
                    current.Review,
                    action.DecisionCode,
                    current.SelectedCandidateId,
                    current.DecisionNote)
                .ConfigureAwait(false);
            dispatcher.Dispatch(new AssociationDecisionSubmittedAction(result));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            dispatcher.Dispatch(new AssociationDecisionSubmitFailedAction(SafeFailureCode(problem.Result?.Code)));
        }
        catch (InvalidOperationException invalid) when (IsSafeValidationCode(invalid.Message))
        {
            dispatcher.Dispatch(new AssociationDecisionPreviewRejectedAction(invalid.Message));
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new AssociationDecisionSubmitFailedAction(GenericFailureCode));
        }
    }

    [EffectMethod]
    public async Task HandleCorrectionAsync(SubmitAssociationCorrectionAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        AssociationReviewState? current = _state?.Value;
        if (current?.Review is null)
        {
            dispatcher.Dispatch(new AssociationCorrectionValidationRejectedAction(GenericFailureCode));
            return;
        }

        if (current.Review.LifecycleState is not ("Associated" or "Corrected"))
        {
            dispatcher.Dispatch(new AssociationCorrectionValidationRejectedAction("correction-invalid-lifecycle"));
            return;
        }

        if (current.SelectedCandidate is null)
        {
            dispatcher.Dispatch(new AssociationCorrectionValidationRejectedAction("correction-target-required"));
            return;
        }

        try
        {
            AssociationCorrectionSubmitResult result = await _service
                .SubmitCorrectionAsync(
                    current.Review,
                    current.SelectedCandidate.ProjectId,
                    current.CorrectionRationale)
                .ConfigureAwait(false);
            dispatcher.Dispatch(new AssociationCorrectionSubmittedAction(result));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            dispatcher.Dispatch(new AssociationCorrectionSubmitFailedAction(SafeFailureCode(problem.Result?.Code)));
        }
        catch (InvalidOperationException invalid) when (IsSafeValidationCode(invalid.Message))
        {
            dispatcher.Dispatch(new AssociationCorrectionValidationRejectedAction(invalid.Message));
        }
        catch (Exception)
        {
            dispatcher.Dispatch(new AssociationCorrectionSubmitFailedAction(GenericFailureCode));
        }
    }

    private static string SafeFailureCode(string? problemCode)
        => string.IsNullOrWhiteSpace(problemCode) ? GenericFailureCode : problemCode;

    private static bool IsSafeValidationCode(string code)
        => code is "candidate-required"
            or "correction-invalid-lifecycle"
            or "correction-target-required"
            or "stale-evidence"
            or "association-review-note-too-long";
}
