using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ProjectionBackedAiActorControlStateProvider(
    IGovernedControlStateProjectionStore store,
    ISystemClock clock) : IAiActorControlStateProvider
{
    public async ValueTask<AiActorControlState> GetControlStateAsync(string tenantId, string aiActorId, CancellationToken cancellationToken)
    {
        GovernedControlStateView? view = await store.GetAsync(tenantId, GovernedControlSubjectClasses.AiActor, aiActorId, cancellationToken).ConfigureAwait(false);
        return GovernedProjectionProviderHelpers.AiActorState(view, clock.UtcNow);
    }
}
