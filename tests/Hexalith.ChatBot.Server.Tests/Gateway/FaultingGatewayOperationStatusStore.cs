using Hexalith.ChatBot.Server.Gateway.Status;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Injects an initial status-write failure while allowing the replay path to restore state.</summary>
internal sealed class FaultingGatewayOperationStatusStore(Action beforeInitialUpdate) : IOperationStatusStore
{
    private readonly InMemoryOperationStatusStore _inner = new();

    /// <inheritdoc/>
    public ValueTask UpsertAsync(OperationStatusRecord record, CancellationToken cancellationToken)
    {
        return _inner.UpsertAsync(record, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask UpdateAsync(
        string tenantId,
        string operationId,
        Func<OperationStatusRecord?, OperationStatusRecord?> update,
        CancellationToken cancellationToken)
        => _inner.UpdateAsync(tenantId, operationId, current =>
        {
            OperationStatusRecord? next = update(current);
            if (next is { PriorOutcome: null })
            {
                beforeInitialUpdate();
            }
            return next;
        }, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<OperationStatusRecord?> TryGetAsync(string tenantId, string operationId, CancellationToken cancellationToken)
        => _inner.TryGetAsync(tenantId, operationId, cancellationToken);
}
