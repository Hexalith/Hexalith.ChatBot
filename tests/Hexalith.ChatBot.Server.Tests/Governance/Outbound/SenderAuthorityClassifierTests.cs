using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Governance.Outbound;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Governance.Outbound;

public static class SenderAuthorityClassifierTests
{
    [Theory]
    [MemberData(nameof(SuccessCases))]
    public static void ClassifierShouldMapSuccessfulAuthorityCases(
        string caseName,
        SenderAuthorityClass expectedClass,
        string expectedEvidenceRef)
    {
        SenderAuthorityClassificationRequest request = SuccessRequest(caseName);
        var result = SenderAuthorityClassifier.Classify(request);

        result.DenialReason.ShouldBeNull();
        result.AuthorityClass.ShouldBe(expectedClass);
        result.RequesterRef.ShouldBe("requester:actor-alpha");
        result.PolicySnapshotRef.ShouldBe("policy-snapshot:policy-alpha");
        result.AuditEvidenceRefs.ShouldContain($"sender-authority:{SenderAuthorityClasses.ToWireValue(expectedClass)}");
        result.AuditEvidenceRefs.ShouldContain(expectedEvidenceRef);
        AssertMetadataOnly(result);
    }

    [Fact]
    public static void SendOnBehalfBlockedByTenantPolicyShouldFailClosedWithMetadataOnlyReason()
    {
        var result = SenderAuthorityClassifier.Classify(SendOnBehalfRequest() with
        {
            TenantPolicy = Policy(allowSendOnBehalf: false),
        });

        result.AuthorityClass.ShouldBe(SenderAuthorityClass.SendOnBehalf);
        result.DenialReason.ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
        result.AuditEvidenceRefs.ShouldContain("sender-authority:send-on-behalf");
        result.AuditEvidenceRefs.ShouldContain("principal-for:owner-alpha");
        result.AuditEvidenceRefs.ShouldContain("policy-snapshot:policy-alpha");
        AssertMetadataOnly(result);
    }

    [Fact]
    public static void SendOnBehalfForDifferentDelegateShouldFailClosedWithDelegationMismatch()
    {
        var result = SenderAuthorityClassifier.Classify(SendOnBehalfRequest() with
        {
            Delegation = new SenderDelegationEvidence("actor-beta", "owner-alpha", false, "delegation:owner-alpha:actor-beta"),
        });

        result.AuthorityClass.ShouldBe(SenderAuthorityClass.SendOnBehalf);
        result.DenialReason.ShouldBe(SenderAuthorityConflictReasons.DelegationMismatch);
        result.RequesterRef.ShouldBe("requester:actor-alpha");
        result.PrincipalForRef.ShouldBe("principal-for:owner-alpha");
        result.AuditEvidenceRefs.ShouldContain("principal-for:owner-alpha");
        AssertMetadataOnly(result);
    }

    [Fact]
    public static void LapsedSharedMailboxMembershipShouldFailClosedWithoutDowngrading()
    {
        var result = SenderAuthorityClassifier.Classify(SharedMailboxRequest() with
        {
            SharedMailboxMembership = new SenderSharedMailboxMembershipEvidence(
                "shared-alpha",
                "actor-alpha",
                false,
                "membership:shared-alpha:actor-alpha"),
        });

        result.AuthorityClass.ShouldBe(SenderAuthorityClass.SharedMailboxSend);
        result.DenialReason.ShouldBe(SenderAuthorityConflictReasons.MembershipRevoked);
        result.MailboxRef.ShouldBe("mailbox:shared-alpha");
        result.AuditEvidenceRefs.ShouldContain("membership:shared-alpha:actor-alpha");
        result.AuditEvidenceRefs.ShouldContain("sender-authority:shared-mailbox send");
        AssertMetadataOnly(result);
    }

