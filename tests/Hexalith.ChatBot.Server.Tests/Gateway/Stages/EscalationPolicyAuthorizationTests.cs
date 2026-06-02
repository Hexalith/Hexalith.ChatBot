using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class EscalationPolicyAuthorizationTests
{
    [Fact]
    public async Task EscalationPolicyChangeShouldAllowOnlyHumanPolicyScopeHolders()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "tenant-admin"),
                     Actor("human", "policy-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(EscalationChange()),
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
                Submission(EscalationChange()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.EscalationPolicyUnauthorized);
        }
    }

    [Fact]
    public async Task EscalationPolicyChangeShouldRejectInvalidOrStalePayloads()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitEscalationPolicyChange invalid in new[]
                 {
                     EscalationChange() with { SourceVersion = -1 },
                     EscalationChange() with { SchemaVersion = "escalation-policy-schema.custom" },
                     EscalationChange() with { ReasonCode = "unsafe reason" },
                     EscalationChange() with { NewEscalationFingerprint = "not-a-fingerprint" },
                     EscalationChange() with { ChangeSet = new EscalationPolicyChangeSet([]) },
                     EscalationChange() with
                     {
                         ChangeSet = new EscalationPolicyChangeSet(
                         [
                             new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
                             new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, 60, EscalationSeverity.Low, AdminRole.TenantAdmin, NotificationChannel.Email),
                         ]),
                     },
                     EscalationChange() with
                     {
                         ChangeSet = new EscalationPolicyChangeSet(
                         [
                             new EscalationPolicyEntry(NotificationStateClass.Retry, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.InApp),
                         ]),
                     },
                     EscalationChange() with
                     {
                         ChangeSet = new EscalationPolicyChangeSet(
                         [
                             new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, -1, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
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
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.EscalationPolicyUnauthorized);
        }
    }

    private static SubmitEscalationPolicyChange EscalationChange()
        => new(
            "escalation-change-001",
            "escalation-snapshot-current",
            "escalation-snapshot-proposed",
            4,
            new EscalationPolicyChangeSet(
            [
                new EscalationPolicyEntry(NotificationStateClass.ReviewNeeded, AdminScope.SeeOnly, 86400, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.InApp),
                new EscalationPolicyEntry(NotificationStateClass.ApprovalPending, AdminScope.Policy, 43200, EscalationSeverity.Medium, AdminRole.PolicyAdmin, NotificationChannel.Email),
                new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            ]),
            "escalation-update",
            "admin-requester",
            EscalationPolicySchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "sha256:escalationold",
            "sha256:escalationnew");

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
