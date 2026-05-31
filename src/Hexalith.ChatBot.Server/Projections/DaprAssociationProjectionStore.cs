using Dapr.Client;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Production association routing/progress projection store backed by the DAPR <c>chatbot-statestore</c>.
/// </summary>
internal sealed class DaprAssociationProjectionStore(DaprClient daprClient) : IAssociationProjectionStore
{
    public async Task<AssociationCandidateView?> GetAsync(
        string tenantId,
        string associationId,
        CancellationToken cancellationToken = default)
        => await daprClient
            .GetStateAsync<AssociationCandidateView?>(
                DaprGovernedOperationViewStore.StateStoreName,
                AssociationCandidateView.KeyFor(tenantId, associationId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

    public async Task SaveAsync(AssociationCandidateView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                AssociationCandidateView.KeyFor(view.TenantId, view.AssociationId),
                view,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
