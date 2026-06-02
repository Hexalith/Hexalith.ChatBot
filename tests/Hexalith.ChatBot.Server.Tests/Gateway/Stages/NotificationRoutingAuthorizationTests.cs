using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class NotificationRoutingAuthorizationTests
{
    [Fact]
    public async Task RoutingChangeShouldAllowOnlyHumanPolicyScopeHolders()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "tenant-admin"),
                     Actor("human", "policy-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(RoutingChange()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "mailbox-admin"),
                     Actor("human", "compliance-admin"),
                     Actor("human", "operations-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                     Actor("service", "policy-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(RoutingChange()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.NotificationRoutingUnauthorized);
        }
    }

    [Fact]
    public async Task RoutingChangeShouldRejectInvalidOrStalePayloads()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitNotificationRoutingChange invalid in new[]
                 {
                     RoutingChange() with { SourceVersion = -1 },
                     RoutingChange() with { SchemaVersion = "notification-routing-schema.custom" },
                     RoutingChange() with { ReasonCode = "unsafe reason" },
                     RoutingChange() with { NewRoutingFingerprint = "not-a-fingerprint" },
                     RoutingChange() with { ChangeSet = new NotificationRoutingChangeSet([]) },
                     RoutingChange() with
                     {
                         ChangeSet = new NotificationRoutingChangeSet(
                         [
                             new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
                             new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.TenantAdmin, NotificationChannel.Email),
                         ]),
                     },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(invalid),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.NotificationRoutingUnauthorized);
        }
    }

    private static SubmitNotificationRoutingChange RoutingChange()
        => new(
            "routing-change-001",
            "routing-snapshot-current",
            "routing-snapshot-proposed",
            4,
            new NotificationRoutingChangeSet(
            [
                new NotificationRoutingEntry(NotificationStateClass.ReviewNeeded, AdminScope.SeeOnly, AdminRole.OperationsAdmin, NotificationChannel.InApp),
                new NotificationRoutingEntry(NotificationStateClass.ApprovalPending, AdminScope.Policy, AdminRole.PolicyAdmin, NotificationChannel.Email),
                new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            ]),
            "routing-update",
            "admin-requester",
            NotificationRoutingSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "sha256:routingold",
            "sha256:routingnew");

    private static ChatBotCommandSubmission Submission(object command, string? commandType = null)
        => new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = commandType ?? command.GetType().Name,
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            null,
            ChatBotSurfaceOrigin.Ui);

    private static ChatBotAuthenticatedActor Actor(string actorType, string role)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));
        return new ChatBotAuthenticatedActor("actor-alpha", principal);
    }
}