    [Fact]
    public static void ApprovedServiceSendWithoutOutboundGrantShouldFailClosedBeforeApprovalUse()
    {
        var result = SenderAuthorityClassifier.Classify(ServiceSendRequest() with
        {
            ServiceClientGrant = new SenderServiceClientGrantEvidence("service-alpha", false, "service-client:service-alpha"),
            ApprovalChain = new SenderApprovalChainEvidence("approval-alpha", true, "approval:approval-alpha"),
        });

        result.AuthorityClass.ShouldBe(SenderAuthorityClass.ApprovedServiceSend);
        result.DenialReason.ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
        result.ServiceClientRef.ShouldBe("service-client:service-alpha");
        result.ApprovalRef.ShouldBeNull();
        result.AuditEvidenceRefs.ShouldContain("service-client:service-alpha");
        result.AuditEvidenceRefs.ShouldNotContain("approval:approval-alpha");
        AssertMetadataOnly(result);
    }

    [Fact]
    public static void ApprovedServiceSendWithGrantShouldRequirePairedApproval()
    {
        var result = SenderAuthorityClassifier.Classify(ServiceSendRequest() with
        {
            ServiceClientGrant = new SenderServiceClientGrantEvidence("service-alpha", true, "service-client:service-alpha"),
            ApprovalChain = new SenderApprovalChainEvidence(null, false, "approval:approval-alpha"),
        });

        result.AuthorityClass.ShouldBe(SenderAuthorityClass.ApprovedServiceSend);
        result.DenialReason.ShouldBe(SenderAuthorityConflictReasons.ApprovalMissing);
        result.ServiceClientRef.ShouldBe("service-client:service-alpha");
        result.ApprovalRef.ShouldBeNull();
        AssertMetadataOnly(result);
    }

    [Fact]
    public static void ProviderPostureAloneShouldNotAssertAuthority()
    {
        var result = SenderAuthorityClassifier.Classify(AuthenticatedUserRequest() with
        {
            ProjectAuthority = ProjectAuthority(hasProjectAuthority: false, scopes: []),
        });

        result.AuthorityClass.ShouldBe(SenderAuthorityClass.AuthenticatedUserSend);
        result.DenialReason.ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
        result.AuditEvidenceRefs.ShouldContain("m365:own-mailbox-mail-send");
        AssertMetadataOnly(result);
    }

    public static TheoryData<string, SenderAuthorityClass, string> SuccessCases()
        =>
        [
            new("draft", SenderAuthorityClass.DraftOnly, "project-authority:outbound-draft"),
            new("authenticated-user", SenderAuthorityClass.AuthenticatedUserSend, "m365:own-mailbox-mail-send"),
            new("shared-mailbox", SenderAuthorityClass.SharedMailboxSend, "membership:shared-alpha:actor-alpha"),
            new("send-on-behalf", SenderAuthorityClass.SendOnBehalf, "delegation:owner-alpha:actor-alpha"),
            new("service-send", SenderAuthorityClass.ApprovedServiceSend, "approval:approval-alpha"),
        ];

    private static SenderAuthorityClassificationRequest SuccessRequest(string caseName)
        => caseName switch
        {
            "draft" => DraftRequest(),
            "authenticated-user" => AuthenticatedUserRequest(),
            "shared-mailbox" => SharedMailboxRequest(),
            "send-on-behalf" => SendOnBehalfRequest(),
            "service-send" => ServiceSendRequest(),
            _ => throw new ArgumentOutOfRangeException(nameof(caseName), caseName, null),
        };

    private static SenderAuthorityClassificationRequest DraftRequest()
        => BaseRequest(SenderAuthorityIntent.DraftOnly) with
        {
            ProjectAuthority = ProjectAuthority(scopes: ["outbound-draft"], evidenceRef: "project-authority:outbound-draft"),
        };

    private static SenderAuthorityClassificationRequest AuthenticatedUserRequest()
        => BaseRequest(SenderAuthorityIntent.AuthenticatedUserSend) with
        {
            M365Posture = new SenderM365Posture(
                MailboxId: "mailbox-alpha",
                IsMailboxOwner: true,
                HasOwnMailboxMailSend: true,
                HasSharedMailboxSendPosture: false,
                HasSendOnBehalfPosture: false,
                HasApplicationMailSend: false,
                EvidenceRef: "m365:own-mailbox-mail-send"),
            ProjectAuthority = ProjectAuthority(scopes: ["outbound-send"]),
        };

