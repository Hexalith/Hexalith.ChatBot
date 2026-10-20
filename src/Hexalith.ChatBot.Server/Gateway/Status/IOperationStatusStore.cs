namespace Hexalith.ChatBot.Server.Gateway.Status;

internal interface IOperationStatusStore
{
    ValueTask UpsertAsync(OperationStatusRecord record, CancellationToken cancellationToken);

    /// <summary>Applies a pure transform to the latest snapshot on every atomic retry. Null skips the write.</summary>
    ValueTask UpdateAsync(string tenantId, string operationId,
        Func<OperationStatusRecord?, OperationStatusRecord?> update, CancellationToken cancellationToken);

    ValueTask<OperationStatusRecord?> TryGetAsync(string tenantId, string operationId, CancellationToken cancellationToken);
}
