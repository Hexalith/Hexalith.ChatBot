using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using SubmitServiceClientQuarantine = Hexalith.ChatBot.Contracts.Commands.SubmitServiceClientQuarantine;
using ApproveServiceClientQuarantine = Hexalith.ChatBot.Contracts.Commands.ApproveServiceClientQuarantine;
using ServiceClientControlState = Hexalith.ChatBot.Contracts.Enums.ServiceClientControlState;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class ServiceClientQuarantineAuthorizationTests
{
    [Fact]
    public async Task QuarantineProposalShouldRequireHumanTenantAdmin()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(QuarantineSubmit()),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();

        // Service-client governance is a TenantAdmin responsibility: every non-tenant-admin scope is denied,
        // and service/AI actors are denied even with tenant-admin-looking claims.
        foreach (ChatBotAuthenticatedActor deniedActor in new[]
                 {
                     Actor("human", "mailbox-admin"),
                     Actor("human", "policy-admin"),
                     Actor("human", "compliance-admin"),
                     Actor("human", "operations-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(QuarantineSubmit()),
                deniedActor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task QuarantineApprovalShouldRequireHumanTenantAdminAndDistinctApprover()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(QuarantineApproval()),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();

        // RequesterRef == ApproverRef is rejected at the gateway (two-person rule, first of three checks).
        ChatBotAuthorizationResult selfApproval = await stage.AuthorizeAsync(
            Submission(QuarantineApproval() with { ApproverRef = "admin-requester" }),
            Actor("human", "tenant-admin"),
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
                Submission(QuarantineApproval()),
                deniedActor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task QuarantineCommandsShouldRejectInvalidMetadataOnlyPayloads()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitServiceClientQuarantine invalid in new[]
                 {
                     QuarantineSubmit() with { SourceVersion = -1 },
                     QuarantineSubmit() with { SchemaVersion = "service-client-control-schema.custom" },
                     QuarantineSubmit() with { ReasonCode = "unsafe reason" },
                     QuarantineSubmit() with { NewState = ServiceClientControlState.Active },
                     QuarantineSubmit() with { OldState = ServiceClientControlState.Quarantined },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(invalid),
                Actor("human", "tenant-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    private static SubmitServiceClientQuarantine QuarantineSubmit()
        => new(
            "service-client-quarantine-001",
            "service-client:cli-automation-client",
            "service-client-unsafe-activity",
            "policy-snapshot:tenant-admin:v1",
            ServiceClientControlState.Active,
            ServiceClientControlState.Quarantined,
            4,
            "admin-requester",
            ServiceClientControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveServiceClientQuarantine QuarantineApproval()
        => new(
            "service-client-quarantine-001",
            "service-client:cli-automation-client",
            "service-client-unsafe-activity",
            "policy-snapshot:tenant-admin:v1",
            ServiceClientControlState.Active,
            ServiceClientControlState.Quarantined,
            5,
            "admin-requester",
            "admin-approver",
            ServiceClientControlSchemaVersions.V1,
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
