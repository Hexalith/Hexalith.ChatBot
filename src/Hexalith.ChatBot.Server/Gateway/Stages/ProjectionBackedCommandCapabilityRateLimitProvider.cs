using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedCommandCapabilityRateLimitProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : ICommandCapabilityRateLimitProvider
{
    public async ValueTask<CommandCapabilityRateLimitState?> GetRateLimitAsync(string tenantId, string commandCapabilityRef, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.CommandCapability, commandCapabilityRef, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.IsStale(view, clock.UtcNow)
            ? new CommandCapabilityRateLimitState(1, CommandCapabilityRateLimitWindow.RollingHour)
            : view?.RateLimitBudget is null ? null : new CommandCapabilityRateLimitState(view.RateLimitBudget.Value, CommandCapabilityRateLimitWindow.RollingHour);
    }
}
