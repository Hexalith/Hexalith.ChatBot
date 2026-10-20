using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Status;

/// <summary>
/// Production operation-status store backed by the platform read-model store.
/// </summary>
internal sealed class ReadModelOperationStatusStore(IReadModelStore store) : IOperationStatusStore
{
    public ValueTask UpsertAsync(OperationStatusRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        return UpdateAsync(record.TenantId, record.OperationId, _ => record, cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask UpdateAsync(string tenantId, string operationId,
        Func<OperationStatusRecord?, OperationStatusRecord?> update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        try
        {
            _ = await ReadModelWritePolicy
            .UpdateAsync<OperationStatusRecord>(
                store,
                ChatBotReadModelStoreNames.StateStoreName,
                KeyFor(tenantId, operationId),
                current =>
                {
                    OperationStatusRecord? incoming = update(current is null ? null : OperationStatusUpdatePolicy.Snapshot(current));
                    cancellationToken.ThrowIfCancellationRequested();
                    return incoming is null ? throw new OperationStatusUpdateSkippedException()
                        : OperationStatusUpdatePolicy.ApplyUpdate(tenantId, operationId, current, incoming);
                },
                new ReadModelWriteContext(Category: nameof(OperationStatusRecord)),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        }
        catch (OperationStatusUpdateSkippedException)
        {
            // The SDK has no nullable transform: stop before TrySaveAsync for an intentional no-op.
        }
    }

    public async ValueTask<OperationStatusRecord?> TryGetAsync(
        string tenantId,
        string operationId,
        CancellationToken cancellationToken)
    {
        OperationStatusRecord? record = (await store
            .GetAsync<OperationStatusRecord>(
                ChatBotReadModelStoreNames.StateStoreName,
                KeyFor(tenantId, operationId),
                cancellationToken)
            .ConfigureAwait(false)).Value;
        return record is null ? null : OperationStatusUpdatePolicy.Snapshot(record);
    }

    public static string KeyFor(string tenantId, string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        return $"{tenantId}:operation-status:{operationId}";
    }
}
