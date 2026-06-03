using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

/// <summary>
/// Story 7.27 AC8: CLI-class, MCP-class, and (generic) service-client actor types flow through the SAME shared
/// command pipeline (ServiceClientGrantValidator + ParticipantAuthorizationStage) and are subject to the SAME
/// disable / quarantine / rate-limit isolation as any human/AI actor — no surface bypasses a control plane. The
/// control planes are keyed by (tenant, service-client id), independent of client class, so the cli-automation
/// coverage in <c>ServiceClientGrantAuthorizationTests</c> and the mcp-tool coverage here are equivalent. These
/// tests additionally prove the recorded actorType and UI/CLI/MCP parity for ≥1 isolation scenario (FR81a/FR84).
/// </summary>
public sealed class CrossActorTypeIsolationParityTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    public static TheoryData<string, string> ServiceClientSurfaces => new()
    {
        { "cli-automation", "cli" },
        { "mcp-tool", "mcp" },
    };

    [Theory]
    [MemberData(nameof(ServiceClientSurfaces))]
    public async Task DisabledServiceClientShouldFailClosedForEveryClientClass(string clientClass, string surface)
    {
        ParticipantAuthorizationStage stage = Stage(new FakeControlStateProvider(ServiceClientControlState.Disabled));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            Submission(surface),
            ServiceActor(clientClass, surface, Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote))),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientDisabled);
        result.ServiceClientGrantEvidence.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(ServiceClientSurfaces))]
    public async Task QuarantinedServiceClientShouldFailClosedForEveryClientClass(string clientClass, string surface)
    {
        ParticipantAuthorizationStage stage = Stage(new FakeControlStateProvider(ServiceClientControlState.Quarantined));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            Submission(surface),
            ServiceActor(clientClass, surface, Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote))),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientQuarantined);
        result.ServiceClientGrantEvidence.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(ServiceClientSurfaces))]
    public async Task RateLimitedServiceClientShouldFailClosedForEveryClientClass(string clientClass, string surface)
    {
        ParticipantAuthorizationStage stage = Stage(
            rateLimitProvider: new FakeRateLimitProvider(new ServiceClientRateLimitState(2, ServiceClientRateLimitWindow.RollingHour)),
            commandHistory: new FakeCommandHistory([Now.AddMinutes(-10), Now.AddMinutes(-20)]));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            Submission(surface),
            ServiceActor(clientClass, surface, Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote))),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientRateLimited);
    }

    [Fact]
    public async Task DisabledMcpAiActorShouldFailClosedWithDistinctAiReason()
    {
        // The MCP surface does not exempt an AI actor: a disabled mcp-tool AI actor fails closed with the precise
        // ai_actor_disabled reason (not service_client_disabled), same as the cli-automation AI actor.
        ParticipantAuthorizationStage stage = Stage(
            aiActorControlStateProvider: new FakeAiActorControlStateProvider(AiActorControlState.Disabled));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            Submission("mcp"),
            AiActor("mcp-tool", "mcp", Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote))),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AiActorDisabled);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientDisabled);
    }

    [Fact]
    public async Task CliAndMcpShouldYieldEquivalentAuthorizationOutcomeAndActorTypeForTheSameIsolationScenario()
    {
        // FR84/FR81a parity: for one isolation scenario (active, in-scope) the CLI and MCP surfaces produce an
        // equivalent ALLOW outcome with the SAME recorded actorType ("service") — differing only by surface origin.
        ParticipantAuthorizationStage stage = Stage();

        AuditEnvelope cli = await AdmitAndAuditAsync(stage, "cli-automation", "cli");
        AuditEnvelope mcp = await AdmitAndAuditAsync(stage, "mcp-tool", "mcp");

        cli.ActorType.ShouldBe("service");
        mcp.ActorType.ShouldBe(cli.ActorType);
        cli.SurfaceOrigin.ShouldBe("cli");
        mcp.SurfaceOrigin.ShouldBe("mcp");
        cli.StateTransition.ShouldBe(mcp.StateTransition);
        cli.Decision.ShouldBe(mcp.Decision);
        mcp.SourceEvidenceRefs.ShouldContain("service-client:mcp-tool-client");
        cli.SourceEvidenceRefs.ShouldContain("service-client:cli-automation-client");
    }

    private async Task<AuditEnvelope> AdmitAndAuditAsync(
        ParticipantAuthorizationStage stage,
        string clientClass,
        string surface)
    {
        ChatBotCommandSubmission submission = Submission(surface);
        ChatBotAuthenticatedActor actor = ServiceActor(
            clientClass,
            surface,
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.IsAllowed.ShouldBeTrue();

        ChatBotGatewayContext context = new(submission, actor, new ChatBotTenantBinding("tenant-alpha"), result.ServiceClientGrantEvidence);
        return AuditEnvelopeFactory.PreCommit(
            context,
            new LifecycleTransitionDefinition(LifecycleStates.Received, LifecycleStates.Proposed),
            Now);
    }

    private static ParticipantAuthorizationStage Stage(
        IServiceClientControlStateProvider? controlStateProvider = null,
        IServiceClientRateLimitProvider? rateLimitProvider = null,
        IServiceClientCommandHistory? commandHistory = null,
        IAiActorControlStateProvider? aiActorControlStateProvider = null)
    {
        FixedClock clock = new(Now);
        return new ParticipantAuthorizationStage(
            serviceClientGrantValidator: new ServiceClientGrantValidator(
                new ClaimsServiceClientGrantResolver(),
                clock,
                new ChatBotSpineCommandAllowlist(),
                controlStateProvider,
                rateLimitProvider,
                commandHistory,
                aiActorControlStateProvider));
    }

    private static ChatBotCommandSubmission Submission(string surface)
        => new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "service-account")], "test")),
            new Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = nameof(RecordGovernedNote),
                Command = new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                RequestSchemaVersion = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            null,
            string.Equals(surface, "mcp", StringComparison.Ordinal) ? ChatBotSurfaceOrigin.Mcp : ChatBotSurfaceOrigin.Cli);

    private static ChatBotAuthenticatedActor ServiceActor(string clientClass, string surface, params Claim[] overrides)
        => Actor(ParticipantAuthorizationStage.ServiceActorValue, clientClass, surface, overrides);

    private static ChatBotAuthenticatedActor AiActor(string clientClass, string surface, params Claim[] overrides)
        => Actor(ParticipantAuthorizationStage.AiActorValue, clientClass, surface, overrides);

    private static ChatBotAuthenticatedActor Actor(string actorType, string clientClass, string surface, params Claim[] overrides)
    {
        string clientId = $"{clientClass}-client";
        List<Claim> claims =
        [
            new("sub", "service-account"),
            new(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
            new(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, clientId),
            new(ClaimsServiceClientGrantResolver.ServiceClientClassClaim, clientClass),
            new(ClaimsServiceClientGrantResolver.GrantIdClaim, "01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new(ClaimsServiceClientGrantResolver.GrantTenantClaim, "tenant-alpha"),
            new(ClaimsServiceClientGrantResolver.GrantExpiryClaim, "2026-06-01T13:00:00Z"),
            new(ClaimsServiceClientGrantResolver.GrantScopeClaim, "notes.write"),
            new(ClaimsServiceClientGrantResolver.GrantSurfaceClaim, surface),
            new(ClaimsServiceClientGrantResolver.CommandSetVersionClaim, "command-set-v1"),
        ];

        foreach (string type in overrides.Select(static claim => claim.Type).Distinct(StringComparer.Ordinal))
        {
            _ = claims.RemoveAll(claim => string.Equals(claim.Type, type, StringComparison.Ordinal));
        }

        claims.AddRange(overrides);
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, "test"));
        return new ChatBotAuthenticatedActor("service-account", principal, actorType, clientId);
    }

    private static Claim Claim(string type, string value) => new(type, value);

    private sealed class FakeControlStateProvider(ServiceClientControlState state) : IServiceClientControlStateProvider
    {
        public ValueTask<ServiceClientControlState> GetControlStateAsync(string tenantId, string serviceClientId, CancellationToken cancellationToken)
            => ValueTask.FromResult(state);
    }

    private sealed class FakeAiActorControlStateProvider(AiActorControlState state) : IAiActorControlStateProvider
    {
        public ValueTask<AiActorControlState> GetControlStateAsync(string tenantId, string aiActorId, CancellationToken cancellationToken)
            => ValueTask.FromResult(state);
    }

    private sealed class FakeRateLimitProvider(ServiceClientRateLimitState state) : IServiceClientRateLimitProvider
    {
        public ValueTask<ServiceClientRateLimitState?> GetRateLimitAsync(string tenantId, string serviceClientId, CancellationToken cancellationToken)
            => ValueTask.FromResult<ServiceClientRateLimitState?>(state);
    }

    private sealed class FakeCommandHistory(IReadOnlyList<DateTimeOffset> timestamps) : IServiceClientCommandHistory
    {
        public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(string tenantId, string serviceClientId, CancellationToken cancellationToken)
            => ValueTask.FromResult(timestamps);
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
