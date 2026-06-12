using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Production association routing/progress projection store backed by the platform read-model store.
/// </summary>
internal sealed class ReadModelAssociationProjectionStore(IReadModelStore store) : IAssociationProjectionStore
{
    public async Task<AssociationCandidateView?> GetAsync(
        string tenantId,
        string associationId,
        CancellationToken cancellationToken = default)
        => (await store
            .GetAsync<AssociationCandidateView>(
                ChatBotReadModelStoreNames.StateStoreName,
                AssociationCandidateView.KeyFor(tenantId, associationId),
                cancellationToken)
            .ConfigureAwait(false)).Value;

    public async Task SaveAsync(AssociationCandidateView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        _ = await ReadModelWritePolicy
            .UpdateAsync<AssociationCandidateView>(
                store,
                ChatBotReadModelStoreNames.StateStoreName,
                AssociationCandidateView.KeyFor(view.TenantId, view.AssociationId),
                current => current is not null && current.SourceVersion > view.SourceVersion ? current : view,
                new ReadModelWriteContext(Category: nameof(AssociationCandidateView), ProjectionType: nameof(AssociationProjectionHandler), CorrelationId: view.CorrelationId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
