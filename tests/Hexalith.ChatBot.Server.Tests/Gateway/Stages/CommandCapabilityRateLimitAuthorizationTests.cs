using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using CommandCapabilityControlState = Hexalith.ChatBot.Contracts.Enums.CommandCapabilityControlState;
using CommandCapabilityRateLimitWindow = Hexalith.ChatBot.Contracts.Enums.CommandCapabilityRateLimitWindow;
using SubmitCommandCapabilityRateLimit = Hexalith.ChatBot.Contracts.Commands.SubmitCommandCapabilityRateLimit;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class CommandCapabilityRateLimitAuthorizationTests
{
    private const string Tenant = "tenant-alpha";

    // The rate-limited command-capability subject is a sibling first-party command type, NOT an FR74 governance command.
    private const string RateLimitedCapability = nameof(Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject);
    private const string SiblingCapability = nameof(Hexalith.ChatBot.Contracts.Commands.MarkEmailAssociationNeedsReview);

    private static readonly DateTimeOffset FixedNow = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    // ----- Authorization of the SubmitCommandCapabilityRateLimit command itself (single human policy-admin) -----

    [Fact]
    public async Task RateLimitShouldRequireSingleHumanPolicyAdminWithNoApprover()
    {
        ParticipantAuthorizationStage stage = new();

        // Command-capability governance is the policy-admin's domain (the "security engineer" persona maps to
        // AdminScope.Policy). A single authorized human policy-admin applies it — no approver needed. A tenant-admin is
        // also allowed via the FR75a scope union (not a relaxation).
        foreach (ChatBotAuthenticatedActor allowedActor in new[]
                 {
                     Actor("human", "policy-admin"),
                     Actor("human", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(RateLimitSubmit()),
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
                Submission(RateLimitSubmit()),
                deniedActor,
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task RateLimitShouldRejectOutOfBoundsOrUndeclaredBudgetAtGateway()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitCommandCapabilityRateLimit invalid in new[]
                 {
                     RateLimitSubmit() with { NewBudget = CommandCapabilityRateLimitBounds.Maximum + 1 },
                     RateLimitSubmit() with { NewBudget = -1 },
                     RateLimitSubmit() with { OldBudget = -1 },
                     RateLimitSubmit() with { SourceVersion = -1 },
                     RateLimitSubmit() with { SchemaVersion = "command-capability-rate-limit-schema.custom" },
                     RateLimitSubmit() with { ReasonCode = "unsafe reason" },
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

        // Boundary budgets (exactly the maximum, and the minimum of zero) are accepted.
        foreach (SubmitCommandCapabilityRateLimit valid in new[]
                 {
                     RateLimitSubmit() with { NewBudget = CommandCapabilityRateLimitBounds.Maximum },
                     RateLimitSubmit() with { NewBudget = CommandCapabilityRateLimitBounds.Minimum },
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(valid),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task SelfLockoutGuardShouldRejectRateLimitingAnFr74GovernanceCommand()
    {
        ParticipantAuthorizationStage stage = new();

        // An admin cannot rate-limit the very commands needed to govern/reverse a control — including the rate-limit
        // command itself — which would otherwise risk locking the tenant out of governance.
        foreach (string governanceRef in new[]
                 {
                     nameof(SubmitCommandCapabilityRateLimit),
                     nameof(Hexalith.ChatBot.Contracts.Commands.SubmitCommandCapabilityDisable),
                     nameof(Hexalith.ChatBot.Contracts.Commands.SubmitCommandCapabilityQuarantine),
                     nameof(Hexalith.ChatBot.Contracts.Commands.ApproveCommandCapabilityQuarantine),
                     nameof(Hexalith.ChatBot.Contracts.Commands.SubmitAiActorDisable),
                     nameof(Hexalith.ChatBot.Contracts.Commands.SubmitMailboxSourceQuarantine),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(RateLimitSubmit() with { CommandCapabilityRef = governanceRef }),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    // ----- Actor-agnostic final-gate enforcement of a configured budget -----

    [Fact]
    public async Task RateLimitedCapabilityAtBudgetShouldFailClosedForEveryActorAsFinalGate()
    {
        // The budget is reached (count == budget) for the command TYPE. Every actor type — human, service, AND AI —
        // submitting that type is denied with the distinct command_capability_rate_limited reason. The gate is the
        // LAST check: a passing grant validator and an Active control-state provider must already have run.
        FakeCommandCapabilityRateLimitProvider rateLimits = new();
        rateLimits.Configure(Tenant, RateLimitedCapability, budget: 3);
        FakeCommandCapabilityCommandHistory history = new();
        history.Seed(Tenant, RateLimitedCapability, FixedNow.AddMinutes(-1), FixedNow.AddMinutes(-2), FixedNow.AddMinutes(-3));
        ParticipantAuthorizationStage stage = new(
            clock: new FixedClock(FixedNow),
            rateLimitProvider: rateLimits,
            commandHistory: history);

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "tenant-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(new object(), RateLimitedCapability),
                actor,
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandCapabilityRateLimited);

            // The rate-limit reason is DISTINCT from the command-capability control states, the global static spine
            // allowlist, the per-grant allowlist, and every per-actor rate-limit reason.
            denied.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.CommandCapabilityDisabled);
            denied.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.CommandCapabilityQuarantined);
            denied.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.CommandNotAllowlisted);
            denied.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantUnderScoped);
            denied.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.AiActorRateLimited);
            denied.ReasonCode.ShouldNotBe(ChatBotAuthorizationReasonCodes.ServiceClientRateLimited);
        }

        // The seams only ever receive the safe tenant id + command type name — never any credential/grant/PII.
        rateLimits.ObservedRequests.ShouldAllBe(request =>
            string.Equals(request.TenantId, Tenant, StringComparison.Ordinal) &&
            string.Equals(request.CommandCapabilityRef, RateLimitedCapability, StringComparison.Ordinal));
        history.ObservedRequests.ShouldAllBe(request =>
            string.Equals(request.TenantId, Tenant, StringComparison.Ordinal) &&
            string.Equals(request.CommandCapabilityRef, RateLimitedCapability, StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnderBudgetSubmissionShouldBeAdmitted()
    {
        FakeCommandCapabilityRateLimitProvider rateLimits = new();
        rateLimits.Configure(Tenant, RateLimitedCapability, budget: 3);
        FakeCommandCapabilityCommandHistory history = new();
        history.Seed(Tenant, RateLimitedCapability, FixedNow.AddMinutes(-1), FixedNow.AddMinutes(-2));
        ParticipantAuthorizationStage stage = new(
            clock: new FixedClock(FixedNow),
            rateLimitProvider: rateLimits,
            commandHistory: history);

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(new object(), RateLimitedCapability),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task RateLimitShouldIsolateSiblingCommandTypesAndOtherTenants()
    {
        FakeCommandCapabilityRateLimitProvider rateLimits = new();
        rateLimits.Configure(Tenant, RateLimitedCapability, budget: 1);
        FakeCommandCapabilityCommandHistory history = new();
        history.Seed(Tenant, RateLimitedCapability, FixedNow.AddMinutes(-1));
        ParticipantAuthorizationStage stage = new(
            clock: new FixedClock(FixedNow),
            rateLimitProvider: rateLimits,
            commandHistory: history);

        // The rate-limited type for this tenant is at budget → denied.
        ChatBotAuthorizationResult throttled = await stage.AuthorizeAsync(
            Submission(new object(), RateLimitedCapability),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        throttled.IsAllowed.ShouldBeFalse();
        throttled.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandCapabilityRateLimited);

        // A sibling un-throttled command type for the same tenant is unaffected (no configured budget).
        ChatBotAuthorizationResult sibling = await stage.AuthorizeAsync(
            Submission(new object(), SiblingCapability),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        sibling.IsAllowed.ShouldBeTrue();

        // The SAME command type under a DIFFERENT tenant is unaffected (per-tenant isolation, NFR30).
        ChatBotAuthorizationResult otherTenant = await stage.AuthorizeAsync(
            Submission(new object(), RateLimitedCapability),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-beta"),
            TestContext.Current.CancellationToken);
        otherTenant.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task RateLimitShouldExemptFr74GovernanceCommands()
    {
        // Even if a fake reports an FR74 governance command as rate-limited at budget, the final gate exempts it so a
        // rate-limited tenant can still govern/reverse controls. A valid SubmitCommandCapabilityRateLimit from a
        // policy-admin therefore stays admittable.
        FakeCommandCapabilityRateLimitProvider rateLimits = new();
        rateLimits.Configure(Tenant, nameof(SubmitCommandCapabilityRateLimit), budget: 0);
        FakeCommandCapabilityCommandHistory history = new();
        history.Seed(Tenant, nameof(SubmitCommandCapabilityRateLimit), FixedNow.AddMinutes(-1));
        ParticipantAuthorizationStage stage = new(
            clock: new FixedClock(FixedNow),
            rateLimitProvider: rateLimits,
            commandHistory: history);

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(RateLimitSubmit()),
            Actor("human", "policy-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();

        // The exemption means the rate-limit provider is never even consulted for a governance command type.
        rateLimits.ObservedRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task DisabledOrQuarantinedCapabilityShouldKeepItsControlReasonOverTheRateLimitGate()
    {
        // The top-of-stage Disabled/Quarantined control-state switch and the bottom rate-limit gate coexist. A
        // controlled capability returns its precise control reason — rate-limit never masks a security denial — even
        // when a rate-limit budget is also configured for the same type.
        FakeCommandCapabilityRateLimitProvider rateLimits = new();
        rateLimits.Configure(Tenant, RateLimitedCapability, budget: 0);
        FakeCommandCapabilityCommandHistory history = new();
        history.Seed(Tenant, RateLimitedCapability, FixedNow.AddMinutes(-1));

        CommandCapabilityQuarantineAuthorizationTestsControlProvider disabled = new();
        disabled.Disable(Tenant, RateLimitedCapability);
        ParticipantAuthorizationStage disabledStage = new(
            commandCapabilityControlStateProvider: disabled,
            clock: new FixedClock(FixedNow),
            rateLimitProvider: rateLimits,
            commandHistory: history);

        ChatBotAuthorizationResult disabledResult = await disabledStage.AuthorizeAsync(
            Submission(new object(), RateLimitedCapability),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        disabledResult.IsAllowed.ShouldBeFalse();
        disabledResult.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandCapabilityDisabled);

        CommandCapabilityQuarantineAuthorizationTestsControlProvider quarantined = new();
        quarantined.Quarantine(Tenant, RateLimitedCapability);
        ParticipantAuthorizationStage quarantinedStage = new(
            commandCapabilityControlStateProvider: quarantined,
            clock: new FixedClock(FixedNow),
            rateLimitProvider: rateLimits,
            commandHistory: history);

        ChatBotAuthorizationResult quarantinedResult = await quarantinedStage.AuthorizeAsync(
            Submission(new object(), RateLimitedCapability),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        quarantinedResult.IsAllowed.ShouldBeFalse();
        quarantinedResult.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandCapabilityQuarantined);
    }

    [Fact]
    public async Task RateLimitShouldCountOnlyAdmittedCommandsInsideTheTrailingWindow()
    {
        // AC5: the count is the server-measured UTC age against the injected clock over the rolling-hour window —
        // admitted commands that have aged OUT of the trailing window (and any future-dated timestamps) must NOT count
        // against the budget, so a command type that was noisy an hour ago but has since gone quiet is no longer
        // throttled. budget = 3, and although SIX timestamps are seeded, only TWO fall strictly inside the trailing
        // hour, so the submission is admitted (2 < 3). A naive total count (6) would wrongly deny — this proves the
        // WindowDuration + NotificationThrottleEvaluator.CountInTrailingWindow wiring is actually exercised (a wrong
        // window duration would otherwise slip past every other test, which only seeds within-minutes timestamps).
        FakeCommandCapabilityRateLimitProvider rateLimits = new();
        rateLimits.Configure(Tenant, RateLimitedCapability, budget: 3);
        FakeCommandCapabilityCommandHistory history = new();
        history.Seed(
            Tenant,
            RateLimitedCapability,
            FixedNow.AddMinutes(-10),   // inside the window
            FixedNow.AddMinutes(-59),   // inside the window (just under the hour)
            FixedNow.AddHours(-1),      // exactly one hour old → aged out (age == window is OUTSIDE)
            FixedNow.AddMinutes(-61),   // aged out
            FixedNow.AddHours(-3),      // aged out
            FixedNow.AddMinutes(30));   // future → ignored (negative age)
        ParticipantAuthorizationStage stage = new(
            clock: new FixedClock(FixedNow),
            rateLimitProvider: rateLimits,
            commandHistory: history);

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(new object(), RateLimitedCapability),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();

        // Contrast: add a THIRD in-window admitted command so the in-window count reaches the budget (3 >= 3) → the
        // submission is throttled. Keeping the same aged-out/future noise locks the boundary, so the admit above can
        // never be a trivial "history is ignored entirely" pass.
        history.Seed(
            Tenant,
            RateLimitedCapability,
            FixedNow.AddMinutes(-5),    // inside the window
            FixedNow.AddMinutes(-10),   // inside the window
            FixedNow.AddMinutes(-59),   // inside the window
            FixedNow.AddHours(-2),      // aged out
            FixedNow.AddMinutes(30));   // future → ignored
        ChatBotAuthorizationResult throttled = await stage.AuthorizeAsync(
            Submission(new object(), RateLimitedCapability),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding(Tenant),
            TestContext.Current.CancellationToken);
        throttled.IsAllowed.ShouldBeFalse();
        throttled.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.CommandCapabilityRateLimited);
    }

    [Fact]
    public void CapacityImpactObservationShouldCarryFiniteIntegerBudgetCountAndThrottledFlag()
    {
        // AC6: the capacity-impact surface is carried as safe, finite integer tokens — the effective budget, the
        // observed trailing-window admitted-command count, and whether this submission was throttled — never floats.
        // This pins the observation record's shape (the deferred Epic-8 dashboard seam) so it stays integer-only.
        CommandCapabilityRateLimitObservation throttledObservation = new(Budget: 3, ObservedWindowCount: 3, Throttled: true);
        throttledObservation.Budget.ShouldBe(3);
        throttledObservation.ObservedWindowCount.ShouldBe(3);
        throttledObservation.Throttled.ShouldBeTrue();

        CommandCapabilityRateLimitObservation admittedObservation = new(Budget: 10, ObservedWindowCount: 4, Throttled: false);
        admittedObservation.Throttled.ShouldBeFalse();
        admittedObservation.ObservedWindowCount.ShouldBeLessThan(admittedObservation.Budget);

        // Finite integer tokens (never floats) — the record exposes only int/bool members.
        typeof(CommandCapabilityRateLimitObservation)
            .GetProperties()
            .Select(property => property.PropertyType)
            .ShouldBe([typeof(int), typeof(int), typeof(bool)]);
    }

    [Fact]
    public void OutOfBoundsConfiguredBudgetShouldFallBackToSafeDefaultNeverRaisingTheCap()
    {
        // An out-of-bounds configured budget (above the declared maximum) falls back to the safe default (= maximum)
        // at the enforcement seam — it can never silently raise the cap above the declared maximum.
        new CommandCapabilityRateLimitState(CommandCapabilityRateLimitBounds.Maximum + 5_000, CommandCapabilityRateLimitWindow.RollingHour)
            .EffectiveBudget.ShouldBe(CommandCapabilityRateLimitBounds.Maximum);
        new CommandCapabilityRateLimitState(-10, CommandCapabilityRateLimitWindow.RollingHour)
            .EffectiveBudget.ShouldBe(CommandCapabilityRateLimitBounds.Maximum);

        // An in-bounds budget is used as-is.
        new CommandCapabilityRateLimitState(42, CommandCapabilityRateLimitWindow.RollingHour)
            .EffectiveBudget.ShouldBe(42);
    }

    private static SubmitCommandCapabilityRateLimit RateLimitSubmit()
        => new(
            "command-capability-rate-limit-001",
            RateLimitedCapability,
            "command-capability-noisy-submissions",
            "policy-snapshot:policy-admin:v1",
            OldBudget: 0,
            NewBudget: 500,
            CommandCapabilityRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            CommandCapabilityRateLimitSchemaVersions.V1,
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

    private sealed class FakeCommandCapabilityRateLimitProvider : ICommandCapabilityRateLimitProvider
    {
        private readonly Dictionary<string, CommandCapabilityRateLimitState> _budgets = new(StringComparer.Ordinal);

        public List<(string TenantId, string CommandCapabilityRef)> ObservedRequests { get; } = [];

        public void Configure(string tenantId, string commandCapabilityRef, int budget)
            => _budgets[$"{tenantId}|{commandCapabilityRef}"] =
                new CommandCapabilityRateLimitState(budget, CommandCapabilityRateLimitWindow.RollingHour);

        public ValueTask<CommandCapabilityRateLimitState?> GetRateLimitAsync(
            string tenantId,
            string commandCapabilityRef,
            CancellationToken cancellationToken)
        {
            ObservedRequests.Add((tenantId, commandCapabilityRef));
            return ValueTask.FromResult(_budgets.TryGetValue($"{tenantId}|{commandCapabilityRef}", out CommandCapabilityRateLimitState? state)
                ? state
                : null);
        }
    }

    private sealed class FakeCommandCapabilityCommandHistory : ICommandCapabilityCommandHistory
    {
        private readonly Dictionary<string, IReadOnlyList<DateTimeOffset>> _history = new(StringComparer.Ordinal);

        public List<(string TenantId, string CommandCapabilityRef)> ObservedRequests { get; } = [];

        public void Seed(string tenantId, string commandCapabilityRef, params DateTimeOffset[] timestamps)
            => _history[$"{tenantId}|{commandCapabilityRef}"] = timestamps;

        public ValueTask<IReadOnlyList<DateTimeOffset>> GetRecentAdmittedAsync(
            string tenantId,
            string commandCapabilityRef,
            CancellationToken cancellationToken)
        {
            ObservedRequests.Add((tenantId, commandCapabilityRef));
            return ValueTask.FromResult(_history.TryGetValue($"{tenantId}|{commandCapabilityRef}", out IReadOnlyList<DateTimeOffset>? timestamps)
                ? timestamps
                : []);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class CommandCapabilityQuarantineAuthorizationTestsControlProvider : ICommandCapabilityControlStateProvider
    {
        private readonly Dictionary<string, CommandCapabilityControlState> _controlled = new(StringComparer.Ordinal);

        public void Disable(string tenantId, string commandCapabilityRef)
            => _controlled[$"{tenantId}|{commandCapabilityRef}"] = CommandCapabilityControlState.Disabled;

        public void Quarantine(string tenantId, string commandCapabilityRef)
            => _controlled[$"{tenantId}|{commandCapabilityRef}"] = CommandCapabilityControlState.Quarantined;

        public ValueTask<CommandCapabilityControlState> GetControlStateAsync(
            string tenantId,
            string commandCapabilityRef,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(_controlled.TryGetValue($"{tenantId}|{commandCapabilityRef}", out CommandCapabilityControlState state)
                ? state
                : CommandCapabilityControlState.Active);
    }
}
