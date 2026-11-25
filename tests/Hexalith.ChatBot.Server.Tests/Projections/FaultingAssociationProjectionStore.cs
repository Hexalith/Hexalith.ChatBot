using System.Text.Json;

using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Tests.Projections;

/// <summary>Fails the first association write before recording the redelivery.</summary>
internal sealed class FaultingAssociationProjectionStore : IAssociationProjectionStore
{
    private readonly InMemoryAssociationProjectionStore _inner = new();
    private int _attempts;

    /// <inheritdoc/>
    public Task<AssociationCandidateView?> GetAsync(string tenantId, string associationId, CancellationToken cancellationToken)
        => _inner.GetAsync(tenantId, associationId, cancellationToken);

    /// <inheritdoc/>
    public Task SaveAsync(AssociationCandidateView view, CancellationToken cancellationToken)
        => Interlocked.Increment(ref _attempts) == 1
            ? throw new JsonException("Simulated association store write fault.")
            : _inner.SaveAsync(view, cancellationToken);
}
