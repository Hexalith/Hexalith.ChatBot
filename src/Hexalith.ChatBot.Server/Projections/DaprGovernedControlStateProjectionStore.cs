using Dapr.Client;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class DaprGovernedControlStateProjectionStore(DaprClient daprClient) : IGovernedControlStateProjectionStore
{
    public async Task<GovernedControlStateView?> GetAsync(
        string tenantId,
        string subjectClass,
        string subjectRef,
        CancellationToken cancellationToken = default)
        => await daprClient
            .GetStateAsync<GovernedControlStateView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                GovernedControlStateView.KeyFor(tenantId, subjectClass, subjectRef),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

    public async Task SaveAsync(GovernedControlStateView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                GovernedControlStateView.KeyFor(view.TenantId, view.SubjectClass, view.SubjectRef),
                view,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
