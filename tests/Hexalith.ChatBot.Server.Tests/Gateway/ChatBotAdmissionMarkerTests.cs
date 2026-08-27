using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.AspNetCore.DataProtection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed class ChatBotAdmissionMarkerTests
{
    private const string CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public void ProductionAdmissionMarkerShouldRoundTripExactInternalFollowUpIdentity()
    {
        DataProtectionChatBotAdmissionMarker marker = new(new EphemeralDataProtectionProvider());
        ChatBotGatewayContext context = Context();
        JsonElement payload = JsonSerializer.SerializeToElement(context.Submission.Request.Command);
        string protectedMarker = marker.Create(
            CommandId,
            "tenant-alpha",
            "project-alpha",
            nameof(RecordProjectConversationMessage),
            payload,
            CorrelationId,
            "actor-alpha",
            "ui",
            "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        CommandEnvelope envelope = Envelope(protectedMarker, JsonSerializer.SerializeToUtf8Bytes(payload));

        marker.IsValid(envelope).ShouldBeTrue();

        marker.IsValid(envelope with { AggregateId = "project-beta" }).ShouldBeFalse();
        marker.IsValid(envelope with { CommandType = nameof(Hexalith.ChatBot.Contracts.Commands.ProposeAIAction) }).ShouldBeFalse();
        marker.IsValid(envelope with { CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV" }).ShouldBeFalse();
        marker.IsValid(envelope with { MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ" }).ShouldBeFalse();
        marker.IsValid(envelope with { Payload = "{}"u8.ToArray() }).ShouldBeFalse();
        marker.IsValid(envelope with { Extensions = Rebind(envelope.Extensions!, "actorId", "actor-beta") }).ShouldBeFalse();
        marker.IsValid(envelope with { Extensions = Rebind(envelope.Extensions!, "surfaceOrigin", "api") }).ShouldBeFalse();
        marker.IsValid(envelope with { Extensions = Rebind(envelope.Extensions!, "taskId", "01ARZ3NDEKTSV4RRFFQ69G5FAV") }).ShouldBeFalse();
        marker.IsValid(envelope with { Extensions = null }).ShouldBeFalse();
    }

    private static ChatBotGatewayContext Context()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test"));
        ChatBotCommandSubmission submission = new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = CommandId,
                CommandType = nameof(RecordProjectConversationMessage),
                Command = new RecordProjectConversationMessage(
                    "project-alpha",
                    "message-alpha",
                    "fingerprint-alpha",
                    12,
                    "en-US",
                    0,
                    CorrelationId),
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CorrelationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            ChatBotSurfaceOrigin.Ui);
        return new ChatBotGatewayContext(
            submission,
            new ChatBotAuthenticatedActor("actor-alpha", principal),
            new ChatBotTenantBinding("tenant-alpha"));
    }

    private static CommandEnvelope Envelope(string protectedMarker, byte[] payload)
        => new(
            CommandId,
            "tenant-alpha",
            "chatbot",
            "project-alpha",
            nameof(RecordProjectConversationMessage),
            payload,
            CorrelationId,
            null,
            "actor-alpha",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [DataProtectionChatBotAdmissionMarker.ExtensionKey] = protectedMarker,
                ["actorId"] = "actor-alpha",
                ["surfaceOrigin"] = "ui",
                ["taskId"] = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            });

    private static Dictionary<string, string> Rebind(
        IReadOnlyDictionary<string, string> extensions,
        string key,
        string value)
    {
        Dictionary<string, string> rebound = new(extensions, StringComparer.Ordinal)
        {
            [key] = value,
        };
        return rebound;
    }
}
