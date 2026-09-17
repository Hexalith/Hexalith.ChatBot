using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedOutboundChannelRateLimitProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IOutboundChannelRateLimitProvider
{
    public async ValueTask<OutboundChannelRateLimitState?> GetRateLimitAsync(string tenantId, string outboundChannelRef, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.OutboundChannel, outboundChannelRef, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.IsStale(view, clock.UtcNow)
            ? new OutboundChannelRateLimitState(1, OutboundChannelRateLimitWindow.RollingHour)
            : view?.RateLimitBudget is null ? null : new OutboundChannelRateLimitState(view.RateLimitBudget.Value, OutboundChannelRateLimitWindow.RollingHour);
    }
}
