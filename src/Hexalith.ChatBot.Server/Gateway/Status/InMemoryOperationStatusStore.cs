using System.Collections.Concurrent;

namespace Hexalith.ChatBot.Server.Gateway.Status;

internal sealed class InMemoryOperationStatusStore : IOperationStatusStore
{
    private readonly ConcurrentDictionary<OperationStatusKey, OperationStatusRecord> _records = [];

    public ValueTask UpsertAsync(OperationStatusRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        _ = _records.AddOrUpdate(new OperationStatusKey(record.TenantId, record.OperationId),
            _ => OperationStatusUpdatePolicy.Apply(null, record),
            (_, current) => OperationStatusUpdatePolicy.Apply(current, record));
        return ValueTask.CompletedTask;
    }

    public ValueTask<OperationStatusRecord?> TryGetAsync(string tenantId, string operationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = _records.TryGetValue(new OperationStatusKey(tenantId, operationId), out OperationStatusRecord? record);
        return ValueTask.FromResult(record is null ? null : OperationStatusUpdatePolicy.Snapshot(record));
    }


}
