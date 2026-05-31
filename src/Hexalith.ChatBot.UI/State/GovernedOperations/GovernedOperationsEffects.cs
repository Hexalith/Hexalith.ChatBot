using Fluxor;

using Hexalith.ChatBot.Client.Generated;
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
    public const string GenericFailureCode = "submission-failed";

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
        catch (OperationCanceledException)
        {
            // Cancellation (navigation away / component disposal) is not a submission failure: never collapse it
            // into a generic failure — rethrow so the host observes the cancellation honestly.
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            // The server returned a catalog-backed, already-redacted (metadata-only) problem. Surface its stable
            // code so distinct server problems (e.g. authorization vs. conflict vs. allowlist refusal) stay
            // distinguishable to the user instead of collapsing to one generic message.
            dispatcher.Dispatch(new GovernedNoteSubmissionFailedAction(SafeFailureCode(problem.Result?.Code)));
        }
        catch (Exception)
        {
            // Any other (transport/unknown) failure collapses to a single stable safe code, never raw text (NFR40).
            dispatcher.Dispatch(new GovernedNoteSubmissionFailedAction(GenericFailureCode));
        }
    }

    private static string SafeFailureCode(string? problemCode)
        => string.IsNullOrWhiteSpace(problemCode) ? GenericFailureCode : problemCode;
}
