using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedServiceClientControlStateProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IServiceClientControlStateProvider
{
    public async ValueTask<ServiceClientControlState> GetControlStateAsync(string tenantId, string serviceClientId, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.ServiceClient, serviceClientId, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.ServiceClientState(view, clock.UtcNow);
    }
}
