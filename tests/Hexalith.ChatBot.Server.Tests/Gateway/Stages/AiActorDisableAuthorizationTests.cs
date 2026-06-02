using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using SubmitAiActorDisable = Hexalith.ChatBot.Contracts.Commands.SubmitAiActorDisable;
using ApproveAiActorDisable = Hexalith.ChatBot.Contracts.Commands.ApproveAiActorDisable;
using AiActorControlState = Hexalith.ChatBot.Contracts.Enums.AiActorControlState;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class AiActorDisableAuthorizationTests
{
    [Fact]
    public async Task DisableProposalShouldRequireHumanPolicyAdmin()
    {
        ParticipantAuthorizationStage stage = new();

        // AI-action governance is the policy-admin's domain (Story 7.2). A policy-admin is allowed; a tenant-admin
        // is also allowed because it holds the FR75a scope union (this is not a relaxation).
        foreach (ChatBotAuthenticatedActor allowedActor in new[]
                 {
                     Actor("human", "policy-admin"),
                     Actor("human", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(DisableSubmit()),
                allowedActor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }

        // Every non-policy human scope is denied, and service/AI actors are denied even with admin-looking claims.
        foreach (ChatBotAuthenticatedActor deniedActor in new[]
                 {
                     Actor("human", "mailbox-admin"),
                     Actor("human", "compliance-admin"),
                     Actor("human", "operations-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                     Actor("ai", "policy-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(DisableSubmit()),
                deniedActor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task DisableApprovalShouldRequireHumanPolicyAdminAndDistinctApprover()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (ChatBotAuthenticatedActor allowedActor in new[]
                 {
                     Actor("human", "policy-admin"),
                     Actor("human", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(DisableApproval()),
                allowedActor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }

        // RequesterRef == ApproverRef is rejected at the gateway (two-person rule, first of three checks).
        ChatBotAuthorizationResult selfApproval = await stage.AuthorizeAsync(
            Submission(DisableApproval() with { ApproverRef = "admin-requester" }),
            Actor("human", "policy-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        selfApproval.IsAllowed.ShouldBeFalse();

        foreach (ChatBotAuthenticatedActor deniedActor in new[]
                 {
                     Actor("human", "mailbox-admin"),
                     Actor("human", "compliance-admin"),
                     Actor("human", "operations-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(DisableApproval()),
                deniedActor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task DisableCommandsShouldRejectInvalidMetadataOnlyPayloads()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitAiActorDisable invalid in new[]
                 {
                     DisableSubmit() with { SourceVersion = -1 },
                     DisableSubmit() with { SchemaVersion = "ai-actor-control-schema.custom" },
                     DisableSubmit() with { ReasonCode = "unsafe reason" },
                     DisableSubmit() with { NewState = AiActorControlState.Active },
                     DisableSubmit() with { OldState = AiActorControlState.Disabled },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(invalid),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    private static SubmitAiActorDisable DisableSubmit()
        => new(
            "ai-actor-disable-001",
            "ai-actor:gpt-mediation-actor",
            "ai-actor-unsafe-proposals",
            "policy-snapshot:policy-admin:v1",
            AiActorControlState.Active,
            AiActorControlState.Disabled,
            4,
            "admin-requester",
            AiActorControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveAiActorDisable DisableApproval()
        => new(
            "ai-actor-disable-001",
            "ai-actor:gpt-mediation-actor",
            "ai-actor-unsafe-proposals",
            "policy-snapshot:policy-admin:v1",
            AiActorControlState.Active,
            AiActorControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            AiActorControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ChatBotCommandSubmission Submission(object command)
        => new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = command.GetType().Name,
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
