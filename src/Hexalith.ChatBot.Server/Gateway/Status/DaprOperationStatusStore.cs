using Dapr.Client;

using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Status;

/// <summary>
/// Production operation-status store backed by the DAPR chatbot state store.
/// </summary>
internal sealed class DaprOperationStatusStore(DaprClient daprClient) : IOperationStatusStore
{
    public async ValueTask UpsertAsync(OperationStatusRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        await daprClient
            .SaveStateAsync(
                DaprGovernedOperationViewStore.StateStoreName,
                KeyFor(record.TenantId, record.OperationId),
                record,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<OperationStatusRecord?> TryGetAsync(
        string tenantId,
        string operationId,
        CancellationToken cancellationToken)
        => await daprClient
            .GetStateAsync<OperationStatusRecord?>(
                DaprGovernedOperationViewStore.StateStoreName,
                KeyFor(tenantId, operationId),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

    public static string KeyFor(string tenantId, string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        return $"{tenantId}:operation-status:{operationId}";
    }
}
