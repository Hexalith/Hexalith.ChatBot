using Fluxor;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.Services;

namespace Hexalith.ChatBot.UI.State.AssociationReview;

/// <summary>
/// Governed decision and correction effects. <see cref="IState{TState}"/> is required, not optional: a DI
/// misconfiguration must fail at startup rather than degrade every decision into a silent
/// "association-review-unavailable" rejection at runtime.
/// </summary>
public sealed class AssociationReviewEffects(AssociationReviewService service, IState<AssociationReviewState> state)
{
    public const string GenericFailureCode = AssociationReviewFailureCatalog.GenericFailureCode;

    private readonly AssociationReviewService _service = service ?? throw new ArgumentNullException(nameof(service));
    private readonly IState<AssociationReviewState> _state = state ?? throw new ArgumentNullException(nameof(state));

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

            // A slower earlier load must not overwrite the association the reviewer is now looking at.
            if (!string.Equals(review.AssociationId, action.AssociationId, StringComparison.Ordinal))
            {
                return;
            }

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

    /// <summary>
    /// Submits the decision the reviewer confirmed. Nothing durable happens before this action - opening the
    /// confirmation dispatches <see cref="RequestAssociationDecisionAction"/>, which writes no command.
    /// </summary>
    [EffectMethod]
    public async Task HandleConfirmedDecisionAsync(ConfirmAssociationDecisionAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        AssociationReviewState current = _state.Value;
        if (current.Review is null || current.PendingDecisionCode is not { Length: > 0 } decisionCode)
        {
            dispatcher.Dispatch(new AssociationDecisionValidationRejectedAction(GenericFailureCode));
            return;
        }

        // Re-validate against the state as it stands now. The review can refresh to a terminal or blocked
        // state between the render that enabled the action and the confirmation.
        if (current.Review.IsTerminal)
        {
            dispatcher.Dispatch(new AssociationDecisionValidationRejectedAction("terminal-state"));
            return;
        }

        if (AssociationReviewActionPolicy.ResolveDecisionDisabledReasonCode(
                current.Review.IsTerminal,
                isSubmitting: false,
                requiresCandidate: string.Equals(decisionCode, "choose-candidate", StringComparison.Ordinal),
                hasSelectedCandidate: current.SelectedCandidate is not null,
                current.Review.DisabledActionReasonCodes) is { Length: > 0 } blocked)
        {
            dispatcher.Dispatch(new AssociationDecisionValidationRejectedAction(
                IsSafeValidationCode(blocked) ? blocked : GenericFailureCode));
            return;
        }

        try
        {
            AssociationDecisionSubmitResult result = await _service
                .SubmitDecisionAsync(
                    current.Review,
                    decisionCode,
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
            dispatcher.Dispatch(new AssociationDecisionValidationRejectedAction(invalid.Message));
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

        AssociationReviewState current = _state.Value;
        if (current.Review is null)
        {
            dispatcher.Dispatch(new AssociationCorrectionValidationRejectedAction(GenericFailureCode));
            return;
        }

        // This gate is the single authority on which lifecycles may be corrected; the surface's CanCorrect
        // mirrors it exactly so the panel is never rendered with a submit that can only fail.
        if (!AssociationReviewModelExtensions.CanCorrect(current.Review.LifecycleState))
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
        => AssociationReviewFailureCatalog.SafeCode(problemCode);

    private static bool IsSafeValidationCode(string code)
        => code is "candidate-required"
            or "correction-invalid-lifecycle"
            or "correction-target-required"
            or "correction-source-required"
            or "stale-evidence"
            or "terminal-state"
            or "not-authorized"
            or "target-unauthorized"
            or "policy-blocked"
            or "already-decided"
            or "association-review-note-too-long";
}
