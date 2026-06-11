using System.Collections.Concurrent;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class InMemoryGovernedControlStateProjectionStore : IGovernedControlStateProjectionStore
{
    private readonly ConcurrentDictionary<string, GovernedControlStateView> _views = new(StringComparer.Ordinal);

    public Task<GovernedControlStateView?> GetAsync(
        string tenantId,
        string subjectClass,
        string subjectRef,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = _views.TryGetValue(GovernedControlStateView.KeyFor(tenantId, subjectClass, subjectRef), out GovernedControlStateView? view);
        return Task.FromResult(view);
    }

    public Task SaveAsync(GovernedControlStateView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        cancellationToken.ThrowIfCancellationRequested();
        _views[GovernedControlStateView.KeyFor(view.TenantId, view.SubjectClass, view.SubjectRef)] = view;
        return Task.CompletedTask;
    }
}
