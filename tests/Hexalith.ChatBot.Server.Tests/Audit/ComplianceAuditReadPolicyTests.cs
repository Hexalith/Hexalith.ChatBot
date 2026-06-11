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

    [Fact]
    public static void SurfaceFilterShouldMatchEnvelopeSurfaceOriginAndRejectMismatches()
    {
        ComplianceAuditSearchResult matched = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            Query() with { Filters = [new ComplianceAuditFilterRef("audit-filter-surface", "surface", "ui")] },
            [Envelope(), Envelope() with { SurfaceOrigin = "cli", ResourceId = "audit-record-cli" }],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        ComplianceAuditSearchResult mismatched = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            Query() with { Filters = [new ComplianceAuditFilterRef("audit-filter-surface", "surface", "mailbox")] },
            [Envelope()],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        matched.Rows.ShouldHaveSingleItem().AuditRecordRef.ShouldBe("audit-record-001");
        mismatched.Rows.ShouldBeEmpty();
    }

    [Fact]
    public static void MessageIdFilterShouldMatchSourceAndProviderMessageEvidenceTokens()
    {
        AuditEnvelope sourceMessage = Envelope() with
        {
            SourceEvidenceRefs = ["source-message:intake-007", "project:redacted-ref"],
        };
        AuditEnvelope providerMessage = Envelope() with
        {
            ResourceId = "audit-record-provider",
            SourceEvidenceRefs = ["provider-message:graph-009"],
        };

        ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            Query() with { Filters = [new ComplianceAuditFilterRef("audit-filter-message", "message-id", "intake-007")] },
            [sourceMessage, providerMessage],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW").Rows.ShouldHaveSingleItem().AuditRecordRef.ShouldBe("audit-record-001");

        ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            Query() with { Filters = [new ComplianceAuditFilterRef("audit-filter-message", "message-id", "graph-009")] },
            [sourceMessage, providerMessage],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW").Rows.ShouldHaveSingleItem().AuditRecordRef.ShouldBe("audit-record-provider");

        ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            Query() with { Filters = [new ComplianceAuditFilterRef("audit-filter-message", "message-id", "unknown-id")] },
            [sourceMessage, providerMessage],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW").Rows.ShouldBeEmpty();
    }

    [Fact]
    public static void EveryFr56FilterDimensionShouldMatchItsEnvelopeFieldInLockStep()
    {
        // The actor/command/decision/correlation/surface/message-id/tenant arms are covered elsewhere; this pins the
        // remaining FR56 dimensions (actor-type, resource, reason, policy-snapshot) so a MatchesFilter arm can never
        // silently drift from ComplianceAdministrationSchema.AuditFilterKeys.
        (string key, string value)[] matchingDimensions =
        [
            ("actor-type", "human"),
            ("resource", "audit-record-001"),
            ("reason", "pre_commit_gate"),
            ("policy-snapshot", "policy-snapshot-admin-v1"),
        ];

        foreach ((string key, string value) in matchingDimensions)
        {
            ComplianceAuditReadPolicy.Search(
                CompliancePrincipal("compliance-admin"),
                Query() with { Filters = [new ComplianceAuditFilterRef($"audit-filter-{key}", key, value)] },
                [Envelope()],
                new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
                "01ARZ3NDEKTSV4RRFFQ69G5FAW").Rows.ShouldHaveSingleItem().AuditRecordRef.ShouldBe("audit-record-001", key);

            ComplianceAuditReadPolicy.Search(
                CompliancePrincipal("compliance-admin"),
                Query() with { Filters = [new ComplianceAuditFilterRef($"audit-filter-{key}", key, "no-such-value")] },
                [Envelope()],
                new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
                "01ARZ3NDEKTSV4RRFFQ69G5FAW").Rows.ShouldBeEmpty(key);
        }
    }

    [Fact]
    public static void ReplayMarkedEnvelopesShouldBeExcludedFromDefaultProductionSearch()
    {
        ComplianceAuditSearchResult result = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            Query(),
            [
                Envelope(),
                Envelope() with { ResourceId = "audit-record-replay", ReplayRunId = "replay-run-001" },
            ],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        result.Rows.ShouldHaveSingleItem().AuditRecordRef.ShouldBe("audit-record-001");
        result.Rows.ShouldNotContain(static row => row.AuditRecordRef == "audit-record-replay");
    }

    [Fact]
    public static void TenantFilterAndLimitShouldBoundTheReturnedRows()
    {
        ComplianceAuditSearchResult crossTenant = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            Query() with { Filters = [new ComplianceAuditFilterRef("audit-filter-tenant", "tenant", "tenant-beta")] },
            [Envelope()],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        ComplianceAuditSearchResult limited = ComplianceAuditReadPolicy.Search(
            CompliancePrincipal("compliance-admin"),
            Query() with { Limit = 1 },
            [
                Envelope() with { Timestamp = new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero) },
                Envelope() with { ResourceId = "audit-record-late", Timestamp = new DateTimeOffset(2026, 6, 2, 4, 30, 0, TimeSpan.Zero) },
            ],
            new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        crossTenant.Rows.ShouldBeEmpty();
        limited.Rows.ShouldHaveSingleItem().AuditRecordRef.ShouldBe("audit-record-001");
    }

    [Fact]
    public static void PerProjectAuthorityShouldDriveDetailVisibilityFromActualGrants()
    {
        AuditEnvelope envelope = Envelope();

        ComplianceAuditReadPolicy.HasPerProjectAuthority(ProjectOwner("compliance-admin", "redacted-ref"), envelope).ShouldBeTrue();
        ComplianceAuditReadPolicy.HasPerProjectAuthority(ProjectOwner("compliance-admin", "other-project"), envelope).ShouldBeFalse();
        ComplianceAuditReadPolicy.HasPerProjectAuthority(CompliancePrincipal("compliance-admin"), envelope).ShouldBeFalse();
        ComplianceAuditReadPolicy.HasPerProjectAuthority(ProjectOwner("policy-admin", "redacted-ref"), envelope).ShouldBeFalse();

        // NFR2 / Story 9.3: a tenant-wide "*" project-owner wildcard must NOT confer compliance full-detail authority.
        // Compliance detail requires an explicit per-project grant matching the record's project: evidence token; the
        // blanket wildcard is honored elsewhere (notification routing/outbound) but is intentionally denied here so it
        // cannot widen unredacted compliance detail to every project.
        ComplianceAuditReadPolicy.HasPerProjectAuthority(ProjectOwner("compliance-admin", "*"), envelope).ShouldBeFalse();

        ComplianceAuditDetail available = ComplianceAuditReadPolicy.Detail(envelope, hasPerProjectAuthority: true);
        available.RedactionState.ShouldBe(ComplianceAuditRedactionState.DetailAvailable);
        available.SafeNextAction.ShouldBe("view-metadata");
        available.VisibleMetadataRefs.ShouldNotBeEmpty();
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

    private static ClaimsPrincipal ProjectOwner(string role, string project)
        => new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, "human"),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
                new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, project),
            ],
            "test"));

    private static ClaimsPrincipal Actor(string actorType, string role)
        => new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));
}
