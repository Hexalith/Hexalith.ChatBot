using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Tests.TrustedAuthority;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    /// <summary>Enumerates every allowlisted HTTP command whose dispatcher uses the caller's command id.</summary>
    public static IEnumerable<object[]> FallbackHttpCommands()
        => new ChatBotAuthorityCatalog().Requirements
            .Where(static row => !row.IsQuery && !ChatBotCanonicalDispatchTarget.IsSupported(row.Operation) && new ChatBotSpineCommandAllowlist().IsAllowed(row.Operation))
            .Select(static row => new object[] { row.Operation });

    /// <summary>Payload authority cannot authorize an unrelated caller-selected fallback aggregate.</summary>
    [Theory]
    [MemberData(nameof(FallbackHttpCommands))]
    public async Task FallbackHttpDispatchRequiresCurrentExactTargetOwnerEvidence(string operation)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = static request => request.ResourceId != "forbidden-target" };
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        InMemoryCoarseIdempotencyStore idempotency = new(clock);
        InMemoryAuthorizationFailureCounter counter = new(clock);
        ChatBotRequestAuthorizer authorizer = TrustedAuthorityFixture.Authorizer(clock, owner);
        ChatBotAuthorityRequirement row = new ChatBotAuthorityCatalog().Find(operation, false)!;
        Dictionary<string, object?> payload = new() { [row.ResourceProperty ?? "ResourceId"] = "payload-resource", ["ProjectId"] = "project-alpha" };
        CommandGateway gateway = Gateway(dispatcher, clock: clock, auditWriter: audit, idempotencyStore: idempotency,
            authorizationFailureCounter: counter, commandAllowlist: new ChatBotSpineCommandAllowlist(), requestAuthorizer: authorizer);
        ChatBotGatewayResult result = await gateway.SubmitAsync(Submission(TrustedAuthorityFixture.Principal(), payload,
            commandType: operation, commandId: "forbidden-target"), TestContext.Current.CancellationToken);
        result.IsAccepted.ShouldBeFalse();
        dispatcher.DispatchCount.ShouldBe(0);
        idempotency.Records.ShouldBeEmpty();
        audit.Envelopes.ShouldBeEmpty();
        audit.AuthorizationFailures.ShouldHaveSingleItem();
        counter.ReadAndReset().ShouldHaveSingleItem().FailureCount.ShouldBe(1);
        owner.Requests.ShouldContain(request => request.Owner == "ChatBot" && request.ResourceId == "forbidden-target" &&
            request.Operation == operation && request.Authority == "operation" && request.RequireCurrent);
    }

    /// <summary>Enumerates every fallback command with a caller CommandId distinct from, and equal to, the payload resource.</summary>
    public static IEnumerable<object[]> FallbackTargetDecisionCases()
        => FallbackHttpCommands().SelectMany(static row => new[] { new object[] { row[0], false }, new object[] { row[0], true } });

    /// <summary>
    /// The authorizer itself issues and honors exact current ChatBot <c>operation</c> evidence for the dispatched
    /// fallback stream, including an admin-scoped row whose caller CommandId equals the payload resource. Only the
    /// fallback evidence differs between the allowed control and the denial.
    /// </summary>
    [Theory]
    [MemberData(nameof(FallbackTargetDecisionCases))]
    public async Task FallbackTargetDecisionRequiresExactOperationEvidenceForTheDispatchedStream(string operation, bool commandIdEqualsPayloadResource)
    {
        ChatBotAuthorityRequirement row = new ChatBotAuthorityCatalog().Find(operation, false)!;
        row.AdminScope.ShouldNotBeNull();
        row.ResourceProperty.ShouldNotBeNull();
        string target = commandIdEqualsPayloadResource ? "payload-resource" : "fallback-target";
        Dictionary<string, object?> payload = new() { [row.ResourceProperty] = "payload-resource", ["ProjectId"] = "project-alpha" };

        (ChatBotAuthorityDecision allowed, SyntheticOwnerAuthorityProvider allowedOwner) = await DecideFallbackTargetAsync(operation, payload, target, denyFallbackEvidence: false);
        (ChatBotAuthorityDecision denied, SyntheticOwnerAuthorityProvider deniedOwner) = await DecideFallbackTargetAsync(operation, payload, target, denyFallbackEvidence: true);

        allowed.IsAllowed.ShouldBeTrue();
        allowed.ReasonCode.ShouldBeEmpty();
        denied.IsAllowed.ShouldBeFalse();
        denied.EvidenceReferences.ShouldBeEmpty();
        denied.ReasonCode.ShouldBe(operation switch
        {
            nameof(AssignTenantAdminRole) => ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized,
            nameof(SubmitEscalationPolicyChange) => ChatBotAuthorizationReasonCodes.EscalationPolicyUnauthorized,
            _ => ChatBotAuthorizationReasonCodes.AuthorizationDenied,
        });
        allowedOwner.Requests.Where(request => IsFallbackTargetRequest(request, operation, target)).ShouldHaveSingleItem().RequireCurrent.ShouldBeTrue();
        deniedOwner.Requests.Where(request => IsFallbackTargetRequest(request, operation, target)).ShouldHaveSingleItem().RequireCurrent.ShouldBeTrue();
    }

    /// <summary>A direct stage invocation without a pipeline-installed authority principal also authorizes the fallback stream.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectStageInvocationAuthorizesTheFallbackCommandIdStream(bool denyFallbackEvidence)
    {
        const string operation = nameof(SubmitMailboxSourceRateLimit);
        ChatBotCanonicalDispatchTarget.IsSupported(operation).ShouldBeFalse();
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Allows = request => !(denyFallbackEvidence && IsFallbackTargetRequest(request, operation, "forbidden-target")),
        };
        ParticipantAuthorizationStage stage = new(clock: clock, requestAuthorizer: TrustedAuthorityFixture.Authorizer(clock, owner));
        SubmitMailboxSourceRateLimit command = new("mailbox-rate-limit-001", "mailbox-source:controlled-mailbox-001", "mailbox-source-noisy-intake",
            "policy-snapshot:mailbox:v1", OldBudget: 0, NewBudget: 200, Hexalith.ChatBot.Contracts.Enums.MailboxRateLimitWindow.RollingHour, 4,
            "admin-requester", MailboxSourceRateLimitSchemaVersions.V1, "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(
            Submission(TrustedAuthorityFixture.Principal(), command, commandType: operation, commandId: "forbidden-target"),
            new ChatBotAuthenticatedActor("actor-alpha", TrustedAuthorityFixture.Principal()),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);

        result.IsAllowed.ShouldBe(!denyFallbackEvidence);
        if (denyFallbackEvidence)
        {
            result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        owner.Requests.Where(request => IsFallbackTargetRequest(request, operation, "forbidden-target")).ShouldHaveSingleItem().RequireCurrent.ShouldBeTrue();
    }

    private static async Task<(ChatBotAuthorityDecision Decision, SyntheticOwnerAuthorityProvider Owner)> DecideFallbackTargetAsync(
        string operation, Dictionary<string, object?> payload, string target, bool denyFallbackEvidence)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Allows = request => !(denyFallbackEvidence && IsFallbackTargetRequest(request, operation, target)),
        };
        ChatBotAuthorityDecision decision = await TrustedAuthorityFixture.Authorizer(clock, owner).AuthorizeAsync(
            TrustedAuthorityFixture.Context(), operation, false, payload, TestContext.Current.CancellationToken, fallbackAggregateId: target).ConfigureAwait(false);
        return (decision, owner);
    }

    private static bool IsFallbackTargetRequest(ChatBotOwnerAuthorityRequest request, string operation, string target)
        => request.Owner == "ChatBot" && request.ResourceId == target && request.Operation == operation && request.Authority == "operation";

    /// <summary>Provider awaits cannot turn lapsed authority or a selected expired grant into admitted history.</summary>
    [Theory]
    [InlineData("service", "machine", false)]
    [InlineData("ai", "machine", false)]
    [InlineData("service", "machine", true)]
    [InlineData("ai", "machine", true)]
    [InlineData("human", "capability", false)]
    [InlineData("service", "capability", false)]
    [InlineData("ai", "capability", false)]
    [InlineData("service", "capability", true)]
    [InlineData("ai", "capability", true)]
    public async Task AuthorityLapseBeforeAdmissionHistoryDeniesWithoutAnyHistoryWrite(string actorClass, string historyKind, bool selectedGrantExpires)
    {
        TrustedAuthorityClock clock = new();
        int grantCalls = 0;
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = evidence => selectedGrantExpires
                ? evidence.ServiceGrant is { } grant && ++grantCalls == 2
                    ? evidence with { ServiceGrant = grant with { ExpiresAt = clock.UtcNow.AddSeconds(1) } } : evidence
                : evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) },
        };
        InMemoryGovernedControlStateProjectionStore real = new();
        string subjectClass = historyKind == "capability" ? GovernedControlSubjectClasses.CommandCapability
            : actorClass == "ai" ? GovernedControlSubjectClasses.AiActor : GovernedControlSubjectClasses.ServiceClient;
        string subject = historyKind == "capability" ? nameof(RecordGovernedNote) : "client-alpha";
        GovernedControlStateView original = new("tenant-alpha", subjectClass, subject, GovernedControlStateView.Active, 10,
            GovernedControlStateView.RollingHour, 1, "correlation-alpha", clock.UtcNow, clock.UtcNow, false, []);
        await real.SaveAsync(original, TestContext.Current.CancellationToken);
        IGovernedControlStateProjectionStore store = TrustedAuthorityBoundaryProxy.Create<IGovernedControlStateProjectionStore>(real, static _ => { });
        void AdvanceAfterHistory(string method) { if (method == "GetRecentAdmittedAsync") { clock.UtcNow += TimeSpan.FromSeconds(2); } }
        IServiceClientCommandHistory serviceHistory = TrustedAuthorityBoundaryProxy.Create<IServiceClientCommandHistory>(
            new ProjectionBackedServiceClientCommandHistory(store, clock), historyKind == "machine" ? AdvanceAfterHistory : static _ => { });
        IAiActorProposalHistory aiHistory = TrustedAuthorityBoundaryProxy.Create<IAiActorProposalHistory>(
            new ProjectionBackedAiActorProposalHistory(store, clock), historyKind == "machine" ? AdvanceAfterHistory : static _ => { });
        ICommandCapabilityCommandHistory capabilityHistory = TrustedAuthorityBoundaryProxy.Create<ICommandCapabilityCommandHistory>(
            new ProjectionBackedCommandCapabilityCommandHistory(store, clock), historyKind == "capability" ? AdvanceAfterHistory : static _ => { });
        ChatBotRequestAuthorizer authorizer = TrustedAuthorityFixture.Authorizer(clock, owner);
        ServiceClientGrantValidator validator = new(new ClaimsServiceClientGrantResolver(authorizer), clock, new ChatBotSpineCommandAllowlist(),
            rateLimitProvider: new ProjectionBackedServiceClientRateLimitProvider(store, clock), commandHistory: serviceHistory,
            aiActorRateLimitProvider: new ProjectionBackedAiActorRateLimitProvider(store, clock), aiActorProposalHistory: aiHistory);
        ParticipantAuthorizationStage stage = new(serviceClientGrantValidator: validator, clock: clock,
            rateLimitProvider: new ProjectionBackedCommandCapabilityRateLimitProvider(store, clock), commandHistory: capabilityHistory, requestAuthorizer: authorizer);
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        InMemoryCoarseIdempotencyStore idempotency = new(clock);
        InMemoryAuthorizationFailureCounter counter = new(clock);
        CommandGateway gateway = Gateway(dispatcher, authorizationStage: stage, clock: clock, auditWriter: audit,
            idempotencyStore: idempotency, authorizationFailureCounter: counter, requestAuthorizer: authorizer);
        ChatBotGatewayResult result = await gateway.SubmitAsync(Submission(TrustedAuthorityFixture.Principal(actorClass: actorClass),
            new RecordGovernedNote("note-alpha")), TestContext.Current.CancellationToken);
        result.IsAccepted.ShouldBeFalse();
        string reason = selectedGrantExpires ? ChatBotAuthorizationReasonCodes.ServiceClientGrantExpired : ChatBotAuthorizationReasonCodes.AuthorizationDenied;
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(reason);
        counter.ReadAndReset().ShouldHaveSingleItem().FailureCount.ShouldBe(1);
        dispatcher.DispatchCount.ShouldBe(0);
        idempotency.Records.ShouldBeEmpty();
        audit.Envelopes.ShouldBeEmpty();
        ((TrustedAuthorityBoundaryProxy)(object)store).Calls.ShouldNotContain("SaveAsync");
        ((TrustedAuthorityBoundaryProxy)(object)serviceHistory).Calls.ShouldNotContain("RecordAdmittedAsync");
        ((TrustedAuthorityBoundaryProxy)(object)aiHistory).Calls.ShouldNotContain("RecordAdmittedAsync");
        ((TrustedAuthorityBoundaryProxy)(object)capabilityHistory).Calls.ShouldNotContain("RecordAdmittedAsync");
        (await real.GetAsync("tenant-alpha", subjectClass, subject, TestContext.Current.CancellationToken)).ShouldBe(original);
    }
}
