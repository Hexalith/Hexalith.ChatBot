using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedCommandCapabilityControlStateProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : ICommandCapabilityControlStateProvider
{
    public async ValueTask<CommandCapabilityControlState> GetControlStateAsync(string tenantId, string commandCapabilityRef, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.CommandCapability, commandCapabilityRef, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.CommandCapabilityState(view, clock.UtcNow);
    }
}