    private static SenderAuthorityClassificationRequest SharedMailboxRequest()
        => BaseRequest(SenderAuthorityIntent.SharedMailboxSend) with
        {
            M365Posture = new SenderM365Posture(
                MailboxId: "shared-alpha",
                IsMailboxOwner: false,
                HasOwnMailboxMailSend: false,
                HasSharedMailboxSendPosture: true,
                HasSendOnBehalfPosture: false,
                HasApplicationMailSend: false,
                EvidenceRef: "m365:shared-mailbox-send"),
            ProjectAuthority = ProjectAuthority(scopes: ["outbound-send"]),
            SharedMailboxMembership = new SenderSharedMailboxMembershipEvidence(
                "shared-alpha",
                "actor-alpha",
                true,
                "membership:shared-alpha:actor-alpha"),
        };

    private static SenderAuthorityClassificationRequest SendOnBehalfRequest()
        => BaseRequest(SenderAuthorityIntent.SendOnBehalf) with
        {
            M365Posture = new SenderM365Posture(
                MailboxId: "owner-alpha",
                IsMailboxOwner: false,
                HasOwnMailboxMailSend: false,
                HasSharedMailboxSendPosture: false,
                HasSendOnBehalfPosture: true,
                HasApplicationMailSend: false,
                EvidenceRef: "m365:send-on-behalf"),
            ProjectAuthority = ProjectAuthority(scopes: ["outbound-send"]),
            Delegation = new SenderDelegationEvidence("actor-alpha", "owner-alpha", false, "delegation:owner-alpha:actor-alpha"),
        };

    private static SenderAuthorityClassificationRequest ServiceSendRequest()
        => BaseRequest(SenderAuthorityIntent.ApprovedServiceSend) with
        {
            M365Posture = new SenderM365Posture(
                MailboxId: "service-mailbox-alpha",
                IsMailboxOwner: false,
                HasOwnMailboxMailSend: false,
                HasSharedMailboxSendPosture: false,
                HasSendOnBehalfPosture: false,
                HasApplicationMailSend: true,
                EvidenceRef: "m365:application-mail-send"),
            ProjectAuthority = ProjectAuthority(scopes: ["outbound-send"]),
            ServiceClientGrant = new SenderServiceClientGrantEvidence("service-alpha", true, "service-client:service-alpha"),
            ApprovalChain = new SenderApprovalChainEvidence("approval-alpha", true, "approval:approval-alpha"),
        };

    private static SenderAuthorityClassificationRequest BaseRequest(SenderAuthorityIntent intent)
        => new(
            intent,
            "tenant-alpha",
            "actor-alpha",
            "policy-alpha",
            Policy(),
            new SenderM365Posture(
                MailboxId: "mailbox-alpha",
                IsMailboxOwner: false,
                HasOwnMailboxMailSend: false,
                HasSharedMailboxSendPosture: false,
                HasSendOnBehalfPosture: false,
                HasApplicationMailSend: false,
                EvidenceRef: "m365:none"),
            ProjectAuthority(),
            null,
            null,
            null,
            null);

    private static SenderTenantOutboundPolicy Policy(
        bool allowDraftOnly = true,
        bool allowAuthenticatedUserSend = true,
        bool allowSharedMailboxSend = true,
        bool allowSendOnBehalf = true,
        bool allowApprovedServiceSend = true)
        => new(
            allowDraftOnly,
            allowAuthenticatedUserSend,
            allowSharedMailboxSend,
            allowSendOnBehalf,
            allowApprovedServiceSend);

    private static SenderProjectAuthorityEvidence ProjectAuthority(
        bool hasProjectAuthority = true,
        IReadOnlyList<string>? scopes = null,
        string evidenceRef = "project-authority:outbound-send")
        => new(hasProjectAuthority, scopes ?? ["outbound-send"], evidenceRef);

    private static void AssertMetadataOnly(object result)
    {
        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        string[] blocked =
        [
            "bearer",
            "accessToken",
            "refreshToken",
            "rawClaims",
            "providerPayload",
            "internetMessageHeaders",
            "messageBody",
            "recipient display",
            "Project Apollo",
            "Graph response",
            "token",
        ];

        foreach (string marker in blocked)
        {
            json.ShouldNotContain(marker, Case.Insensitive);
        }
    }
}
