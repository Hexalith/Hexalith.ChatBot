using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using SubmitMailboxSourceDisable = Hexalith.ChatBot.Contracts.Commands.SubmitMailboxSourceDisable;
using ApproveMailboxSourceDisable = Hexalith.ChatBot.Contracts.Commands.ApproveMailboxSourceDisable;
using MailboxSourceControlState = Hexalith.ChatBot.Contracts.Enums.MailboxSourceControlState;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class MailboxSourceDisableAuthorizationTests
{
    [Fact]
    public async Task DisableProposalShouldRequireHumanMailboxScope()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (ChatBotAuthenticatedActor allowedActor in new[]
                 {
                     Actor("human", "mailbox-admin"),
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

        foreach (ChatBotAuthenticatedActor deniedActor in new[]
                 {
                     Actor("human", "policy-admin"),
                     Actor("human", "compliance-admin"),
                     Actor("human", "operations-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
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
    public async Task DisableApprovalShouldRequireHumanMailboxScopeAndDistinctApprover()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(DisableApproval()),
            Actor("human", "mailbox-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();

        ChatBotAuthorizationResult selfApproval = await stage.AuthorizeAsync(
            Submission(DisableApproval() with { ApproverRef = "admin-requester" }),
            Actor("human", "mailbox-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        selfApproval.IsAllowed.ShouldBeFalse();

        foreach (ChatBotAuthenticatedActor deniedActor in new[]
                 {
                     Actor("human", "policy-admin"),
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

        foreach (SubmitMailboxSourceDisable invalid in new[]
                 {
                     DisableSubmit() with { SourceVersion = -1 },
                     DisableSubmit() with { SchemaVersion = "mailbox-source-control-schema.custom" },
                     DisableSubmit() with { ReasonCode = "unsafe reason" },
                     DisableSubmit() with { NewState = MailboxSourceControlState.Active },
                     DisableSubmit() with { OldState = MailboxSourceControlState.Disabled },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(invalid),
                Actor("human", "mailbox-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    private static SubmitMailboxSourceDisable DisableSubmit()
        => new(
            "mailbox-disable-001",
            "mailbox-source:controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot:mailbox:v1",
            MailboxSourceControlState.Active,
            MailboxSourceControlState.Disabled,
            4,
            "admin-requester",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveMailboxSourceDisable DisableApproval()
        => new(
            "mailbox-disable-001",
            "mailbox-source:controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot:mailbox:v1",
            MailboxSourceControlState.Active,
            MailboxSourceControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            MailboxSourceControlSchemaVersions.V1,
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
