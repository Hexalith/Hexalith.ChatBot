using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using SubmitOutboundChannelDisable = Hexalith.ChatBot.Contracts.Commands.SubmitOutboundChannelDisable;
using ApproveOutboundChannelDisable = Hexalith.ChatBot.Contracts.Commands.ApproveOutboundChannelDisable;
using OutboundChannelControlState = Hexalith.ChatBot.Contracts.Enums.OutboundChannelControlState;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class OutboundChannelDisableAuthorizationTests
{
    private const string Tenant = "tenant-alpha";

    // The single M0/M1 outbound channel: the M365 mailbox outbound adapter, identified by its safe AdapterRef token.
    private const string OutboundChannel = "adapter:mailbox-outbound";

    [Fact]
    public async Task DisableProposalShouldRequireHumanPolicyAdmin()
    {
        ParticipantAuthorizationStage stage = new();

        // Outbound-channel governance is the policy-admin's domain (the "policy administrator" persona maps to
        // AdminScope.Policy). A policy-admin is allowed; a tenant-admin is also allowed via the FR75a scope union.
        foreach (ChatBotAuthenticatedActor allowedActor in new[]
                 {
                     Actor("human", "policy-admin"),
                     Actor("human", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(DisableSubmit()),
                allowedActor,
                new ChatBotTenantBinding(Tenant),
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
                new ChatBotTenantBinding(Tenant),
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
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }

        // RequesterRef == ApproverRef is rejected at the gateway (two-person rule, first of three checks).
        ChatBotAuthorizationResult selfApproval = await stage.AuthorizeAsync(
            Submission(DisableApproval() with { ApproverRef = "admin-requester" }),
            Actor("human", "policy-admin"),
            new ChatBotTenantBinding(Tenant),
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
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task DisableCommandsShouldRejectInvalidMetadataOnlyPayloads()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitOutboundChannelDisable invalid in new[]
                 {
                     DisableSubmit() with { SourceVersion = -1 },
                     DisableSubmit() with { SchemaVersion = "outbound-channel-control-schema.custom" },
                     DisableSubmit() with { ReasonCode = "unsafe reason" },
                     DisableSubmit() with { NewState = OutboundChannelControlState.Active },
                     DisableSubmit() with { OldState = OutboundChannelControlState.Disabled },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(invalid),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    private static SubmitOutboundChannelDisable DisableSubmit()
        => new(
            "outbound-channel-disable-001",
            OutboundChannel,
            "outbound-channel-policy-violation",
            "policy-snapshot:policy-admin:v1",
            OutboundChannelControlState.Active,
            OutboundChannelControlState.Disabled,
            4,
            "admin-requester",
            OutboundChannelControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveOutboundChannelDisable DisableApproval()
        => new(
            "outbound-channel-disable-001",
            OutboundChannel,
            "outbound-channel-policy-violation",
            "policy-snapshot:policy-admin:v1",
            OutboundChannelControlState.Active,
            OutboundChannelControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            OutboundChannelControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ChatBotCommandSubmission Submission(object command)
        => Submission(command, command.GetType().Name);

    private static ChatBotCommandSubmission Submission(object command, string commandType)
        => new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = commandType,
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
