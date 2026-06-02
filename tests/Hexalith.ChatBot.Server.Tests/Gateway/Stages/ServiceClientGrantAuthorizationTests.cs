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

public sealed class ServiceClientGrantAuthorizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ClaimsAuthenticationStageShouldClassifyKeycloakServiceAccountPosture()
    {
        ClaimsAuthenticationStage stage = new();
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "service-account-cli"),
                new Claim("preferred_username", "service-account-cli-automation-client"),
            ],
            "test"));

        ChatBotAuthenticationResult result = await stage.AuthenticateAsync(
            Submission(principal, ChatBotSurfaceOrigin.Cli),
            TestContext.Current.CancellationToken);

        result.IsAuthenticated.ShouldBeTrue();
        result.Actor.ShouldNotBeNull().ActorType.ShouldBe(ParticipantAuthorizationStage.ServiceActorValue);
        result.Actor.ServiceClientId.ShouldBe("cli-automation-client");
    }

    [Fact]
    public async Task ClaimsAuthenticationStageShouldNotLetServiceAccountClaimHumanPosture()
    {
        ClaimsAuthenticationStage stage = new();
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "service-account-cli"),
                new Claim(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, "cli-automation-client"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
            ],
            "test"));

        ChatBotAuthenticationResult result = await stage.AuthenticateAsync(
            Submission(principal, ChatBotSurfaceOrigin.Cli),
            TestContext.Current.CancellationToken);

        result.IsAuthenticated.ShouldBeTrue();
        result.Actor.ShouldNotBeNull().ActorType.ShouldBe(ParticipantAuthorizationStage.ServiceActorValue);
        result.Actor.ServiceClientId.ShouldBe("cli-automation-client");
    }

    [Fact]
    public async Task ValidServiceClientGrantShouldAllowBoundCommandAndProduceMetadataEvidence()
    {
        ParticipantAuthorizationStage stage = Stage();
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.ServiceClientGrantEvidence.ShouldNotBeNull();
        result.ServiceClientGrantEvidence.ServiceClientId.ShouldBe("cli-automation-client");
        result.ServiceClientGrantEvidence.GrantId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        result.ServiceClientGrantEvidence.Scopes.ShouldBe(["notes.write"], ignoreOrder: false);
    }

    [Fact]
    public async Task ServiceClientGrantShouldMatchAuthenticatedServiceClientIdentity()
    {
        ParticipantAuthorizationStage stage = Stage();
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, "mcp-tool-client"),
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor with { ServiceClientId = "cli-automation-client" },
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing);
    }

    [Theory]
    [InlineData("missing-grant", ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing)]
    [InlineData("ambiguous-grant", ChatBotAuthorizationReasonCodes.ServiceClientGrantAmbiguous)]
    [InlineData("expired-grant", ChatBotAuthorizationReasonCodes.ServiceClientGrantExpired)]
    [InlineData("revoked-grant", ChatBotAuthorizationReasonCodes.ServiceClientGrantRevoked)]
    [InlineData("wrong-surface", ChatBotAuthorizationReasonCodes.ServiceClientWrongSurface)]
    [InlineData("under-scoped-command", ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped)]
    [InlineData("over-scoped-command", ChatBotAuthorizationReasonCodes.ServiceClientGrantOverScoped)]
    [InlineData("tenant-mismatch", ChatBotAuthorizationReasonCodes.ServiceClientGrantTenantMismatch)]
    public async Task InvalidServiceClientGrantShouldDenyBeforeDurableWork(string caseName, string expectedReason)
    {
        ParticipantAuthorizationStage stage = Stage();
        ChatBotCommandSubmission submission = caseName == "wrong-surface"
            ? Submission(ChatBotSurfaceOrigin.Mcp)
            : Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotTenantBinding binding = new(caseName == "tenant-mismatch" ? "tenant-beta" : "tenant-alpha");

        ChatBotAuthenticatedActor actor = caseName switch
        {
            "missing-grant" => ActorWithoutGrant(),
            "ambiguous-grant" => Actor(
                Claim(ClaimsServiceClientGrantResolver.GrantIdClaim, "01ARZ3NDEKTSV4RRFFQ69G5FAV"),
                Claim(ClaimsServiceClientGrantResolver.GrantIdClaim, "01ARZ3NDEKTSV4RRFFQ69G5FAW"),
                Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote))),
            "expired-grant" => Actor(
                Claim(ClaimsServiceClientGrantResolver.GrantExpiryClaim, "2026-06-01T11:59:00Z"),
                Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote))),
            "revoked-grant" => Actor(
                Claim(ClaimsServiceClientGrantResolver.GrantRevokedClaim, "true"),
                Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote))),
            "under-scoped-command" => Actor(
                Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(CaptureMailboxMessageIntake))),
            "over-scoped-command" => Actor(
                Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, "*")),
            _ => Actor(Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote))),
        };

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            binding,
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(expectedReason);
    }

    [Fact]
    public async Task ServiceClientShouldNotMutateThresholdPolicyEvenWithTenantAdminLookingClaim()
    {
        ParticipantAuthorizationStage stage = Stage();
        ChatBotCommandSubmission submission = Submission(
            ChatBotSurfaceOrigin.Cli,
            nameof(SetAssociationConfidenceThresholds),
            new SetAssociationConfidenceThresholds("association", 0.9, 0.6, "policy-v1", null, null));
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(SetAssociationConfidenceThresholds)),
            Claim(ParticipantAuthorizationStage.TenantRoleClaim, ParticipantAuthorizationStage.TenantAdminValue));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantOverScoped);
    }

    [Fact]
    public async Task ServiceClientGrantEvidenceShouldBeIncludedInAuditRefsWithoutSecrets()
    {
        ParticipantAuthorizationStage stage = Stage();
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)),
            Claim(ClaimsServiceClientGrantResolver.OAuthGrantEvidenceFingerprintClaim, "oauth-proof-01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            Claim(ClaimsServiceClientGrantResolver.DelegatedUserIdClaim, "actor-alpha"));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        ChatBotGatewayContext context = new(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            result.ServiceClientGrantEvidence);
        AuditEnvelope envelope = AuditEnvelopeFactory.PreCommit(
            context,
            new LifecycleTransitionDefinition(LifecycleStates.Received, LifecycleStates.Proposed),
            Now);

        envelope.ActorType.ShouldBe("service");
        envelope.SurfaceOrigin.ShouldBe("cli");
        envelope.SourceEvidenceRefs.ShouldContain("service-client:cli-automation-client");
        envelope.SourceEvidenceRefs.ShouldContain("grant:01ARZ3NDEKTSV4RRFFQ69G5FAV");
        envelope.SourceEvidenceRefs.ShouldContain("grant-scope:notes.write");
        envelope.SourceEvidenceRefs.ShouldContain("delegated-user:actor-alpha");
        envelope.SourceEvidenceRefs.ShouldContain("oauth-evidence:oauth-proof-01ARZ3NDEKTSV4RRFFQ69G5FAV");
        string serializedRefs = string.Join('|', envelope.SourceEvidenceRefs);
        serializedRefs.ShouldNotContain("bearer", Case.Insensitive);
        serializedRefs.ShouldNotContain("secret", Case.Insensitive);
        serializedRefs.ShouldNotContain("raw", Case.Insensitive);
    }

    [Fact]
    public async Task DisabledServiceClientShouldFailClosedBeforeGrantScopeChecks()
    {
        // The disabled control state must short-circuit before grant scope/allowlist checks: this grant is
        // under-scoped (would otherwise deny with service_client_grant_under_scoped), yet the FR74 disabled
        // reason wins, proving precedence. An OAuth fingerprint claim is present but never read or leaked.
        ParticipantAuthorizationStage stage = Stage(new FakeControlStateProvider(ServiceClientControlState.Disabled));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(CaptureMailboxMessageIntake)),
            Claim(ClaimsServiceClientGrantResolver.OAuthGrantEvidenceFingerprintClaim, "oauth-proof-01ARZ3NDEKTSV4RRFFQ69G5FAV"));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientDisabled);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantRevoked);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped);
        // Redacted, metadata-only denial: no grant evidence (and therefore no credential/OAuth fingerprint).
        result.ServiceClientGrantEvidence.ShouldBeNull();
    }

    [Fact]
    public async Task ActiveSiblingServiceClientShouldBeUnaffectedByDisabledPeer()
    {
        // Isolation: only "other-client" is disabled. The authenticated "cli-automation-client" with a valid
        // grant is admitted normally — one client's disabled control state never blocks another's.
        ParticipantAuthorizationStage stage = Stage(
            new FakeControlStateProvider(ServiceClientControlState.Disabled, onlyForServiceClientId: "other-client"));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.ServiceClientGrantEvidence.ShouldNotBeNull();
        result.ServiceClientGrantEvidence.ServiceClientId.ShouldBe("cli-automation-client");
    }

    [Fact]
    public async Task QuarantinedServiceClientShouldFailClosedBeforeGrantScopeChecks()
    {
        // The quarantined control state must short-circuit before grant scope/allowlist checks: this grant is
        // under-scoped (would otherwise deny with service_client_grant_under_scoped), yet the FR74 quarantined
        // reason wins, proving precedence. An OAuth fingerprint claim is present but never read or leaked. The
        // reason is distinct from both service_client_disabled and the Epic 5 service_client_grant_revoked.
        ParticipantAuthorizationStage stage = Stage(new FakeControlStateProvider(ServiceClientControlState.Quarantined));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(CaptureMailboxMessageIntake)),
            Claim(ClaimsServiceClientGrantResolver.OAuthGrantEvidenceFingerprintClaim, "oauth-proof-01ARZ3NDEKTSV4RRFFQ69G5FAV"));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientQuarantined);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientDisabled);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantRevoked);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped);
        // Redacted, metadata-only denial: no grant evidence (and therefore no credential/OAuth fingerprint).
        result.ServiceClientGrantEvidence.ShouldBeNull();
    }

    [Fact]
    public async Task ActiveSiblingServiceClientShouldBeUnaffectedByQuarantinedPeer()
    {
        // Isolation: only "other-client" is quarantined. The authenticated "cli-automation-client" with a valid
        // grant is admitted normally — one client's quarantined control state never blocks another's.
        ParticipantAuthorizationStage stage = Stage(
            new FakeControlStateProvider(ServiceClientControlState.Quarantined, onlyForServiceClientId: "other-client"));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.ServiceClientGrantEvidence.ShouldNotBeNull();
        result.ServiceClientGrantEvidence.ServiceClientId.ShouldBe("cli-automation-client");
    }

    [Fact]
    public async Task RateLimitedServiceClientShouldDenyAsFinalGateDistinctFromEverySecurityReason()
    {
        // Story 7.17: rate-limit is the FINAL admission gate. This command is otherwise fully admissible
        // (active control state, valid grant, in-surface, in-scope, in-allowlist) — only the budget denies it.
        // Budget = 2 with two admitted commands already in the trailing hour ⇒ count (2) >= budget (2) ⇒ throttled.
        // An OAuth fingerprint claim is present but never read or leaked by the rate-limit branch.
        ParticipantAuthorizationStage stage = Stage(
            rateLimitProvider: new FakeRateLimitProvider(new ServiceClientRateLimitState(2, ServiceClientRateLimitWindow.RollingHour)),
            commandHistory: new FakeCommandHistory([Now.AddMinutes(-10), Now.AddMinutes(-20)]));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)),
            Claim(ClaimsServiceClientGrantResolver.OAuthGrantEvidenceFingerprintClaim, "oauth-proof-01ARZ3NDEKTSV4RRFFQ69G5FAV"));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientRateLimited);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientDisabled);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientQuarantined);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantRevoked);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantExpired);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantOverScoped);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped);
        // Redacted, metadata-only denial: no grant evidence (and therefore no credential/OAuth fingerprint).
        result.ServiceClientGrantEvidence.ShouldBeNull();
    }

    [Fact]
    public async Task SecurityDenialShouldKeepItsPreciseReasonAndNeverBeMaskedByRateLimit()
    {
        // Even when a budget is configured and over, an under-scoped command must keep its precise security reason
        // (rate-limit is the LAST gate, reached only after scope/allowlist pass) — rate-limit never masks a denial.
        ParticipantAuthorizationStage stage = Stage(
            rateLimitProvider: new FakeRateLimitProvider(new ServiceClientRateLimitState(1, ServiceClientRateLimitWindow.RollingHour)),
            commandHistory: new FakeCommandHistory([Now.AddMinutes(-5), Now.AddMinutes(-15)]));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(CaptureMailboxMessageIntake)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped);
        result.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientRateLimited);
    }

    [Fact]
    public async Task UnderBudgetServiceClientShouldBeAdmittedNormally()
    {
        // Count (2) strictly under budget (5) ⇒ admitted; the Nth command that brings the window to the budget is
        // the one that gets denied next time, not this one.
        ParticipantAuthorizationStage stage = Stage(
            rateLimitProvider: new FakeRateLimitProvider(new ServiceClientRateLimitState(5, ServiceClientRateLimitWindow.RollingHour)),
            commandHistory: new FakeCommandHistory([Now.AddMinutes(-10), Now.AddMinutes(-20)]));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.ServiceClientGrantEvidence.ShouldNotBeNull();
        result.ServiceClientGrantEvidence.ServiceClientId.ShouldBe("cli-automation-client");
    }

    [Fact]
    public async Task SiblingServiceClientBudgetShouldNotThrottleAnotherClient()
    {
        // Isolation (NFR30): the budget/counter applies only to "other-client". The authenticated
        // "cli-automation-client" has no configured limit, so a noisy sibling never throttles or starves it.
        ParticipantAuthorizationStage stage = Stage(
            rateLimitProvider: new FakeRateLimitProvider(
                new ServiceClientRateLimitState(1, ServiceClientRateLimitWindow.RollingHour),
                onlyForServiceClientId: "other-client"),
            commandHistory: new FakeCommandHistory(
                [Now.AddMinutes(-1), Now.AddMinutes(-2), Now.AddMinutes(-3)],
                onlyForServiceClientId: "other-client"));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.ServiceClientGrantEvidence.ShouldNotBeNull();
        result.ServiceClientGrantEvidence.ServiceClientId.ShouldBe("cli-automation-client");
    }

    [Fact]
    public async Task OutOfBoundsBudgetShouldFallBackToSafeDefaultAndNeverRaiseTheCap()
    {
        // An out-of-bounds configured budget (above the governance maximum) falls back to the in-bounds SafeDefaults
        // (= Maximum) at the seam — never the raw out-of-bounds value. With a small count under SafeDefaults the
        // command is admitted; the EffectiveBudget unit assertion below proves the cap is not raised.
        new ServiceClientRateLimitState(ServiceClientRateLimitBounds.Maximum + 1, ServiceClientRateLimitWindow.RollingHour)
            .EffectiveBudget.ShouldBe(ServiceClientRateLimitBounds.Maximum);
        new ServiceClientRateLimitState(-1, ServiceClientRateLimitWindow.RollingHour)
            .EffectiveBudget.ShouldBe(ServiceClientRateLimitBounds.Maximum);

        ParticipantAuthorizationStage stage = Stage(
            rateLimitProvider: new FakeRateLimitProvider(
                new ServiceClientRateLimitState(ServiceClientRateLimitBounds.Maximum + 1, ServiceClientRateLimitWindow.RollingHour)),
            commandHistory: new FakeCommandHistory([Now.AddMinutes(-10), Now.AddMinutes(-20)]));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task StaleAdmittedCommandsOutsideTrailingWindowShouldNotCountAgainstBudget()
    {
        // AC5: the count is over the TRAILING rolling window (NotificationThrottleEvaluator.CountInTrailingWindow,
        // server-measured UTC age), not a cumulative lifetime total. Budget = 3. The history has FIVE admitted
        // commands, but only two fall inside the trailing hour (-10m, -20m); the -60m entry is exactly the window
        // edge (age == 3600s ⇒ outside) and the -90m/-120m entries have aged out. Effective in-window count = 2 < 3
        // ⇒ admitted. A cumulative-count regression (5 >= 3) would wrongly throttle this command — this test guards it.
        ParticipantAuthorizationStage stage = Stage(
            rateLimitProvider: new FakeRateLimitProvider(new ServiceClientRateLimitState(3, ServiceClientRateLimitWindow.RollingHour)),
            commandHistory: new FakeCommandHistory(
            [
                Now.AddMinutes(-10),
                Now.AddMinutes(-20),
                Now.AddMinutes(-60),
                Now.AddMinutes(-90),
                Now.AddMinutes(-120),
            ]));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeTrue();
        result.ServiceClientGrantEvidence.ShouldNotBeNull();
    }

    [Fact]
    public async Task TightBudgetBoundaryShouldAdmitBelowAndThrottleAtEffectiveCountUsingOnlyInWindowCommands()
    {
        // AC5 boundary, computed from in-window commands only: budget = 2, with two in-window admitted commands and
        // one stale (aged-out) command. The stale command must not be counted, so the in-window count is exactly 2,
        // reaching the budget ⇒ throttled (count >= budget). This pins both the "Nth command at the budget throttles"
        // boundary AND that stale commands are excluded from the throttling count, not only from the admit path.
        ParticipantAuthorizationStage stage = Stage(
            rateLimitProvider: new FakeRateLimitProvider(new ServiceClientRateLimitState(2, ServiceClientRateLimitWindow.RollingHour)),
            commandHistory: new FakeCommandHistory(
            [
                Now.AddMinutes(-5),
                Now.AddMinutes(-15),
                Now.AddMinutes(-200),
            ]));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientRateLimited);
    }

    [Fact]
    public async Task ZeroBudgetShouldDeferEveryCommandEvenWithNoRecentHistory()
    {
        // AC2 + ServiceClientRateLimitBounds: a tenant may lower the budget to its Minimum (0) to defer ALL commands.
        // Zero is in-bounds (not coerced to SafeDefaults), so the EffectiveBudget is 0; with an empty trailing window
        // the count (0) still reaches the budget (0 >= 0) ⇒ the command is throttled. This pins the most-restrictive
        // in-bounds budget — the lower boundary of the closed range — at the enforcement seam.
        new ServiceClientRateLimitState(ServiceClientRateLimitBounds.Minimum, ServiceClientRateLimitWindow.RollingHour)
            .EffectiveBudget.ShouldBe(0);

        ParticipantAuthorizationStage stage = Stage(
            rateLimitProvider: new FakeRateLimitProvider(
                new ServiceClientRateLimitState(ServiceClientRateLimitBounds.Minimum, ServiceClientRateLimitWindow.RollingHour)),
            commandHistory: new FakeCommandHistory([]));
        ChatBotCommandSubmission submission = Submission(ChatBotSurfaceOrigin.Cli);
        ChatBotAuthenticatedActor actor = Actor(
            Claim(ClaimsServiceClientGrantResolver.GrantCommandClaim, nameof(RecordGovernedNote)));

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            submission,
            actor,
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBeFalse();
        result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientRateLimited);
        result.ServiceClientGrantEvidence.ShouldBeNull();
    }

    private static ParticipantAuthorizationStage Stage(
        IServiceClientControlStateProvider? controlStateProvider = null,
        IServiceClientRateLimitProvider? rateLimitProvider = null,
        IServiceClientCommandHistory? commandHistory = null)
    {
        FixedClock clock = new(Now);
        return new ParticipantAuthorizationStage(
            serviceClientGrantValidator: new ServiceClientGrantValidator(
                new ClaimsServiceClientGrantResolver(),
                clock,
                new ChatBotSpineCommandAllowlist(),
                controlStateProvider,
                rateLimitProvider,
                commandHistory));
    }

    private sealed class FakeControlStateProvider(
        ServiceClientControlState state,
        string? onlyForServiceClientId = null) : IServiceClientControlStateProvider
    {
        public ValueTask<ServiceClientControlState> GetControlStateAsync(
            string tenantId,
            string serviceClientId,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(
                onlyForServiceClientId is null ||
                string.Equals(onlyForServiceClientId, serviceClientId, StringComparison.Ordinal)
                    ? state
                    : ServiceClientControlState.Active);
    }

    private sealed class FakeRateLimitProvider(
        ServiceClientRateLimitState state,
        string? onlyForServiceClientId = null) : IServiceClientRateLimitProvider
    {
        public ValueTask<ServiceClientRateLimitState?> GetRateLimitAsync(
            string tenantId,
            string serviceClientId,
            CancellationToken cancellationToken)
            => ValueTask.FromResult<ServiceClientRateLimitState?>(
                onlyForServiceClientId is null ||
                string.Equals(onlyForServiceClientId, serviceClientId, StringComparison.Ordinal)
                    ? state
                    : null);
    }

    private sealed class FakeCommandHistory(
        IReadOnlyList<DateTimeOffset> timestamps,
        string? onlyForServiceClientId = null) : IServiceClientCommandHistory
    {
        public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
            string tenantId,
            string serviceClientId,
            CancellationToken cancellationToken)
            => ValueTask.FromResult<IReadOnlyList<DateTimeOffset>>(
                onlyForServiceClientId is null ||
                string.Equals(onlyForServiceClientId, serviceClientId, StringComparison.Ordinal)
                    ? timestamps
                    : []);
    }

    private static ChatBotCommandSubmission Submission(
        ClaimsPrincipal principal,
        ChatBotSurfaceOrigin origin,
        string commandType = nameof(RecordGovernedNote),
        object? command = null)
        => new(
            principal,
            new Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = commandType,
                Command = command ?? new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAX"),
                RequestSchemaVersion = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            null,
            origin);

    private static ChatBotCommandSubmission Submission(
        ChatBotSurfaceOrigin origin,
        string commandType = nameof(RecordGovernedNote),
        object? command = null)
        => Submission(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "service-account-cli")], "test")),
            origin,
            commandType,
            command);

    private static ChatBotAuthenticatedActor Actor(params Claim[] overrides)
    {
        List<Claim> claims =
        [
            new("sub", "service-account-cli"),
            new(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.ServiceActorValue),
            new(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, "cli-automation-client"),
            new(ClaimsServiceClientGrantResolver.ServiceClientClassClaim, "cli-automation"),
            new(ClaimsServiceClientGrantResolver.GrantIdClaim, "01ARZ3NDEKTSV4RRFFQ69G5FAV"),
            new(ClaimsServiceClientGrantResolver.GrantTenantClaim, "tenant-alpha"),
            new(ClaimsServiceClientGrantResolver.GrantExpiryClaim, "2026-06-01T13:00:00Z"),
            new(ClaimsServiceClientGrantResolver.GrantScopeClaim, "notes.write"),
            new(ClaimsServiceClientGrantResolver.GrantSurfaceClaim, "cli"),
            new(ClaimsServiceClientGrantResolver.CommandSetVersionClaim, "command-set-v1"),
        ];

        RemoveOverriddenClaims(claims, overrides);
        claims.AddRange(overrides);
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, "test"));
        return new ChatBotAuthenticatedActor(
            "service-account-cli",
            principal,
            ParticipantAuthorizationStage.ServiceActorValue,
            "cli-automation-client");
    }

    private static ChatBotAuthenticatedActor ActorWithoutGrant()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "service-account-cli"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.ServiceActorValue),
            ],
            "test"));
        return new ChatBotAuthenticatedActor(
            "service-account-cli",
            principal,
            ParticipantAuthorizationStage.ServiceActorValue,
            "cli-automation-client");
    }

    private static Claim Claim(string type, string value) => new(type, value);

    private static void RemoveOverriddenClaims(List<Claim> claims, IReadOnlyList<Claim> overrides)
    {
        foreach (string type in overrides.Select(static claim => claim.Type).Distinct(StringComparer.Ordinal))
        {
            _ = claims.RemoveAll(claim => string.Equals(claim.Type, type, StringComparison.Ordinal));
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
