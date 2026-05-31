using System.Text.Json;

using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class EventStoreCorrectionPropagationCommandWriter(
    IEventStoreGatewayClient eventStore) : ICorrectionPropagationCommandWriter
{
    public async ValueTask SubmitAsync<TCommand>(
        CorrectionPropagationRequest request,
        string commandType,
        TCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        ArgumentNullException.ThrowIfNull(command);

        SubmitCommandRequest submit = new(
            MessageId: $"{request.WorkflowInstanceId}:{commandType}:{DeterministicSuffix(command)}",
            Tenant: request.TenantId,
            Domain: ChatBotEventStore.DomainName,
            AggregateId: request.AssociationId,
            CommandType: commandType,
            Payload: JsonSerializer.SerializeToElement(command),
            CorrelationId: request.CorrelationId,
            Extensions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["surfaceOrigin"] = "workflow",
                ["actorType"] = "system",
                ["workflowInstanceId"] = request.WorkflowInstanceId,
            });

        _ = await eventStore.SubmitCommandAsync(submit, cancellationToken).ConfigureAwait(false);
    }

    private static string DeterministicSuffix<TCommand>(TCommand command)
        => command switch
        {
            Association.AcknowledgeMailboxAssociationCorrectionStoreInvalidated ack => ack.StoreKey,
            _ => typeof(TCommand).Name,
        };
}
