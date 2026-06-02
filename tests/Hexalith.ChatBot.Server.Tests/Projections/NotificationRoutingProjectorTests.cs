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

public sealed class NotificationRoutingProjectorTests
{
    [Fact]
    public void ProjectorShouldRenderSummarySafeRowsForValidSnapshot()
    {
        NotificationRoutingSummary summary = NotificationRoutingSnapshotProjector.Create(
            ValidSnapshot(),
            "routing-snapshot-active",
            7,
            "sha256:routingactive",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        summary.Rows.Count.ShouldBe(3);
        summary.SchemaVersion.ShouldBe(NotificationRoutingSchemaVersions.V1);
        summary.SourceVersion.ShouldBe(7);
        summary.RoutingFingerprint.ShouldBe("sha256:routingactive");
        summary.Rows.ShouldContain(row =>
            row.StateClass == "approval-pending" && row.Scope == "policy" &&
            row.RecipientRole == "policy-admin" && row.Channel == "email");

        // Read-back is summary-safe; no recipient PII.
        string json = JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("@", Case.Insensitive);
        json.ShouldNotContain("address", Case.Insensitive);
    }

    [Fact]
    public void ProjectorShouldDropUndeclaredEntriesAndDenyInvalidSnapshot()
    {
        NotificationRoutingSummary summary = NotificationRoutingSnapshotProjector.Create(
            new NotificationRoutingChangeSet(
            [
                new NotificationRoutingEntry((NotificationStateClass)99, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.InApp),
            ]),
            "routing-snapshot-active",
            7,
            "sha256:routingactive",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        summary.Rows.ShouldBeEmpty();
        summary.RoutingFingerprint.ShouldBe("sha256:denied");
        summary.SourceVersion.ShouldBe(0);
    }

    [Fact]
    public void ReadPolicyShouldAllowPolicyScopeHoldersAndDenyOthers()
    {
        GetNotificationRoutingSummary query = new(AdminScope.Policy, "routing-snapshot-active", "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        foreach (string role in new[] { "policy-admin", "tenant-admin" })
        {
            NotificationRoutingReadDecision allowed = NotificationRoutingReadPolicy.Read(
                Principal("human", role),
                query,
                ValidSnapshot(),
                7,
                "sha256:routingactive",
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
            NotificationRoutingReadDecision denied = NotificationRoutingReadPolicy.Read(
                Principal(actorType, role),
                query,
                ValidSnapshot(),
                7,
                "sha256:routingactive",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW");
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.NotificationRoutingUnauthorized);
            denied.Summary.ShouldBeNull();
        }
    }

    private static NotificationRoutingChangeSet ValidSnapshot()
        => new(
        [
            new NotificationRoutingEntry(NotificationStateClass.ReviewNeeded, AdminScope.SeeOnly, AdminRole.OperationsAdmin, NotificationChannel.InApp),
            new NotificationRoutingEntry(NotificationStateClass.ApprovalPending, AdminScope.Policy, AdminRole.PolicyAdmin, NotificationChannel.Email),
            new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
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
