using System.Text.Json;

using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Submits trusted workflow commands with an envelope-bound admission marker.</summary>
internal sealed class EventStoreCorrectionPropagationCommandWriter(
    IEventStoreGatewayClient eventStore,
    IChatBotAdmissionMarker admissionMarker) : ICorrectionPropagationCommandWriter
{
    /// <inheritdoc/>
    public async ValueTask SubmitAsync<TCommand>(
        CorrectionPropagationRequest request,
        string commandType,
        TCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        ArgumentNullException.ThrowIfNull(command);

        string messageId = $"{request.WorkflowInstanceId}:{commandType}:{DeterministicSuffix(command)}";
        const string actorId = "correction-propagation-workflow";
        JsonElement payload = JsonSerializer.SerializeToElement(command);
        SubmitCommandRequest submit = new(
            MessageId: messageId,
            Tenant: request.TenantId,
            Domain: ChatBotEventStore.DomainName,
            AggregateId: request.AssociationId,
            CommandType: commandType,
            Payload: payload,
            CorrelationId: request.CorrelationId,
            Extensions: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["surfaceOrigin"] = "workflow",
                ["actorType"] = "system",
                ["actorId"] = actorId,
                [DataProtectionChatBotAdmissionMarker.ExtensionKey] = admissionMarker.Create(messageId, request.TenantId,
                    request.AssociationId, commandType, payload, request.CorrelationId, actorId, "workflow", null),
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
