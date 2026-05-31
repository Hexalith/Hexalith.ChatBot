using System.Collections.Concurrent;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class InMemoryAssociationProjectionStore : IAssociationProjectionStore
{
    private readonly ConcurrentDictionary<string, AssociationCandidateView> _views = new(StringComparer.Ordinal);

    public Task<AssociationCandidateView?> GetAsync(
        string tenantId,
        string associationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = _views.TryGetValue(AssociationCandidateView.KeyFor(tenantId, associationId), out AssociationCandidateView? view);
        return Task.FromResult(view);
    }

    public Task SaveAsync(AssociationCandidateView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        cancellationToken.ThrowIfCancellationRequested();
        _views[AssociationCandidateView.KeyFor(view.TenantId, view.AssociationId)] = view;
        return Task.CompletedTask;
    }
}
