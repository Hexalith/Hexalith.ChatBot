using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedOutboundChannelControlStateProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IOutboundChannelControlStateProvider
{
    public async ValueTask<OutboundChannelControlState> GetControlStateAsync(string tenantId, string outboundChannelRef, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.OutboundChannel, outboundChannelRef, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.OutboundChannelState(view, clock.UtcNow);
    }
}
