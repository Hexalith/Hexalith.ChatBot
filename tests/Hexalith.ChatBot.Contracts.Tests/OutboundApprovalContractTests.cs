using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class OutboundApprovalContractTests
{
    [Fact]
    public static void OutboundApprovalCommandsShouldSerializeCanonicalApprovalAndAuthorityTokens()
    {
        RequestOutboundSendApproval request = ApprovalRequest();
        DecideOutboundApproval decision = ApprovalDecision();
        ExecuteApprovedOutboundDraft send = SendCommand();

        string requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        string decisionJson = JsonSerializer.Serialize(decision, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        string sendJson = JsonSerializer.Serialize(send, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        request.ShouldBeAssignableTo<IChatBotCommand>();
        decision.ShouldBeAssignableTo<IChatBotCommand>();
        send.ShouldBeAssignableTo<IChatBotCommand>();
        requestJson.ShouldContain("\"evidenceFreshness\":\"fresh\"");
        requestJson.ShouldContain("\"senderAuthorityClass\":\"authenticated-user send\"");
        decisionJson.ShouldContain("\"decision\":\"request-revision\"");
        sendJson.ShouldContain("\"schemaVersion\":\"chatbot.outbound-send.v1\"");
    }

    [Fact]
    public static void OutboundApprovalContentSnapshotShouldPreserveGovernedContentButExposeMetadataOnlyPublicState()
    {
        OutboundApprovalContentSnapshot snapshot = ApprovalRequest().ContentSnapshot;

        snapshot.ProposedContent.Subject.ShouldBe("Status update");
        snapshot.ProposedContent.ContentText.ShouldBe("Governed draft content.");
        snapshot.ApprovedContent.ShouldBeNull();
        snapshot.PublicRedactionState.ShouldBe("metadata_only");
        JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            .ShouldContain("\"publicRedactionState\":\"metadata_only\"");
    }

    [Fact]
    public static void OutboundApprovalContractsShouldNotExposeProviderPayloadsOrUnsafeDisplayNames()
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
            "SubjectDisplay",
            "Body",
        ];
        Type[] contractTypes =
        [
            typeof(RequestOutboundSendApproval),
            typeof(DecideOutboundApproval),
            typeof(ExecuteApprovedOutboundDraft),
            typeof(OutboundApprovalContentSnapshot),
        ];

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

    private static RequestOutboundSendApproval ApprovalRequest()
        => new(
            "approval-001",
            "draft-001",
            "project-001",
            "requester-001",
            "conv-001",
            "msg-001",
            "item-001",
            ["recipient:party-001"],
            ["conversation:conv-001", "source-message:msg-001", "file:file-001"],
            "policy-snap-001",
            "authorized",
            "ExecuteApprovedOutboundDraft",
            "chatbot-spine.v1",
            "metadata_only",
            new OutboundApprovalContentSnapshot(
                new OutboundDraftContent("Status update", "Governed draft content.", "text/plain"),
                null,
                "governed_content",
                null),
            SenderAuthorityClass.AuthenticatedUserSend,
            ApprovalEvidenceFreshness.Fresh,
            1,
            "correlation-001");

    private static DecideOutboundApproval ApprovalDecision()
        => new(
            "approval-001",
            "draft-001",
            "project-001",
            ApprovalDecisionKind.RequestRevision,
            "decision-001",
            2,
            "correlation-001");

    private static ExecuteApprovedOutboundDraft SendCommand()
        => new(
            "send-001",
            "approval-001",
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
            "ExecuteApprovedOutboundDraft",
            "chatbot-spine.v1",
            SenderAuthorityClass.AuthenticatedUserSend,
            ApprovalEvidenceFreshness.Fresh,
            3,
            1,
            "correlation-001");
}

