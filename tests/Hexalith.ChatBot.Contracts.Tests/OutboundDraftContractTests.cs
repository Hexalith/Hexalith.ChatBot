using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class OutboundDraftContractTests
{
    [Fact]
    public static void CreateOutboundDraftShouldSerializeFiniteDraftOnlyAuthorityAndDefaultSchema()
    {
        CreateOutboundDraft command = Command();

        string json = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"senderAuthorityClass\":\"draft-only\"");
        json.ShouldContain("\"schemaVersion\":\"chatbot.outbound-draft.v1\"");
        command.ShouldBeAssignableTo<IChatBotCommand>();
        command.SenderAuthorityClass.ShouldBe(SenderAuthorityClass.DraftOnly);
    }

    [Fact]
    public static void CreateOutboundDraftContractShouldNotExposeSecretBearingInfrastructureProperties()
    {
        string[] blockedNameFragments =
        [
            "AccessToken",
            "RefreshToken",
            "RawClaim",
            "RawJwt",
            "ProviderPayload",
            "RawHeader",
            "MailboxName",
            "RecipientDisplayName",
            "ProjectName",
        ];
        Type[] contractTypes = [typeof(CreateOutboundDraft), typeof(OutboundDraftContent)];

        foreach (Type contractType in contractTypes)
        {
            string[] propertyNames = contractType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(static property => property.Name)
                .ToArray();

            foreach (string blocked in blockedNameFragments)
            {
                propertyNames.ShouldNotContain(name => name.Contains(blocked, StringComparison.Ordinal), contractType.Name);
            }
        }
    }

    [Fact]
    public static void CreateOutboundDraftShouldKeepRefsSafeAndContentGoverned()
    {
        CreateOutboundDraft command = Command();

        command.RecipientRefs.ShouldBe(["recipient:party-001"]);
        command.ContextRefs.ShouldBe(["conversation:conv-001", "source-message:msg-001", "file:file-001"]);
        command.GovernedContent.ContentRedactionState.ShouldBe("governed_content");
        command.HasM365SendPosture.ShouldBeFalse();
    }

    private static CreateOutboundDraft Command()
        => new(
            "draft-001",
            "project-001",
            "requester-001",
            "actor-001",
            "conv-001",
            "msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001", "file:file-001"],
            "policy-snap-001",
            "correlation-001",
            new OutboundDraftContent("Status update", "Governed draft content.", "text/plain"));
}
