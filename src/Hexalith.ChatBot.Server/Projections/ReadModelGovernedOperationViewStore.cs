using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Production governed operation projection store backed by the platform read-model store.
/// </summary>
internal sealed class ReadModelGovernedOperationViewStore(IReadModelStore store) : IGovernedOperationProjectionStore
{
    public async Task<GovernedOperationView?> GetAsync(string tenantId, string noteId, CancellationToken cancellationToken = default)
        => (await store
            .GetAsync<GovernedOperationView>(
                ChatBotReadModelStoreNames.StateStoreName,
                GovernedOperationView.KeyFor(tenantId, noteId),
                cancellationToken)
            .ConfigureAwait(false)).Value;

    public async Task SaveAsync(GovernedOperationView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        _ = await ReadModelWritePolicy
            .UpdateAsync<GovernedOperationView>(
                store,
                ChatBotReadModelStoreNames.StateStoreName,
                GovernedOperationView.KeyFor(view.TenantId, view.NoteId),
                // Guard the optimistic-concurrency retry against lost updates: if a concurrent projection advanced
                // the persisted view to a higher source version, keep it rather than re-applying the stale view on
                // retry. The handler already drops lower-or-equal versions, so an equal version never reaches here.
                current => current is not null && current.SourceVersion > view.SourceVersion ? current : view,
                new ReadModelWriteContext(Category: nameof(GovernedOperationView), ProjectionType: nameof(GovernedOperationProjectionHandler)),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
