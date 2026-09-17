using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedServiceClientRateLimitProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IServiceClientRateLimitProvider
{
    public async ValueTask<ServiceClientRateLimitState?> GetRateLimitAsync(string tenantId, string serviceClientId, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.ServiceClient, serviceClientId, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.IsStale(view, clock.UtcNow)
            ? new ServiceClientRateLimitState(1, ServiceClientRateLimitWindow.RollingHour)
            : view?.RateLimitBudget is null ? null : new ServiceClientRateLimitState(view.RateLimitBudget.Value, ServiceClientRateLimitWindow.RollingHour);
    }
}
