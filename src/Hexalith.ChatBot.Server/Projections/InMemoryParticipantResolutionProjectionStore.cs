using System.Collections.Concurrent;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class InMemoryParticipantResolutionProjectionStore : IParticipantResolutionProjectionStore
{
    private readonly ConcurrentDictionary<string, ParticipantResolutionView> _views = new(StringComparer.Ordinal);

    public Task<ParticipantResolutionView?> GetAsync(
        string tenantId,
        string resolutionId,
        string sourceParticipantId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = _views.TryGetValue(ParticipantResolutionView.KeyFor(tenantId, resolutionId, sourceParticipantId), out ParticipantResolutionView? view);
        return Task.FromResult(view);
    }

    public Task SaveAsync(ParticipantResolutionView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        cancellationToken.ThrowIfCancellationRequested();
        _views[ParticipantResolutionView.KeyFor(view.TenantId, view.ResolutionId, view.SourceParticipantId)] = view;
        return Task.CompletedTask;
    }
}
