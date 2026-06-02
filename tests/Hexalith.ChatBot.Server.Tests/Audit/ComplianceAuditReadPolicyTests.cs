using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

public static class ComplianceAuditReadPolicyTests
{
    [Fact]
    public static void TenantComplianceSearchShouldReturnMetadataOnlyRowsForHumanComplianceScope()
    {
        ComplianceAuditQueryFilters query = Query();
        AuditEnvelope envelope = Envelope();

        ComplianceAuditSearchResult result = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            query,
            [envelope],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        result.Rows.ShouldHaveSingleItem();
        ComplianceAuditResultRow row = result.Rows[0];
        row.AuditRecordRef.ShouldBe("audit-record-001");
        row.ActorRef.ShouldBe("actor-alpha");
        row.CommandRef.ShouldBe("SubmitRetentionConfigurationChange");
        row.RedactionState.ShouldBe(ComplianceAuditRedactionState.Restricted);
        row.SafeNextAction.ShouldBe("request-access");

        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("Hidden Project", Case.Insensitive);
        json.ShouldNotContain("mailbox subject", Case.Insensitive);
        json.ShouldNotContain("provider payload", Case.Insensitive);
        json.ShouldNotContain("audit envelope", Case.Insensitive);
    }

    [Fact]
    public static void TenantAdminComplianceSearchShouldReturnTenantWideMetadataOnlyRows()
    {
        ComplianceAuditSearchResult result = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("tenant-admin"),
            Query(),
            [Envelope()],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        result.Rows.ShouldHaveSingleItem();
        result.ResultFingerprint.ShouldBe("sha256:1");
        result.Rows[0].SafeNextAction.ShouldBe("request-access");
    }

    [Fact]
    public static void ComplianceSearchShouldApplyFiltersAndUtcTimeWindowBeforeReturningRows()
    {
        ComplianceAuditSearchResult result = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            Query() with
            {
                Filters =
                [
                    new ComplianceAuditFilterRef("audit-filter-actor", "actor", "actor-alpha"),
                    new ComplianceAuditFilterRef("audit-filter-command", "command", "SubmitRetentionConfigurationChange"),
                    new ComplianceAuditFilterRef("audit-filter-decision", "decision", "allow"),
                    new ComplianceAuditFilterRef("audit-filter-correlation", "correlation", "01ARZ3NDEKTSV4RRFFQ69G5FAW"),
                ],
            },
            [
                Envelope(),
                Envelope() with { ActorId = "actor-beta", ResourceId = "audit-record-002" },
                Envelope() with { CommandName = "RequestComplianceEscalation", ResourceId = "audit-record-003" },
                Envelope() with { Timestamp = new DateTimeOffset(2026, 5, 31, 23, 59, 59, TimeSpan.Zero), ResourceId = "audit-record-004" },
            ],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        result.Rows.ShouldHaveSingleItem().AuditRecordRef.ShouldBe("audit-record-001");
    }

    [Fact]
    public static void InvalidComplianceAuditQueryShouldDenyBeforeHydration()
    {
        ComplianceAuditSearchResult result = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            Query() with
            {
                Filters = [new ComplianceAuditFilterRef("audit-filter-001", "raw-json", "actor-alpha")],
            },
            [Envelope()],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        result.Rows.ShouldBeEmpty();
        result.ResultFingerprint.ShouldBe("sha256:denied");
    }

    [Fact]
    public static void InvalidComplianceAuditQueryShouldNotEchoUnsafeQueryRefs()
    {
        ComplianceAuditSearchResult result = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            Query() with { QueryRef = "raw query with secret" },
            [Envelope()],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "raw correlation secret");

        result.Rows.ShouldBeEmpty();
        result.QueryRef.ShouldBe("denied");
        result.CorrelationId.ShouldBe("denied");
    }

    [Fact]
    public static void RestrictedComplianceDetailShouldOfferEscalationWithoutHiddenResourceLeakage()
    {
        ComplianceAuditDetail detail = ComplianceAuditReadPolicy.Detail(Envelope(), hasPerProjectAuthority: false);

        detail.RedactionState.ShouldBe(ComplianceAuditRedactionState.EscalationRequired);
        detail.EscalationStatus.ShouldBe(ComplianceEscalationStatus.Requested);
        detail.VisibleMetadataRefs.ShouldBeEmpty();
        detail.SafeNextAction.ShouldBe("request-access");

        string json = JsonSerializer.Serialize(detail, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("Hidden Project", Case.Insensitive);
        json.ShouldNotContain("mailbox subject", Case.Insensitive);
        json.ShouldNotContain("provider payload", Case.Insensitive);
    }

    [Fact]
    public static void ServiceAndNonComplianceActorsShouldNotReceiveRows()
    {
        ComplianceAuditSearchResult serviceResult = ComplianceAuditReadPolicy.Search(
            Actor("service", "tenant-admin"),
            Query(),
            [Envelope()],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        ComplianceAuditSearchResult policyResult = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("policy-admin"),
            Query(),
            [Envelope()],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        serviceResult.Rows.ShouldBeEmpty();
        policyResult.Rows.ShouldBeEmpty();
    }

    private static ComplianceAuditQueryFilters Query()
        => new(
            "audit-query-001",
            [new ComplianceAuditFilterRef("audit-filter-001", "actor", "actor-alpha")],
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero),
            100);

    private static AuditEnvelope Envelope()
        => new(
            "tenant-alpha",
            "actor-alpha",
            "human",
            "SubmitRetentionConfigurationChange",
            "audit-record-001",
            "allow",
            "pre_commit_gate",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
            "policy-snapshot-admin-v1",
            ["admin-scope:compliance", "project:redacted-ref", "resource:opaque-ref"],
            null,
            "Received->Proposed",
            "metadata_only",
            "gate_passed",
            AuditCommitPhase.PreCommit,
            "chatbot.audit-envelope.v1",
            null,
            "ui");

    private static ClaimsPrincipal CompliancePrincipal(string role)
        => Actor("human", role);

    private static ClaimsPrincipal Actor(string actorType, string role)
        => new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));
}
