using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using SubmitCommandCapabilityQuarantine = Hexalith.ChatBot.Contracts.Commands.SubmitCommandCapabilityQuarantine;
using ApproveCommandCapabilityQuarantine = Hexalith.ChatBot.Contracts.Commands.ApproveCommandCapabilityQuarantine;
using CommandCapabilityControlState = Hexalith.ChatBot.Contracts.Enums.CommandCapabilityControlState;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class CommandCapabilityQuarantineAuthorizationTests
{
    private const string Tenant = "tenant-alpha";

    // The quarantined command-capability subject is a sibling first-party command type, NOT an FR74 governance command.
    private const string QuarantinedCapability = nameof(Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject);

    [Fact]
    public async Task QuarantineProposalShouldRequireHumanPolicyAdmin()
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
                Submission(QuarantineSubmit()),
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
                Submission(QuarantineSubmit()),
                deniedActor,
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task QuarantineApprovalShouldRequireHumanPolicyAdminAndDistinctApprover()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (ChatBotAuthenticatedActor allowedActor in new[]
                 {
                     Actor("human", "policy-admin"),
                     Actor("human", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(QuarantineApproval()),
                allowedActor,
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }

        // RequesterRef == ApproverRef is rejected at the gateway (two-person rule, first of three checks).
        ChatBotAuthorizationResult selfApproval = await stage.AuthorizeAsync(
            Submission(QuarantineApproval() with { ApproverRef = "admin-requester" }),
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
                Submission(QuarantineApproval()),
                deniedActor,
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task QuarantineCommandsShouldRejectInvalidMetadataOnlyPayloads()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitCommandCapabilityQuarantine invalid in new[]
                 {
                     QuarantineSubmit() with { SourceVersion = -1 },
                     QuarantineSubmit() with { SchemaVersion = "command-capability-control-schema.custom" },
                     QuarantineSubmit() with { ReasonCode = "unsafe reason" },
                     QuarantineSubmit() with { NewState = CommandCapabilityControlState.Active },
                     QuarantineSubmit() with { OldState = CommandCapabilityControlState.Quarantined },
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
    public async Task SelfLockoutGuardShouldRejectQuarantiningAnFr74GovernanceCommand()
    {
        ParticipantAuthorizationStage stage = new();

        // An admin cannot quarantine the very commands needed to govern/reverse a quarantine: a propose/approve whose
        // CommandCapabilityRef names an FR74 governance/two-person command — including the quarantine commands
        // themselves — is rejected at the gateway validator.
        foreach (string governanceRef in new[]
                 {
                     nameof(SubmitCommandCapabilityQuarantine),
                     nameof(ApproveCommandCapabilityQuarantine),
                     nameof(Hexalith.ChatBot.Contracts.Commands.SubmitCommandCapabilityDisable),
                     nameof(Hexalith.ChatBot.Contracts.Commands.SubmitAiActorDisable),
                     nameof(Hexalith.ChatBot.Contracts.Commands.ApproveServiceClientDisable),
                     nameof(Hexalith.ChatBot.Contracts.Commands.SubmitMailboxSourceQuarantine),
                 })
        {
            ChatBotAuthorizationResult deniedProposal = await stage.AuthorizeAsync(
                Submission(QuarantineSubmit() with { CommandCapabilityRef = governanceRef }),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            deniedProposal.IsAllowed.ShouldBeFalse();
            deniedProposal.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);

            ChatBotAuthorizationResult deniedApproval = await stage.AuthorizeAsync(
                Submission(QuarantineApproval() with { CommandCapabilityRef = governanceRef }),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            deniedApproval.IsAllowed.ShouldBeFalse();
            deniedApproval.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task QuarantinedCommandCapabilityShouldFailClosedForEveryActorBeforeGrantValidation()
    {
        // The controlled-capability check is the FIRST check in AuthorizeAsync. To prove it runs BEFORE grant
        // validation / admin-scope branches / spine allowlist, inject a grant validator that would otherwise deny
        // with a different sentinel reason: the precise `command_capability_quarantined` reason must still win — for
        // a human, a service actor, AND an AI actor (all-actor coverage).
        FakeCommandCapabilityControlStateProvider provider = new();
        provider.Quarantine(Tenant, QuarantinedCapability);
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
                Submission(new object(), QuarantinedCapability),
                actor,
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandCapabilityQuarantined);

            // The quarantine reason is DISTINCT from disable, the global static spine allowlist, the per-grant
            // allowlist, and every per-actor control reason.
            denied.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.CommandCapabilityDisabled);
            denied.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.CommandNotAllowlisted);
            denied.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped);
            denied.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.AiActorQuarantined);
        }

        // The provider only ever receives the safe tenant id and command type name — never any credential/grant/PII.
        provider.ObservedRequests.ShouldAllBe(request =>
            string.Equals(request.TenantId, Tenant, StringComparison.Ordinal) &&
            string.Equals(request.CommandCapabilityRef, QuarantinedCapability, StringComparison.Ordinal));
    }

    [Fact]
    public async Task QuarantinedCapabilityShouldNotAffectSiblingActiveTypeOrOtherTenants()
    {
        FakeCommandCapabilityControlStateProvider provider = new();
        provider.Quarantine(Tenant, QuarantinedCapability);
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
            Submission(new object(), QuarantinedCapability),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-beta"),
            TestContext.Current.CancellationToken);
        otherTenant.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task QuarantinedCapabilityCheckShouldExemptFr74GovernanceCommands()
    {
        // Even if a fake somehow reports an FR74 governance command as quarantined, the enforcement seam exempts it
        // so the tenant can still govern/reverse a quarantine (defense-in-depth with the self-lockout guard). A valid
        // SubmitCommandCapabilityQuarantine from a policy-admin therefore stays admittable.
        FakeCommandCapabilityControlStateProvider provider = new();
        provider.Quarantine(Tenant, nameof(SubmitCommandCapabilityQuarantine));
        ParticipantAuthorizationStage stage = new(commandCapabilityControlStateProvider: provider);

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(QuarantineSubmit()),
            Actor("human", "policy-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task ControlStateCheckShouldKeepDisabledAndQuarantinedReasonsDistinctOffOneProviderRead()
    {
        // Both control-state branches coexist off a SINGLE provider read (the Story 7.22 switch). A Disabled subject
        // still returns command_capability_disabled; a Quarantined subject returns command_capability_quarantined.
        FakeCommandCapabilityControlStateProvider provider = new();
        provider.Disable(Tenant, nameof(Hexalith.ChatBot.Contracts.Commands.MarkEmailAssociationNeedsReview));
        provider.Quarantine(Tenant, QuarantinedCapability);
        ParticipantAuthorizationStage stage = new(commandCapabilityControlStateProvider: provider);

        ChatBotAuthorizationResult disabled = await stage.AuthorizeAsync(
            Submission(new object(), nameof(Hexalith.ChatBot.Contracts.Commands.MarkEmailAssociationNeedsReview)),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        disabled.IsAllowed.ShouldBeFalse();
        disabled.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandCapabilityDisabled);

        ChatBotAuthorizationResult quarantined = await stage.AuthorizeAsync(
            Submission(new object(), QuarantinedCapability),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        quarantined.IsAllowed.ShouldBeFalse();
        quarantined.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandCapabilityQuarantined);
    }

    private static SubmitCommandCapabilityQuarantine QuarantineSubmit()
        => new(
            "command-capability-quarantine-001",
            QuarantinedCapability,
            "command-capability-unsafe-execution",
            "policy-snapshot:policy-admin:v1",
            CommandCapabilityControlState.Active,
            CommandCapabilityControlState.Quarantined,
            4,
            "admin-requester",
            CommandCapabilityControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ApproveCommandCapabilityQuarantine QuarantineApproval()
        => new(
            "command-capability-quarantine-001",
            QuarantinedCapability,
            "command-capability-unsafe-execution",
            "policy-snapshot:policy-admin:v1",
            CommandCapabilityControlState.Active,
            CommandCapabilityControlState.Quarantined,
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
        private readonly Dictionary<string, CommandCapabilityControlState> _controlled = new(StringComparer.Ordinal);

        public List<(string TenantId, string CommandCapabilityRef)> ObservedRequests { get; } = [];

        public void Disable(string tenantId, string commandCapabilityRef)
            => _controlled[$"{tenantId}|{commandCapabilityRef}"] = CommandCapabilityControlState.Disabled;

        public void Quarantine(string tenantId, string commandCapabilityRef)
            => _controlled[$"{tenantId}|{commandCapabilityRef}"] = CommandCapabilityControlState.Quarantined;

        public ValueTask<CommandCapabilityControlState> GetControlStateAsync(
            string tenantId,
            string commandCapabilityRef,
            CancellationToken cancellationToken)
        {
            ObservedRequests.Add((tenantId, commandCapabilityRef));
            return ValueTask.FromResult(_controlled.TryGetValue($"{tenantId}|{commandCapabilityRef}", out CommandCapabilityControlState state)
                ? state
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
