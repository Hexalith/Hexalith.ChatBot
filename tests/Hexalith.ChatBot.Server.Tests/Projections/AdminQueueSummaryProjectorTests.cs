using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
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

    private static ClaimsPrincipal Principal(string role, string actorType = "human")
        => new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));
}
