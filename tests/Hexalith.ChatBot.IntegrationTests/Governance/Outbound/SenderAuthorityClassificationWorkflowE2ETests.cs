using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Governance.Outbound;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Governance.Outbound;

[Trait("Category", "E2E")]
public static class SenderAuthorityClassificationWorkflowE2ETests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [MemberData(nameof(SuccessfulAuthorityWorkflows))]
    public static void SuccessfulAuthorityWorkflowShouldRoundTripAsMetadataOnlyBoundaryPayload(
        string caseName,
        SenderAuthorityClass expectedClass,
        string expectedAuthorityRef,
        string expectedEvidenceRef)
    {
        SenderAuthorityClassificationRequest request = SuccessRequest(caseName);
        (SenderAuthorityClassificationResult result, string json) = ClassifyAndRoundTrip(request);

        result.DenialReason.ShouldBeNull();
        result.AuthorityClass.ShouldBe(expectedClass);
        result.RequesterRef.ShouldBe("requester:actor-alpha");
        result.PolicySnapshotRef.ShouldBe("policy-snapshot:policy-alpha");
        result.AuditEvidenceRefs.ShouldContain(expectedAuthorityRef);
        result.AuditEvidenceRefs.ShouldContain(expectedEvidenceRef);
        json.ShouldContain(SenderAuthorityClasses.ToWireValue(expectedClass));
        AssertMetadataOnly(json);
    }

    [Theory]
    [MemberData(nameof(FailClosedAuthorityWorkflows))]
    public static void DeniedAuthorityWorkflowShouldRoundTripStableReasonAndSafeAuditRefs(
        string caseName,
        SenderAuthorityClass expectedClass,
        string expectedDenialReason,
        string expectedEvidenceRef)
    {
        SenderAuthorityClassificationRequest request = FailClosedRequest(caseName);
        (SenderAuthorityClassificationResult result, string json) = ClassifyAndRoundTrip(request);

        result.AuthorityClass.ShouldBe(expectedClass);
        result.DenialReason.ShouldBe(expectedDenialReason);
        result.RequesterRef.ShouldBe("requester:actor-alpha");
        result.PolicySnapshotRef.ShouldBe("policy-snapshot:policy-alpha");
        result.AuditEvidenceRefs.ShouldContain($"sender-authority:{SenderAuthorityClasses.ToWireValue(expectedClass)}");
        result.AuditEvidenceRefs.ShouldContain(expectedEvidenceRef);
        json.ShouldContain(expectedDenialReason);
        AssertMetadataOnly(json);
    }

    [Fact]
    public static void ApplicationMailSendAndServiceGrantShouldStillRequirePairedApprovalAtBoundary()
    {
        SenderAuthorityClassificationRequest request = ServiceSendRequest() with
        {
            ServiceClientGrant = new SenderServiceClientGrantEvidence("service-alpha", true, "service-client:service-alpha"),
            ApprovalChain = new SenderApprovalChainEvidence(null, false, "approval:missing"),
        };

        (SenderAuthorityClassificationResult result, string json) = ClassifyAndRoundTrip(request);

        result.AuthorityClass.ShouldBe(SenderAuthorityClass.ApprovedServiceSend);
        result.DenialReason.ShouldBe(SenderAuthorityConflictReasons.ApprovalMissing);
        result.ServiceClientRef.ShouldBe("service-client:service-alpha");
        result.ApprovalRef.ShouldBeNull();
        json.ShouldContain("\"approvalRef\":null");
        json.ShouldNotContain("approval:approval-alpha");
        AssertMetadataOnly(json);
    }

    [Fact]
    public static void ApplicationMailSendAndApprovalShouldStillRequireOutboundServiceGrantAtBoundary()
    {
        SenderAuthorityClassificationRequest request = ServiceSendRequest() with
        {
            ServiceClientGrant = new SenderServiceClientGrantEvidence("service-alpha", false, "service-client:service-alpha"),
            ApprovalChain = new SenderApprovalChainEvidence("approval-alpha", true, "approval:approval-alpha"),
        };

        (SenderAuthorityClassificationResult result, string json) = ClassifyAndRoundTrip(request);

        result.AuthorityClass.ShouldBe(SenderAuthorityClass.ApprovedServiceSend);
        result.DenialReason.ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
        result.ServiceClientRef.ShouldBe("service-client:service-alpha");
        result.ApprovalRef.ShouldBeNull();
        result.AuditEvidenceRefs.ShouldContain("service-client:service-alpha");
        result.AuditEvidenceRefs.ShouldNotContain("approval:approval-alpha");
        AssertMetadataOnly(json);
    }

    [Fact]
    public static void ApprovedServiceSendWorkflowShouldCarryGrantAndApprovalEvidenceAtBoundary()
    {
        SenderAuthorityClassificationRequest request = ServiceSendRequest();

        (SenderAuthorityClassificationResult result, string json) = ClassifyAndRoundTrip(request);

        result.AuthorityClass.ShouldBe(SenderAuthorityClass.ApprovedServiceSend);
        result.DenialReason.ShouldBeNull();
        result.ServiceClientRef.ShouldBe("service-client:service-alpha");
        result.ApprovalRef.ShouldBe("approval:approval-alpha");
        result.AuditEvidenceRefs.ShouldContain("service-client:service-alpha");
        result.AuditEvidenceRefs.ShouldContain("approval:approval-alpha");
        json.ShouldContain("\"serviceClientRef\":\"service-client:service-alpha\"");
        json.ShouldContain("\"approvalRef\":\"approval:approval-alpha\"");
        AssertMetadataOnly(json);
    }

    [Fact]
    public static void RevokedSharedMailboxMembershipShouldNotDowngradeToAuthenticatedUserSendAtBoundary()
    {
        SenderAuthorityClassificationRequest request = SharedMailboxRequest() with
        {
            M365Posture = new SenderM365Posture(
                MailboxId: "shared-alpha",
                IsMailboxOwner: true,
                HasOwnMailboxMailSend: true,
                HasSharedMailboxSendPosture: true,
                HasSendOnBehalfPosture: false,
                HasApplicationMailSend: false,
                EvidenceRef: "m365:shared-mailbox-send"),
            SharedMailboxMembership = new SenderSharedMailboxMembershipEvidence(
                "shared-alpha",
                "actor-alpha",
                false,
                "membership:shared-alpha:actor-alpha"),
        };

        (SenderAuthorityClassificationResult result, string json) = ClassifyAndRoundTrip(request);

        result.AuthorityClass.ShouldBe(SenderAuthorityClass.SharedMailboxSend);
        result.DenialReason.ShouldBe(SenderAuthorityConflictReasons.MembershipRevoked);
        result.MailboxRef.ShouldBe("mailbox:shared-alpha");
        result.AuditEvidenceRefs.ShouldContain("sender-authority:shared-mailbox send");
        result.AuditEvidenceRefs.ShouldNotContain("sender-authority:authenticated-user send");
        AssertMetadataOnly(json);
    }

    [Fact]
    public static void ProviderPostureOnlyShouldFailClosedInBoundaryPayload()
    {
        SenderAuthorityClassificationRequest request = AuthenticatedUserRequest() with
        {
            ProjectAuthority = ProjectAuthority(hasProjectAuthority: false, scopes: []),
        };

        (SenderAuthorityClassificationResult result, string json) = ClassifyAndRoundTrip(request);

        result.AuthorityClass.ShouldBe(SenderAuthorityClass.AuthenticatedUserSend);
        result.DenialReason.ShouldBe(ChatBotDisabledActionReasons.PolicyBlocked);
        result.AuditEvidenceRefs.ShouldContain("m365:own-mailbox-mail-send");
        result.AuditEvidenceRefs.ShouldContain("project-authority:outbound-send");
        AssertMetadataOnly(json);
    }

    public static TheoryData<string, SenderAuthorityClass, string, string> SuccessfulAuthorityWorkflows()
        =>
        [
            new("draft", SenderAuthorityClass.DraftOnly, "sender-authority:draft-only", "project-authority:outbound-draft"),
            new("authenticated-user", SenderAuthorityClass.AuthenticatedUserSend, "sender-authority:authenticated-user send", "m365:own-mailbox-mail-send"),
            new("shared-mailbox", SenderAuthorityClass.SharedMailboxSend, "sender-authority:shared-mailbox send", "membership:shared-alpha:actor-alpha"),
            new("send-on-behalf", SenderAuthorityClass.SendOnBehalf, "sender-authority:send-on-behalf", "delegation:owner-alpha:actor-alpha"),
            new("service-send", SenderAuthorityClass.ApprovedServiceSend, "sender-authority:approved service-send", "approval:approval-alpha"),
        ];

    public static TheoryData<string, SenderAuthorityClass, string, string> FailClosedAuthorityWorkflows()
        =>
        [
            new(
                "policy-blocked",
                SenderAuthorityClass.SendOnBehalf,
                ChatBotDisabledActionReasons.PolicyBlocked,
                "principal-for:owner-alpha"),
            new(
                "delegation-mismatch",
                SenderAuthorityClass.SendOnBehalf,
                SenderAuthorityConflictReasons.DelegationMismatch,
                "delegation:owner-alpha:actor-beta"),
            new(
                "membership-revoked",
                SenderAuthorityClass.SharedMailboxSend,
                SenderAuthorityConflictReasons.MembershipRevoked,
                "membership:shared-alpha:actor-alpha"),
            new(
                "approval-missing",
                SenderAuthorityClass.ApprovedServiceSend,
                SenderAuthorityConflictReasons.ApprovalMissing,
                "service-client:service-alpha"),
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

    private static SenderAuthorityClassificationRequest FailClosedRequest(string caseName)
        => caseName switch
        {
            "policy-blocked" => SendOnBehalfRequest() with { TenantPolicy = Policy(allowSendOnBehalf: false) },
            "delegation-mismatch" => SendOnBehalfRequest() with
            {
                Delegation = new SenderDelegationEvidence("actor-beta", "owner-alpha", false, "delegation:owner-alpha:actor-beta"),
            },
            "membership-revoked" => SharedMailboxRequest() with
            {
                SharedMailboxMembership = new SenderSharedMailboxMembershipEvidence("shared-alpha", "actor-alpha", false, "membership:shared-alpha:actor-alpha"),
            },
            "approval-missing" => ServiceSendRequest() with
            {
                ApprovalChain = new SenderApprovalChainEvidence(null, false, "approval:missing"),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(caseName), caseName, null),
        };

    private static (SenderAuthorityClassificationResult Result, string Json) ClassifyAndRoundTrip(SenderAuthorityClassificationRequest request)
    {
        SenderAuthorityClassificationResult classified = SenderAuthorityClassifier.Classify(request);
        string json = JsonSerializer.Serialize(classified, JsonOptions);
        SenderAuthorityClassificationResult? roundTripped = JsonSerializer.Deserialize<SenderAuthorityClassificationResult>(json, JsonOptions);

        roundTripped.ShouldNotBeNull();
        return (roundTripped, json);
    }

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

    private static void AssertMetadataOnly(string json)
    {
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
            "Authorization",
        ];

        foreach (string marker in blocked)
        {
            json.ShouldNotContain(marker, Case.Insensitive);
        }
    }
}
