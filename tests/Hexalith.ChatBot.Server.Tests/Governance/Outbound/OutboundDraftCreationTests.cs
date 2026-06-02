using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.Outbound;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Governance.Outbound;

public static class OutboundDraftCreationTests
{
    [Fact]
    public static void DraftAuthorityShouldAllowOnlyProjectScopeAndTenantPolicyWithNoSendPosture()
    {
        var result = OutboundDraftAuthorityEvaluator.Classify(Command(), Principal(), "tenant-alpha");

        result.DenialReason.ShouldBeNull();
        result.AuthorityClass.ShouldBe(SenderAuthorityClass.DraftOnly);
        result.AuditEvidenceRefs.ShouldContain("sender-authority:draft-only");
        result.AuditEvidenceRefs.ShouldContain("project-authority:outbound-draft");
    }

    [Fact]
    public static void DraftAuthorityShouldDenyMissingProjectAuthorityAsInsufficientAuthority()
    {
        CreateOutboundDraft command = Command();
        ClaimsPrincipal principal = Principal(project: "project-other");

        var result = OutboundDraftAuthorityEvaluator.Classify(command, principal, "tenant-alpha");

        result.DenialReason.ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
        OutboundDraftAuthorityEvaluator.SafeDenialReason(command, principal, result)
            .ShouldBe(ChatBotDisabledActionReasons.InsufficientAuthority);
    }

    [Fact]
    public static void DraftAuthorityShouldDenyMissingScopeAsInsufficientAuthority()
    {
        CreateOutboundDraft command = Command();
        ClaimsPrincipal principal = Principal(includeScope: false);

        var result = OutboundDraftAuthorityEvaluator.Classify(command, principal, "tenant-alpha");

        result.DenialReason.ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
        OutboundDraftAuthorityEvaluator.SafeDenialReason(command, principal, result)
            .ShouldBe(ChatBotDisabledActionReasons.InsufficientAuthority);
    }

    [Fact]
    public static void DraftAuthorityShouldDenySendPostureOrPolicyBlockAsPolicyBlocked()
    {
        CreateOutboundDraft command = Command() with { HasM365SendPosture = true };
        ClaimsPrincipal principal = Principal();

        var sendPosture = OutboundDraftAuthorityEvaluator.Classify(command, principal, "tenant-alpha");
        var tenantPolicy = OutboundDraftAuthorityEvaluator.Classify(Command(), Principal(includePolicy: false), "tenant-alpha");

        sendPosture.DenialReason.ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
        tenantPolicy.DenialReason.ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
        OutboundDraftAuthorityEvaluator.SafeDenialReason(command, principal, sendPosture)
            .ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
    }

    internal static CreateOutboundDraft Command()
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

    private static ClaimsPrincipal Principal(
        string project = "project-001",
        bool includeScope = true,
        bool includePolicy = true)
    {
        List<Claim> claims =
        [
            new(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
            new(ParticipantAuthorizationStage.ProjectOwnerClaim, project),
        ];
        if (includeScope)
        {
            claims.Add(new Claim(OutboundDraftAuthorityEvaluator.ProjectScopeClaim, "project-001:outbound-draft"));
        }

        if (includePolicy)
        {
            claims.Add(new Claim(OutboundDraftAuthorityEvaluator.TenantOutboundPolicyClaim, "draft-only"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
