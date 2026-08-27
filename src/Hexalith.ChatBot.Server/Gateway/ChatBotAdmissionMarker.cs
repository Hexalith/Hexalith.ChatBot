using System.Security.Cryptography;
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
    string Create(
        string messageId,
        string tenantId,
        string aggregateId,
        string commandType,
        JsonElement payload,
        string correlationId,
        string actorId,
        string surfaceOrigin,
        string? taskId);

    bool IsValid(CommandEnvelope command);
}

internal sealed class DataProtectionChatBotAdmissionMarker(IDataProtectionProvider dataProtectionProvider) : IChatBotAdmissionMarker
{
    internal const string ExtensionKey = "chatbot.admission.v1";

    private static readonly TimeSpan MaximumMarkerAge = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaximumClockSkew = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDataProtector _protector = dataProtectionProvider
        .CreateProtector("Hexalith.ChatBot.Server.Gateway.AdmissionMarker.v1");

    public string Create(
        string messageId,
        string tenantId,
        string aggregateId,
        string commandType,
        JsonElement payload,
        string correlationId,
        string actorId,
        string surfaceOrigin,
        string? taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(surfaceOrigin);

        ChatBotAdmissionMarkerPayload marker = new(
            messageId,
            tenantId,
            ChatBotEventStore.DomainName,
            aggregateId,
            commandType,
            correlationId,
            actorId,
            surfaceOrigin,
            taskId,
            PayloadDigest(payload),
            DateTimeOffset.UtcNow);

        return _protector.Protect(JsonSerializer.Serialize(marker, JsonOptions));
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

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string? actorId = Extension(command, "actorId");
        string? surfaceOrigin = Extension(command, "surfaceOrigin");
        string? taskId = Extension(command, "taskId");
        return payload.IssuedAtUtc >= now.Subtract(MaximumMarkerAge) &&
            payload.IssuedAtUtc <= now.Add(MaximumClockSkew) &&
            string.Equals(payload.CommandId, command.MessageId, StringComparison.Ordinal) &&
            string.Equals(payload.TenantId, command.TenantId, StringComparison.Ordinal) &&
            string.Equals(payload.Domain, command.Domain, StringComparison.Ordinal) &&
            string.Equals(payload.AggregateId, command.AggregateId, StringComparison.Ordinal) &&
            string.Equals(payload.CommandType, command.CommandType, StringComparison.Ordinal) &&
            string.Equals(payload.CorrelationId, command.CorrelationId, StringComparison.Ordinal) &&
            string.Equals(payload.ActorId, actorId, StringComparison.Ordinal) &&
            string.Equals(payload.SurfaceOrigin, surfaceOrigin, StringComparison.Ordinal) &&
            string.Equals(payload.TaskId, taskId, StringComparison.Ordinal) &&
            string.Equals(payload.PayloadDigest, PayloadDigest(command.Payload), StringComparison.Ordinal);
    }

    private static string? Extension(CommandEnvelope command, string key)
        => command.Extensions is not null && command.Extensions.TryGetValue(key, out string? value)
            ? AuditMetadata.SafeOptionalToken(value)
            : null;

    private static string PayloadDigest(JsonElement payload)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload)));

    private static string PayloadDigest(byte[] payload)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            return PayloadDigest(document.RootElement);
        }
        catch (JsonException)
        {
            return Convert.ToHexString(SHA256.HashData(payload));
        }
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
        string? TaskId,
        string PayloadDigest,
        DateTimeOffset IssuedAtUtc);
}
