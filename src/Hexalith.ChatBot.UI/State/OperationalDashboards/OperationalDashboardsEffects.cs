using Fluxor;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.UI.Services;

namespace Hexalith.ChatBot.UI.State.OperationalDashboards;

/// <summary>
/// Side-effecting handlers for the operational-dashboards slice. The load effect reads the metadata-only health
/// overview through <see cref="OperationalDashboardService"/> (and therefore only through the <c>IChatBotClient</c>
/// façade), then dispatches the overview or a safe metadata-only failure code — never raw exception text.
/// </summary>
public sealed class OperationalDashboardsEffects(OperationalDashboardService service)
{
    /// <summary>The generic safe failure code used when the server returned no catalog-backed problem code.</summary>
    public const string GenericFailureCode = "dashboard-load-failed";

    private readonly OperationalDashboardService _service = service ?? throw new ArgumentNullException(nameof(service));

    /// <summary>Handles a load/refresh request by routing it through the spine and dispatching the outcome.</summary>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    [EffectMethod(typeof(LoadOperationalDashboardAction))]
    public async Task HandleLoadAsync(IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        try
        {
            OperationalDashboardOverview overview = await _service.GetOverviewAsync().ConfigureAwait(false);
            dispatcher.Dispatch(new OperationalDashboardLoadedAction(overview));
        }
        catch (OperationCanceledException)
        {
            // Cancellation (navigation away / component disposal) is not a load failure: rethrow honestly.
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            // Surface the server's catalog-backed, already-redacted stable code so distinct problems
            // (authorization vs. audit-unavailable) stay distinguishable instead of collapsing to one message.
            dispatcher.Dispatch(new OperationalDashboardLoadFailedAction(SafeFailureCode(problem.Result?.Code)));
        }
        catch (Exception)
        {
            // Any other (transport/unknown) failure collapses to a single stable safe code, never raw text (NFR40).
            dispatcher.Dispatch(new OperationalDashboardLoadFailedAction(GenericFailureCode));
        }
    }

    private static string SafeFailureCode(string? problemCode)
        => string.IsNullOrWhiteSpace(problemCode) ? GenericFailureCode : problemCode;
}
