using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using SubmitCommandCapabilityDisable = Hexalith.ChatBot.Contracts.Commands.SubmitCommandCapabilityDisable;
using ApproveCommandCapabilityDisable = Hexalith.ChatBot.Contracts.Commands.ApproveCommandCapabilityDisable;
using CommandCapabilityControlState = Hexalith.ChatBot.Contracts.Enums.CommandCapabilityControlState;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class CommandCapabilityDisableAuthorizationTests
{
    private const string Tenant = "tenant-alpha";

    // The disabled command-capability subject is a sibling first-party command type, NOT an FR74 governance command.
    private const string DisabledCapability = nameof(Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject);

    [Fact]
    public async Task DisableProposalShouldRequireHumanPolicyAdmin()
    {
        ParticipantAuthorizationStage stage = new();

        // Command-capability governance is the policy-admin's domain (the "security engineer" persona maps to
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

        foreach (SubmitCommandCapabilityDisable invalid in new[]
                 {
                     DisableSubmit() with { SourceVersion = -1 },
                     DisableSubmit() with { SchemaVersion = "command-capability-control-schema.custom" },
                     DisableSubmit() with { ReasonCode = "unsafe reason" },
                     DisableSubmit() with { NewState = CommandCapabilityControlState.Active },
                     DisableSubmit() with { OldState = CommandCapabilityControlState.Disabled },
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

    [Fact]
    public async Task SelfLockoutGuardShouldRejectDisablingAnFr74GovernanceCommand()
    {
        ParticipantAuthorizationStage stage = new();

        // An admin cannot disable the very commands needed to govern/reverse a disable: a propose/approve whose
        // CommandCapabilityRef names an FR74 governance/two-person command is rejected at the gateway validator.
        foreach (string governanceRef in new[]
                 {
                     nameof(SubmitCommandCapabilityDisable),
                     nameof(ApproveCommandCapabilityDisable),
                     nameof(Hexalith.ChatBot.Contracts.Commands.SubmitAiActorDisable),
                     nameof(Hexalith.ChatBot.Contracts.Commands.ApproveServiceClientDisable),
                     nameof(Hexalith.ChatBot.Contracts.Commands.SubmitMailboxSourceQuarantine),
                 })
        {
            ChatBotAuthorizationResult deniedProposal = await stage.AuthorizeAsync(
                Submission(DisableSubmit() with { CommandCapabilityRef = governanceRef }),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            deniedProposal.IsAllowed.ShouldBeFalse();
            deniedProposal.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);

            ChatBotAuthorizationResult deniedApproval = await stage.AuthorizeAsync(
                Submission(DisableApproval() with { CommandCapabilityRef = governanceRef }),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            deniedApproval.IsAllowed.ShouldBeFalse();
            deniedApproval.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task DisabledCommandCapabilityShouldFailClosedForEveryActorBeforeGrantValidation()
    {
        // The disabled-capability check is the FIRST check in AuthorizeAsync. To prove it runs BEFORE grant
        // validation / admin-scope branches / spine allowlist, inject a grant validator that would otherwise deny
        // with a different sentinel reason: the precise `command_capability_disabled` reason must still win.
        FakeCommandCapabilityControlStateProvider provider = new();
        provider.Disable(Tenant, DisabledCapability);
        ParticipantAuthorizationStage stage = new(
            serviceClientGrantValidator: new SentinelDenyingServiceClientGrantValidator(),
            commandCapabilityControlStateProvider: provider);

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "tenant-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(new object(), DisabledCapability),
                actor,
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandCapabilityDisabled);
        }

        // The provider only ever receives the safe tenant id and command type name — never any credential/grant/PII.
        provider.ObservedRequests.ShouldAllBe(request =>
            string.Equals(request.TenantId, Tenant, StringComparison.Ordinal) &&
            string.Equals(request.CommandCapabilityRef, DisabledCapability, StringComparison.Ordinal));
    }

    [Fact]
    public async Task DisabledCapabilityShouldNotAffectSiblingActiveTypeOrOtherTenants()
    {
        FakeCommandCapabilityControlStateProvider provider = new();
        provider.Disable(Tenant, DisabledCapability);
        ParticipantAuthorizationStage stage = new(commandCapabilityControlStateProvider: provider);

        // A sibling, still-Active command type for the same tenant is unaffected (isolation).
        ChatBotAuthorizationResult sibling = await stage.AuthorizeAsync(
            Submission(new object(), nameof(Hexalith.ChatBot.Contracts.Commands.MarkEmailAssociationNeedsReview)),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        sibling.IsAllowed.ShouldBeTrue();

        // The SAME command type under a DIFFERENT tenant is unaffected (per-tenant isolation).
        ChatBotAuthorizationResult otherTenant = await stage.AuthorizeAsync(
            Submission(new object(), DisabledCapability),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-beta"),
            TestContext.Current.CancellationToken);
        otherTenant.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task DisabledCapabilityCheckShouldExemptFr74GovernanceCommands()
    {
        // Even if a fake somehow reports an FR74 governance command as disabled, the enforcement seam exempts it so
        // the tenant can still govern/reverse a disable (defense-in-depth with the self-lockout guard). A valid
        // SubmitCommandCapabilityDisable from a policy-admin therefore stays admittable.
        FakeCommandCapabilityControlStateProvider provider = new();
        provider.Disable(Tenant, nameof(SubmitCommandCapabilityDisable));
        ParticipantAuthorizationStage stage = new(commandCapabilityControlStateProvider: provider);

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(DisableSubmit()),
            Actor("human", "policy-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();
    }

    private static SubmitCommandCapabilityDisable DisableSubmit()
        => new(
            "command-capability-disable-001",
            DisabledCapability,
            "command-capability-unsafe-execution",
            "policy-snapshot:policy-admin:v1",
            CommandCapabilityControlState.Active,
            CommandCapabilityControlState.Disabled,
            4,
            "admin-requester",
            CommandCapabilityControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveCommandCapabilityDisable DisableApproval()
        => new(
            "command-capability-disable-001",
            DisabledCapability,
            "command-capability-unsafe-execution",
            "policy-snapshot:policy-admin:v1",
            CommandCapabilityControlState.Active,
            CommandCapabilityControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            CommandCapabilityControlSchemaVersions.V1,
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

    private sealed class FakeCommandCapabilityControlStateProvider : ICommandCapabilityControlStateProvider
    {
        private readonly HashSet<string> _disabled = new(StringComparer.Ordinal);

        public List<(string TenantId, string CommandCapabilityRef)> ObservedRequests { get; } = [];

        public void Disable(string tenantId, string commandCapabilityRef)
            => _disabled.Add($"{tenantId}|{commandCapabilityRef}");

        public ValueTask<CommandCapabilityControlState> GetControlStateAsync(
            string tenantId,
            string commandCapabilityRef,
            CancellationToken cancellationToken)
        {
            ObservedRequests.Add((tenantId, commandCapabilityRef));
            return ValueTask.FromResult(_disabled.Contains($"{tenantId}|{commandCapabilityRef}")
                ? CommandCapabilityControlState.Disabled
                : CommandCapabilityControlState.Active);
        }
    }

    private sealed class SentinelDenyingServiceClientGrantValidator : IServiceClientGrantValidator
    {
        public ValueTask<ChatBotAuthorizationResult> ValidateAsync(
            ChatBotCommandSubmission submission,
            ChatBotAuthenticatedActor actor,
            ChatBotTenantBinding tenantBinding,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(ChatBotAuthorizationResult.Denied("sentinel_grant_denied"));
    }
}
