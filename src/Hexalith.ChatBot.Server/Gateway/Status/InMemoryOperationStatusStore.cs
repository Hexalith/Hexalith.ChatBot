using System.Collections.Concurrent;

namespace Hexalith.ChatBot.Server.Gateway.Status;

internal sealed class InMemoryOperationStatusStore : IOperationStatusStore
{
    private readonly ConcurrentDictionary<OperationStatusKey, OperationStatusRecord> _records = [];

    public ValueTask UpsertAsync(OperationStatusRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        return UpdateAsync(record.TenantId, record.OperationId, _ => record, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask UpdateAsync(string tenantId, string operationId,
        Func<OperationStatusRecord?, OperationStatusRecord?> update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        OperationStatusKey key = new(tenantId, operationId);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _records.TryGetValue(key, out OperationStatusRecord? current);
            OperationStatusRecord? incoming = update(current is null ? null : OperationStatusUpdatePolicy.Snapshot(current));
            cancellationToken.ThrowIfCancellationRequested();
            if (incoming is null) { return ValueTask.CompletedTask; }
            OperationStatusRecord next = OperationStatusUpdatePolicy.ApplyUpdate(tenantId, operationId, current, incoming);
            if (current is null ? _records.TryAdd(key, next) : _records.TryUpdate(key, next, current))
            {
                return ValueTask.CompletedTask;
            }
        }
    }

    public ValueTask<OperationStatusRecord?> TryGetAsync(string tenantId, string operationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = _records.TryGetValue(new OperationStatusKey(tenantId, operationId), out OperationStatusRecord? record);
        return ValueTask.FromResult(record is null ? null : OperationStatusUpdatePolicy.Snapshot(record));
    }


}
