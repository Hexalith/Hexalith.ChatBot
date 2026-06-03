using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class AdminQueueSummaryProjectorTests
{
    [Fact]
    public void SeeOnlySummaryShouldOmitProjectEvidenceFileAuditAndMailboxDetail()
    {
        AdminQueueSummary summary = AdminQueueSummaryProjector.Create(
            "queue:failure",
            [
                new AdminQueueSummaryProjectionItem(
                    "queue:failure",
                    "item:001",
                    "retryable",
                    "operations",
                    ChatBotHealthStatus.Degraded,
                    120,
                    ProjectName: "Project Alpha",
                    EvidenceContent: "evidence content sentinel",
                    FileMetadata: "file-secret.pdf",
                    AuditReason: "raw audit reason",
                    MailboxSubject: "customer subject",
                    CandidateEvidence: "candidate evidence"),
            ],
            AuditRef(),
            "correlation-alpha");

        summary.QueueRef.ShouldBe("queue:failure");
        summary.Health.ShouldBe(ChatBotHealthStatus.Degraded);
        summary.Buckets.Single().Count.ShouldBe(1);
        summary.VisibleItemRefs.Single().ItemRef.ShouldBe("item:001");

        string json = JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("Project Alpha", Case.Insensitive);
        json.ShouldNotContain("evidence content sentinel", Case.Insensitive);
        json.ShouldNotContain("file-secret.pdf", Case.Insensitive);
        json.ShouldNotContain("raw audit reason", Case.Insensitive);
        json.ShouldNotContain("customer subject", Case.Insensitive);
        json.ShouldNotContain("candidate evidence", Case.Insensitive);
    }

    [Fact]
    public void OperationalQueueSearchShouldRenderAllFamiliesWithStablePaginationFilteringAndRedaction()
    {
        AdminQueueSummaryProjectionItem[] items =
        [
            QueueItem(OperationalQueueFamily.AmbiguousAssociation, "item:ambiguous-001", age: 300, risk: "high", confidence: 0.62m, priority: 90, sourceVersion: 4),
            QueueItem(OperationalQueueFamily.UnresolvedParticipant, "item:participant-001", age: 200, risk: "medium", confidence: 0.44m, priority: 50, sourceVersion: 3),
            QueueItem(OperationalQueueFamily.PendingApproval, "item:approval-001", age: 100, risk: "critical", confidence: 0.91m, priority: 100, sourceVersion: 8),
            QueueItem(OperationalQueueFamily.FailedIngestion, "item:ingestion-001", age: 90, risk: "high", confidence: 0.70m, priority: 70, sourceVersion: 6, retryCount: 3),
            QueueItem(OperationalQueueFamily.FailedAttachment, "item:attachment-001", age: 80, risk: "medium", confidence: 0.68m, priority: 60, sourceVersion: 5, retryCount: 2),
            QueueItem(OperationalQueueFamily.RetryableOperation, "item:retry-001", age: 70, risk: "low", confidence: 0.80m, priority: 40, sourceVersion: 2, retryCount: 1),
            QueueItem(OperationalQueueFamily.AmbiguousAssociation, "item:ambiguous-002", age: 400, risk: "high", confidence: 0.72m, priority: 90, sourceVersion: 5),
        ];

        foreach (OperationalQueueFamily family in OperationalQueueFamilies.All)
        {
            OperationalQueueSearchResult result = AdminQueueSummaryProjector.Search(
                new SearchOperationalQueueItems(
                    family,
                    PageSize: 100,
                    PageToken: null,
                    OperationalQueueSortKey.Priority,
                    SortDescending: true,
                    new OperationalQueueFilter()),
                items,
                "correlation-alpha");

            result.Rows.ShouldNotBeEmpty(OperationalQueueFamilies.ToWireValue(family));
            result.PageSize.ShouldBeLessThanOrEqualTo(100);
            result.Rows.ShouldAllBe(row => row.QueueFamily == family);
            result.Rows.ShouldAllBe(row => row.RedactionState == "metadata_only");
            result.Rows.ShouldAllBe(row => row.Diagnostics.WorkflowItemRef == row.ItemRef);
        }

        OperationalQueueSearchResult ambiguous = AdminQueueSummaryProjector.Search(
            new SearchOperationalQueueItems(
                OperationalQueueFamily.AmbiguousAssociation,
                PageSize: 1,
                PageToken: null,
                OperationalQueueSortKey.Priority,
                SortDescending: true,
                new OperationalQueueFilter(Risk: "high")),
            items,
            "correlation-alpha");

        ambiguous.Rows.Count.ShouldBe(1);
        ambiguous.Rows.Single().ItemRef.ShouldBe("item:ambiguous-002");
        ambiguous.NextPageToken.ShouldBe("item:ambiguous-002");
        ambiguous.TotalCount.ShouldBe(2);
        ambiguous.StableFilterFingerprint.ShouldStartWith("sha256:");

        OperationalQueueSearchResult secondPage = AdminQueueSummaryProjector.Search(
            new SearchOperationalQueueItems(
                OperationalQueueFamily.AmbiguousAssociation,
                PageSize: 1,
                PageToken: ambiguous.NextPageToken,
                OperationalQueueSortKey.Priority,
                SortDescending: true,
                new OperationalQueueFilter(Risk: "high")),
            items,
            "correlation-alpha");

        secondPage.Rows.Count.ShouldBe(1);
        secondPage.Rows.Single().ItemRef.ShouldBe("item:ambiguous-001");
        secondPage.NextPageToken.ShouldBeNull();
        secondPage.TotalCount.ShouldBe(2);
        secondPage.StableFilterFingerprint.ShouldBe(ambiguous.StableFilterFingerprint);

        OperationalQueueItemDetail deniedDetail = AdminQueueSummaryProjector.CreateSafeDetail(ambiguous.Rows.Single(), hasProjectAuthority: false);
        deniedDetail.DetailAccessState.ShouldBe("request-access");
        deniedDetail.SafeDetailStatus.ShouldBe("restricted-detail-redacted");
        deniedDetail.EscalationActions.ShouldContain("request-access");

        string json = JsonSerializer.Serialize(ambiguous, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("Project Alpha", Case.Insensitive);
        json.ShouldNotContain("evidence content sentinel", Case.Insensitive);
        json.ShouldNotContain("file-secret.pdf", Case.Insensitive);
        json.ShouldNotContain("customer subject", Case.Insensitive);
    }

    [Fact]
    public void ToOperationalRowShouldEmitRunbookRealDiagnosticsFromPopulatedSourceFields()
    {
        DateTimeOffset transitionAt = new(2026, 6, 2, 4, 0, 0, TimeSpan.Zero);
        AdminQueueSummaryProjectionItem item = new(
            QueueRef: "queue:retryable-operation",
            ItemRef: "item:retry-001",
            Status: "retryable",
            OwnerClass: "operations",
            Health: ChatBotHealthStatus.Degraded,
            AgeSeconds: 90,
            QueueFamily: OperationalQueueFamily.RetryableOperation,
            NextAction: "wait-for-next-retry",
            RetryCount: 3,
            FailureState: ChatBotMessageCodes.RetryExhausted,
            CorrelationId: "corr-alpha-01",
            TenantRef: "t-alpha",
            LastTransitionFromState: "request",
            LastTransitionActor: "requester-a",
            LastTransitionTimestampUtc: transitionAt);

        OperationalQueueSearchResult result = AdminQueueSummaryProjector.Search(
            new SearchOperationalQueueItems(
                OperationalQueueFamily.RetryableOperation,
                PageSize: 100,
                PageToken: null,
                OperationalQueueSortKey.Priority,
                SortDescending: true,
                new OperationalQueueFilter()),
            [item],
            "correlation-alpha");

        OperationalQueueDiagnostics diagnostics = result.Rows.Single().Diagnostics;
        diagnostics.CorrelationId.ShouldBe("corr-alpha-01");
        diagnostics.TenantRef.ShouldBe("t-alpha");
        diagnostics.LastTransition.ShouldBe($"from:request|actor:requester-a|at:{transitionAt.ToUnixTimeSeconds()}");
        diagnostics.FailureReason.ShouldBe(ChatBotMessageCodes.RetryExhausted);
        diagnostics.NextSafeAction.ShouldBe("wait-for-next-retry");

        // The fully-populated diagnostic is runbook-complete (no stubs, no placeholders).
        RunbookDiagnosticCompletenessValidator.IsComplete(diagnostics).ShouldBeTrue();
    }

    [Fact]
    public void ToOperationalRowShouldEmitUnknownTokensNotLegacyStubsWhenSourceFieldsAreAbsent()
    {
        // No correlation/tenant/last-transition source fields: the row emits fail-closed "unknown" tokens (never the
        // old "correlation:"/"tenant:current"/"last-transition:" stubs), which the completeness validator flags.
        AdminQueueSummaryProjectionItem item = new(
            QueueRef: "queue:retryable-operation",
            ItemRef: "item:retry-002",
            Status: "waiting",
            OwnerClass: "operations",
            Health: ChatBotHealthStatus.Healthy,
            AgeSeconds: 10,
            QueueFamily: OperationalQueueFamily.RetryableOperation,
            NextAction: "claim");

        OperationalQueueSearchResult result = AdminQueueSummaryProjector.Search(
            new SearchOperationalQueueItems(
                OperationalQueueFamily.RetryableOperation,
                PageSize: 100,
                PageToken: null,
                OperationalQueueSortKey.Priority,
                SortDescending: true,
                new OperationalQueueFilter()),
            [item],
            "correlation-alpha");

        OperationalQueueDiagnostics diagnostics = result.Rows.Single().Diagnostics;
        diagnostics.CorrelationId.ShouldBe("unknown");
        diagnostics.TenantRef.ShouldBe("unknown");
        diagnostics.LastTransition.ShouldBe("from:unknown|actor:unknown|at:0");

        IReadOnlyList<string> defects = RunbookDiagnosticCompletenessValidator.Validate(diagnostics);
        defects.ShouldContain("CorrelationId");
        defects.ShouldContain("TenantRef");
        defects.ShouldContain("LastTransition");
    }

    [Fact]
    public void ReadPolicyShouldAllowHumanSeeOnlyAdminWithoutProjectMembership()
    {
        AdminQueueSummaryReadDecision decision = AdminQueueSummaryReadPolicy.Evaluate(
            Principal("operations-admin"),
            aggregationCount: 4,
            auditThreshold: 10,
            auditAvailable: true);

        decision.IsAllowed.ShouldBeTrue();
        decision.RedactionState.ShouldBe("metadata_only");
    }

    [Fact]
    public void ReadPolicyShouldFailClosedAboveAuditThresholdWhenAuditUnavailable()
    {
        AdminQueueSummaryReadDecision decision = AdminQueueSummaryReadPolicy.Evaluate(
            Principal("tenant-admin"),
            aggregationCount: 10,
            auditThreshold: 10,
            auditAvailable: false);

        decision.IsAllowed.ShouldBeFalse();
        decision.ReasonCode.ShouldBe("audit_unavailable");
        decision.RedactionState.ShouldBe("metadata_only");
    }

    [Fact]
    public void ReadPolicyShouldDenyServiceAndAiActorsWithTenantAdminLookingClaims()
    {
        foreach (ClaimsPrincipal principal in new[]
                 {
                     Principal("tenant-admin", "service"),
                     Principal("tenant-admin", "ai"),
                 })
        {
            AdminQueueSummaryReadDecision decision = AdminQueueSummaryReadPolicy.Evaluate(
                principal,
                aggregationCount: 1,
                auditThreshold: 10,
                auditAvailable: true);

            decision.IsAllowed.ShouldBeFalse();
            decision.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    private static AdminOperationReference AuditRef()
        => new(
            "admin-alpha",
            "human",
            AdminScope.SeeOnly,
            "queue:failure",
            ["item:001"],
            1,
            "dashboard-read",
            "policy-snapshot:admin:v1",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
            7,
            "metadata_only");

    private static AdminQueueSummaryProjectionItem QueueItem(
        OperationalQueueFamily family,
        string itemRef,
        int age,
        string risk,
        decimal confidence,
        decimal priority,
        long sourceVersion,
        int retryCount = 0)
        => new(
            QueueRef: $"queue:{OperationalQueueFamilies.ToWireValue(family)}",
            ItemRef: itemRef,
            Status: retryCount > 0 ? "retryable" : "waiting",
            OwnerClass: "operations",
            Health: retryCount > 2 ? ChatBotHealthStatus.Degraded : ChatBotHealthStatus.Healthy,
            AgeSeconds: age,
            QueueFamily: family,
            Risk: risk,
            Confidence: confidence,
            AssigneeRef: "admin:reviewer-a",
            NextAction: "claim",
            RetryCount: retryCount,
            FreshnessTimestampUtc: new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
            OwnerRole: "operations-admin",
            MailboxRef: "mailbox:ops",
            FailureState: retryCount > 0 ? "retryable" : "waiting",
            SourceVersion: sourceVersion,
            PriorityScore: priority,
            PriorityExplanation: "risk-age-source",
            ProjectName: "Project Alpha",
            EvidenceContent: "evidence content sentinel",
            FileMetadata: "file-secret.pdf",
            AuditReason: "raw audit reason",
            MailboxSubject: "customer subject",
            CandidateEvidence: "candidate evidence");

    private static ClaimsPrincipal Principal(string role, string actorType = "human")
        => new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));
}
