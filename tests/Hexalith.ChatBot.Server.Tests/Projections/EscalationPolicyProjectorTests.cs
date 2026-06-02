using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class EscalationPolicyProjectorTests
{
    [Fact]
    public void ProjectorShouldRenderSummarySafeRowsForValidSnapshot()
    {
        EscalationPolicySummary summary = EscalationPolicySnapshotProjector.Create(
            ValidSnapshot(),
            "escalation-snapshot-active",
            7,
            "sha256:escalationactive",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        summary.Rows.Count.ShouldBe(3);
        summary.SchemaVersion.ShouldBe(EscalationPolicySchemaVersions.V1);
        summary.SourceVersion.ShouldBe(7);
        summary.EscalationFingerprint.ShouldBe("sha256:escalationactive");
        summary.Rows.ShouldContain(row =>
            row.StateClass == "approval-pending" && row.Scope == "policy" &&
            row.SeverityThreshold == "medium" && row.EscalationTargetRole == "policy-admin" &&
            row.EscalationChannel == "email" && row.AgeThresholdSeconds == 43200);

        // Read-back is summary-safe; no recipient PII.
        string json = JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("@", Case.Insensitive);
        json.ShouldNotContain("address", Case.Insensitive);
    }

    [Fact]
    public void ProjectorShouldDropUndeclaredEntriesAndDenyInvalidSnapshot()
    {
        EscalationPolicySummary summary = EscalationPolicySnapshotProjector.Create(
            new EscalationPolicyChangeSet(
            [
                new EscalationPolicyEntry(NotificationStateClass.Retry, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.InApp),
            ]),
            "escalation-snapshot-active",
            7,
            "sha256:escalationactive",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        summary.Rows.ShouldBeEmpty();
        summary.EscalationFingerprint.ShouldBe("sha256:denied");
        summary.SourceVersion.ShouldBe(0);
    }

    [Fact]
    public void ReadPolicyShouldAllowPolicyScopeHoldersAndDenyOthers()
    {
        GetEscalationPolicySummary query = new(AdminScope.Policy, "escalation-snapshot-active", "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        foreach (string role in new[] { "policy-admin", "tenant-admin" })
        {
            EscalationPolicyReadDecision allowed = EscalationPolicyReadPolicy.Read(
                Principal("human", role),
                query,
                ValidSnapshot(),
                7,
                "sha256:escalationactive",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW");
            allowed.IsAllowed.ShouldBeTrue();
            allowed.Summary.ShouldNotBeNull();
            allowed.Summary!.Rows.Count.ShouldBe(3);
        }

        foreach ((string actorType, string role) in new[]
                 {
                     ("human", "operations-admin"),
                     ("human", "compliance-admin"),
                     ("human", "mailbox-admin"),
                     ("service", "policy-admin"),
                     ("ai", "policy-admin"),
                 })
        {
            EscalationPolicyReadDecision denied = EscalationPolicyReadPolicy.Read(
                Principal(actorType, role),
                query,
                ValidSnapshot(),
                7,
                "sha256:escalationactive",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW");
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.EscalationPolicyUnauthorized);
            denied.Summary.ShouldBeNull();
        }
    }

    private static EscalationPolicyChangeSet ValidSnapshot()
        => new(
        [
            new EscalationPolicyEntry(NotificationStateClass.ReviewNeeded, AdminScope.SeeOnly, 86400, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.InApp),
            new EscalationPolicyEntry(NotificationStateClass.ApprovalPending, AdminScope.Policy, 43200, EscalationSeverity.Medium, AdminRole.PolicyAdmin, NotificationChannel.Email),
            new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
        ]);

    private static ClaimsPrincipal Principal(string actorType, string role)
        => new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));
}
