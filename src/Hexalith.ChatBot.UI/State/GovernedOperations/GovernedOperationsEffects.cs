using Fluxor;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.UI.Services;

namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>
/// Side-effecting handlers for the governed-operations slice. The submit effect calls the spine through
/// <see cref="GovernedOperationService"/> (and therefore only through <c>IChatBotClient</c>) and dispatches a
/// success or a safe metadata-only failure — never raw exception text.
/// </summary>
public sealed class GovernedOperationsEffects(GovernedOperationService service)
{
    /// <summary>The generic safe failure code used when the server returned no catalog-backed problem code.</summary>
    public const string GenericFailureCode = ChatBotMessageCodes.GenericDenial;

    private readonly GovernedOperationService _service = service ?? throw new ArgumentNullException(nameof(service));

    /// <summary>Handles a submit request by routing it through the spine and dispatching the outcome.</summary>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    [EffectMethod(typeof(SubmitGovernedNoteAction))]
    public async Task HandleSubmitAsync(IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        try
        {
            OperationOutcome outcome = await _service.SubmitGovernedNoteAsync().ConfigureAwait(false);
            dispatcher.Dispatch(new GovernedNoteSubmittedAction(outcome));
        }
        catch (OperationCanceledException cancellation) when (!ChatBotUiFailureCatalog.IsTimeout(cancellation))
        {
            // Explicit cancellation is rethrown after clearing the in-flight flag so the action does not stay disabled.
            // Client timeouts pass through the safe failure mapping instead.
            dispatcher.Dispatch(new GovernedNoteSubmissionCancelledAction());
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            // The server returned a catalog-backed, already-redacted (metadata-only) problem. Surface its stable
            // code so distinct server problems (e.g. authorization vs. conflict vs. allowlist refusal) stay
            // distinguishable to the user instead of collapsing to one generic message.
            dispatcher.Dispatch(new GovernedNoteSubmissionFailedAction(ChatBotUiFailureCatalog.Resolve(problem)));
        }
        catch (Exception exception)
        {
            // Transient transport failures retain retry guidance; programming failures remain terminal and metadata-only.
            dispatcher.Dispatch(new GovernedNoteSubmissionFailedAction(ChatBotUiFailureCatalog.Resolve(exception)));
        }
    }

}
