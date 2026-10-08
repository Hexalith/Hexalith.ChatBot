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
