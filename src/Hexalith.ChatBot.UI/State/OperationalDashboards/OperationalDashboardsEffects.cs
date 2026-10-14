using Fluxor;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.UI.Services;

namespace Hexalith.ChatBot.UI.State.OperationalDashboards;

/// <summary>
/// Side-effecting handlers for the operational-dashboards slice. The load effect reads the metadata-only health
/// overview through <see cref="OperationalDashboardService"/> (and therefore only through the <c>IChatBotClient</c>
/// façade), then dispatches the overview or a safe metadata-only failure code — never raw exception text.
/// </summary>
public sealed class OperationalDashboardsEffects
{
    /// <summary>The generic safe failure code used when the server returned no catalog-backed problem code.</summary>
    public const string GenericFailureCode = ChatBotMessageCodes.GenericDenial;

    private readonly Func<Task<OperationalDashboardOverview>> _loadOverview;

    /// <summary>Creates the effect using the UI-owned dashboard service.</summary>
    /// <param name="service">The dashboard read seam.</param>
    public OperationalDashboardsEffects(OperationalDashboardService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        _loadOverview = () => service.GetOverviewAsync();
    }

    /// <summary>Provides a deterministic read seam for effect verification.</summary>
    /// <param name="loadOverview">The metadata-only dashboard reader.</param>
    internal OperationalDashboardsEffects(Func<Task<OperationalDashboardOverview>> loadOverview)
    {
        ArgumentNullException.ThrowIfNull(loadOverview);
        _loadOverview = loadOverview;
    }

    /// <summary>Handles a load/refresh request by routing it through the spine and dispatching the outcome.</summary>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    [EffectMethod(typeof(LoadOperationalDashboardAction))]
    public async Task HandleLoadAsync(IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        try
        {
            OperationalDashboardOverview overview = await _loadOverview().ConfigureAwait(false);
            dispatcher.Dispatch(new OperationalDashboardLoadedAction(overview));
        }
        catch (OperationCanceledException cancellation) when (!ChatBotUiFailureCatalog.IsTimeout(cancellation))
        {
            // Cancellation (navigation away / component disposal) is not a load failure: rethrow honestly.
            throw;
        }
        catch (HexalithChatBotApiException<ProblemDetails> problem)
        {
            // Surface the server's catalog-backed, already-redacted stable code so distinct problems
            // (authorization vs. audit-unavailable) stay distinguishable instead of collapsing to one message.
            dispatcher.Dispatch(new OperationalDashboardLoadFailedAction(ChatBotUiFailureCatalog.Resolve(problem)));
        }
        catch (Exception exception)
        {
            // Transient transport failures retain retry guidance; programming failures remain terminal and metadata-only.
            dispatcher.Dispatch(new OperationalDashboardLoadFailedAction(ChatBotUiFailureCatalog.Resolve(exception)));
        }
    }

}
