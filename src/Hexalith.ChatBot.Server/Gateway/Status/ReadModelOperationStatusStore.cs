using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Status;

/// <summary>
/// Production operation-status store backed by the platform read-model store.
/// </summary>
internal sealed class ReadModelOperationStatusStore(IReadModelStore store) : IOperationStatusStore
{
    public async ValueTask UpsertAsync(OperationStatusRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        _ = await ReadModelWritePolicy
            .UpdateAsync<OperationStatusRecord>(
                store,
                ChatBotReadModelStoreNames.StateStoreName,
                KeyFor(record.TenantId, record.OperationId),
                _ => record,
                new ReadModelWriteContext(Category: nameof(OperationStatusRecord), CorrelationId: record.CorrelationId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<OperationStatusRecord?> TryGetAsync(
        string tenantId,
        string operationId,
        CancellationToken cancellationToken)
        => (await store
            .GetAsync<OperationStatusRecord>(
                ChatBotReadModelStoreNames.StateStoreName,
                KeyFor(tenantId, operationId),
                cancellationToken)
            .ConfigureAwait(false)).Value;

    public static string KeyFor(string tenantId, string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        return $"{tenantId}:operation-status:{operationId}";
    }
}
