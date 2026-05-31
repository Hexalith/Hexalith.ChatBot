using System.Collections.Concurrent;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// In-memory, tenant-partitioned governed operation projection store. This is the M0 default (mirroring the
/// sibling Folders projection default); the DAPR <c>chatbot-statestore</c>-backed
/// <see cref="DaprGovernedOperationViewStore"/> is the production swap. Keys are tenant-prefixed so a second
/// tenant is additive and no record is shared across tenants.
/// </summary>
internal sealed class InMemoryGovernedOperationProjectionStore : IGovernedOperationProjectionStore
{
    private readonly ConcurrentDictionary<string, GovernedOperationView> _views = new(StringComparer.Ordinal);

    public Task<GovernedOperationView?> GetAsync(string tenantId, string noteId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = _views.TryGetValue(GovernedOperationView.KeyFor(tenantId, noteId), out GovernedOperationView? view);
        return Task.FromResult(view);
    }

    public Task SaveAsync(GovernedOperationView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        cancellationToken.ThrowIfCancellationRequested();
        _views[GovernedOperationView.KeyFor(view.TenantId, view.NoteId)] = view;
        return Task.CompletedTask;
    }
}
