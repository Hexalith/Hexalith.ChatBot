using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedAiActorRateLimitProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IAiActorRateLimitProvider
{
    public async ValueTask<AiActorRateLimitState?> GetRateLimitAsync(string tenantId, string aiActorId, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.AiActor, aiActorId, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.IsStale(view, clock.UtcNow)
            ? new AiActorRateLimitState(1, AiActorRateLimitWindow.RollingHour)
            : view?.RateLimitBudget is null ? null : new AiActorRateLimitState(view.RateLimitBudget.Value, AiActorRateLimitWindow.RollingHour);
    }
}
