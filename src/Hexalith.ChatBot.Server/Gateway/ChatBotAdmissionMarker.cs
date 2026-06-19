using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.AspNetCore.DataProtection;

namespace Hexalith.ChatBot.Server.Gateway;

internal interface IChatBotAdmissionMarker
{
    string Create(ChatBotGatewayContext context, string aggregateId, string commandType);

    bool IsValid(CommandEnvelope command);
}

internal sealed class DataProtectionChatBotAdmissionMarker(IDataProtectionProvider dataProtectionProvider) : IChatBotAdmissionMarker
{
    internal const string ExtensionKey = "chatbot.admission.v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDataProtector _protector = dataProtectionProvider
        .CreateProtector("Hexalith.ChatBot.Server.Gateway.AdmissionMarker.v1");

    public string Create(ChatBotGatewayContext context, string aggregateId, string commandType)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);

        ChatBotAdmissionMarkerPayload payload = new(
            context.Submission.Request.CommandId,
            context.TenantBinding.TenantId,
            ChatBotEventStore.DomainName,
            aggregateId,
            commandType,
            context.Submission.CorrelationId,
            context.Actor.ActorId,
            ChatBotSurfaceOrigins.ToWireValue(context.Submission.Origin),
            context.Submission.TaskId);

        return _protector.Protect(JsonSerializer.Serialize(payload, JsonOptions));
    }

    public bool IsValid(CommandEnvelope command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.Extensions is null ||
            !command.Extensions.TryGetValue(ExtensionKey, out string? protectedPayload) ||
            string.IsNullOrWhiteSpace(protectedPayload))
        {
            return false;
        }

        ChatBotAdmissionMarkerPayload? payload;
        try
        {
            string json = _protector.Unprotect(protectedPayload);
            payload = JsonSerializer.Deserialize<ChatBotAdmissionMarkerPayload>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or JsonException)
        {
            return false;
        }

        if (payload is null)
        {
            return false;
        }

        return string.Equals(payload.CommandId, command.MessageId, StringComparison.Ordinal) &&
            string.Equals(payload.TenantId, command.TenantId, StringComparison.Ordinal) &&
            string.Equals(payload.Domain, command.Domain, StringComparison.Ordinal) &&
            string.Equals(payload.AggregateId, command.AggregateId, StringComparison.Ordinal) &&
            string.Equals(payload.CommandType, command.CommandType, StringComparison.Ordinal) &&
            string.Equals(payload.CorrelationId, command.CorrelationId, StringComparison.Ordinal) &&
            AuditMetadata.SafeOptionalToken(payload.SurfaceOrigin) is not null &&
            (payload.TaskId is null || AuditMetadata.SafeOptionalToken(payload.TaskId) is not null);
    }

    private sealed record ChatBotAdmissionMarkerPayload(
        string CommandId,
        string TenantId,
        string Domain,
        string AggregateId,
        string CommandType,
        string CorrelationId,
        string ActorId,
        string SurfaceOrigin,
        string? TaskId);
}
