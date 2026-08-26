using System.Security.Claims;

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
        string protectedMarker = marker.Create(context, "project-alpha", nameof(RecordProjectConversationMessage));
        CommandEnvelope envelope = Envelope(protectedMarker);

        marker.IsValid(envelope).ShouldBeTrue();

        marker.IsValid(envelope with { AggregateId = "project-beta" }).ShouldBeFalse();
        marker.IsValid(envelope with { CommandType = nameof(Hexalith.ChatBot.Contracts.Commands.ProposeAIAction) }).ShouldBeFalse();
        marker.IsValid(envelope with { CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV" }).ShouldBeFalse();
        marker.IsValid(envelope with { MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ" }).ShouldBeFalse();
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

    private static CommandEnvelope Envelope(string protectedMarker)
        => new(
            CommandId,
            "tenant-alpha",
            "chatbot",
            "project-alpha",
            nameof(RecordProjectConversationMessage),
            "{}"u8.ToArray(),
            CorrelationId,
            null,
            "actor-alpha",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [DataProtectionChatBotAdmissionMarker.ExtensionKey] = protectedMarker,
            });
}
