namespace Hexalith.ChatBot.Server.Gateway.Status;

internal interface IOperationStatusStore
{
    ValueTask UpsertAsync(OperationStatusRecord record, CancellationToken cancellationToken);

    ValueTask<OperationStatusRecord?> TryGetAsync(string tenantId, string operationId, CancellationToken cancellationToken);
}
