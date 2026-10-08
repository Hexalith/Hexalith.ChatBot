using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Tests.TrustedAuthority;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorityExpiryDuringPreparationReleasesOnlyUndispatchedOwnershipAndAllowsFreshRetry(bool durable)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) } };
        FakeCoarseIdempotencyStateClient state = new();
        IIdempotencyStore real = durable ? new DaprCoarseIdempotencyStore(state, clock) : new InMemoryCoarseIdempotencyStore(clock);
        AuthorityExpiryPreparationStore injected = new(real, () => clock.UtcNow += TimeSpan.FromSeconds(2));
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        InMemoryAuthorizationFailureCounter counter = new(clock);
        CommandGateway gateway = Gateway(dispatcher, clock: clock, idempotencyStore: injected, auditWriter: audit, authorizationFailureCounter: counter,
            requestAuthorizer: TrustedAuthorityFixture.Authorizer(clock, owner));
        ChatBotCommandSubmission request = Submission(TrustedAuthorityFixture.Principal(), new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"));
        ChatBotGatewayResult result = await gateway.SubmitAsync(request, TestContext.Current.CancellationToken);
        result.IsAccepted.ShouldBeFalse();
        result.Problem!.Code.ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        dispatcher.DispatchCount.ShouldBe(0);
        injected.Prepared.ShouldBeTrue();
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        counter.ReadAndReset().ShouldHaveSingleItem().FailureCount.ShouldBe(1);
        if (durable)
        {
            state.DomainRecords.ShouldBeEmpty();
            state.IdentityRecords.ShouldBeEmpty();
        }
        else { ((InMemoryCoarseIdempotencyStore)real).Records.ShouldBeEmpty(); }

        injected.AdvanceDuringPreparation = false;
        ChatBotGatewayResult fresh = await gateway.SubmitAsync(request, TestContext.Current.CancellationToken);
        fresh.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        if (durable)
        {
            state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
            state.IdentityRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
        }
        else { ((InMemoryCoarseIdempotencyStore)real).Records.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull(); }
    }

    /// <summary>A pre-effect lapse releases ownership, audits denial and permits a fresh retry.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorityLapseDuringDispatchBindingIsAuditedDeniedAndFreshRetryCanDispatch(bool durable)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) } };
        FakeCoarseIdempotencyStateClient state = new();
        IIdempotencyStore real = durable ? new DaprCoarseIdempotencyStore(state, clock) : new InMemoryCoarseIdempotencyStore(clock);
        bool advance = true;
        TrustedAuthorityAdvancingIdempotencyStore store = new(real, step => { if (advance && step == "binding") { clock.UtcNow += TimeSpan.FromSeconds(2); } });
        DurableRecoveryEventStoreClient sdk = new();
        RecordingAuditWriter audit = new();
        RecordingReplayIntentQueue replay = new();
        RecordingOperatorAlertSink alerts = new();
        InMemoryAuthorizationFailureCounter counter = new(clock);
        CommandGateway gateway = Gateway(new AcceptedCommandDispatcher(sdk, null!, null!, clock), clock: clock, idempotencyStore: store,
            auditWriter: audit, replayQueue: replay, alertSink: alerts, authorizationFailureCounter: counter, requestAuthorizer: TrustedAuthorityFixture.Authorizer(clock, owner));
        ChatBotCommandSubmission request = Submission(TrustedAuthorityFixture.Principal(), new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"));
        ChatBotGatewayResult result = await gateway.SubmitAsync(request, TestContext.Current.CancellationToken);
        result.IsAccepted.ShouldBeFalse();
        result.Problem!.Code.ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        store.Boundaries.ShouldContain("binding");
        sdk.SubmissionCount.ShouldBe(0);
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        counter.ReadAndReset().ShouldHaveSingleItem().FailureCount.ShouldBe(1);
        replay.Intents.ShouldBeEmpty();
        alerts.Alerts.ShouldBeEmpty();
        if (durable) { state.DomainRecords.ShouldBeEmpty(); state.IdentityRecords.ShouldBeEmpty(); }
        else { ((InMemoryCoarseIdempotencyStore)real).Records.ShouldBeEmpty(); }
        advance = false;
        (await gateway.SubmitAsync(request, TestContext.Current.CancellationToken)).IsAccepted.ShouldBeTrue();
        sdk.SubmissionCount.ShouldBe(1);
        if (durable) { state.IdentityRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull(); }
        else { ((InMemoryCoarseIdempotencyStore)real).Records.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull(); }
    }

    /// <summary>Replay expiry is audited and cannot change the prior ownership, outcome, or operation status.</summary>
    [Theory]
    [InlineData(false, "admission")]
    [InlineData(true, "admission")]
    [InlineData(false, "status")]
    [InlineData(true, "status")]
    public async Task ReplayRetainsAuthorityAcrossAdmissionAndStatusLookup(bool durable, string boundary)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) } };
        FakeCoarseIdempotencyStateClient state = new();
        IIdempotencyStore real = durable ? new DaprCoarseIdempotencyStore(state, clock) : new InMemoryCoarseIdempotencyStore(clock);
        bool inject = false;
        TrustedAuthorityAdvancingIdempotencyStore idempotency = new(real, step => { if (inject && boundary == "admission" && step == "admission") { clock.UtcNow += TimeSpan.FromSeconds(2); } });
        InMemoryOperationStatusStore realStatus = new();
        IOperationStatusStore status = TrustedAuthorityBoundaryProxy.Create<IOperationStatusStore>(realStatus, method =>
        {
            if (inject && boundary == "status" && method == "TryGetAsync") { clock.UtcNow += TimeSpan.FromSeconds(2); }
        });
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        InMemoryAuthorizationFailureCounter counter = new(clock);
        CommandGateway gateway = Gateway(dispatcher, clock: clock, idempotencyStore: idempotency, operationStatusStore: status,
            auditWriter: audit, authorizationFailureCounter: counter, requestAuthorizer: TrustedAuthorityFixture.Authorizer(clock, owner));
        ChatBotCommandSubmission submission = Submission(TrustedAuthorityFixture.Principal(), new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"));
        ChatBotGatewayResult first = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        first.IsAccepted.ShouldBeTrue();
        OperationStatusRecord beforeStatus = (await realStatus.TryGetAsync("tenant-alpha", OperationStatusRecord.OperationIdFor(first.Accepted!), TestContext.Current.CancellationToken))!;
        CoarseIdempotencyRecord before = durable ? state.DomainRecords.Single() : ((InMemoryCoarseIdempotencyStore)real).Records.Single();
        ((TrustedAuthorityBoundaryProxy)(object)status).Calls.Clear();
        inject = true;
        ChatBotGatewayResult replay = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        replay.IsAccepted.ShouldBeFalse();
        replay.Problem!.Code.ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        dispatcher.DispatchCount.ShouldBe(1);
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        counter.ReadAndReset().ShouldHaveSingleItem().FailureCount.ShouldBe(1);
        ((TrustedAuthorityBoundaryProxy)(object)status).Calls.ShouldBe(boundary == "status" ? ["TryGetAsync"] : []);
        CoarseIdempotencyRecord after = durable ? state.DomainRecords.Single() : ((InMemoryCoarseIdempotencyStore)real).Records.Single();
        after.ShouldBe(before);
        if (durable) { AssertPreparedOutcome(state.IdentityRecords.ShouldHaveSingleItem().PriorOutcome!, first.Accepted!); }
        (await realStatus.TryGetAsync("tenant-alpha", beforeStatus.OperationId, TestContext.Current.CancellationToken)).ShouldBe(beforeStatus);
    }

    /// <summary>
    /// Authority expiry in a real reconciliation read, either before or inside the guarded receipt restore, prevents
    /// recovery writes and queue acknowledgement and is denied rather than swallowed as a receipt failure.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ReplayReconciliationRechecksBeforeEveryDurableFollowup(int lapseOnCall)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) } };
        FakeCoarseIdempotencyStateClient state = new();
        RecordingReplayIntentQueue queue = new();
        RecordingDispatcher firstDispatcher = new(onDispatch: () => state.ThrowOutcomeWrites = 100);
        ChatBotCommandSubmission submission = Submission(TrustedAuthorityFixture.Principal(), AssociationDecisionCommand());
        ChatBotGatewayResult first = await Gateway(firstDispatcher, clock: clock, idempotencyStore: new DaprCoarseIdempotencyStore(state, clock), replayQueue: queue,
            auditWriter: new RecordingAuditWriter { PostCommitResult = AuditWriteResult.Unavailable() }, requestAuthorizer: TrustedAuthorityFixture.Authorizer(clock, owner)).SubmitAsync(submission, TestContext.Current.CancellationToken);
        first.IsAccepted.ShouldBeFalse();
        AuditReplayIntent prior = queue.Intents.ShouldHaveSingleItem();
        CoarseIdempotencyRecord beforeDomain = state.DomainRecords.ShouldHaveSingleItem();
        CoarseCommandIdentityRecord beforeIdentity = state.IdentityRecords.ShouldHaveSingleItem();
        state.ThrowOutcomeWrites = 0;
        bool reconcile = false;
        List<string> followups = [];
        ICoarseIdempotencyStateClient client = TrustedAuthorityBoundaryProxy.Create<ICoarseIdempotencyStateClient>(state, method =>
        {
            if (!reconcile) { return; }
            followups.Add(method);
            if (followups.Count == lapseOnCall) { clock.UtcNow += TimeSpan.FromSeconds(2); }
        });
        queue.OnSnapshot = () => reconcile = true;
        RecordingAuditWriter audit = new();
        InMemoryAuthorizationFailureCounter counter = new(clock);
        RecordingDispatcher retryDispatcher = new();
        ChatBotGatewayResult replay = await Gateway(retryDispatcher, clock: clock, idempotencyStore: new DaprCoarseIdempotencyStore(client, clock), replayQueue: queue,
            auditWriter: audit, authorizationFailureCounter: counter, requestAuthorizer: TrustedAuthorityFixture.Authorizer(clock, owner)).SubmitAsync(submission, TestContext.Current.CancellationToken);
        replay.Problem!.Code.ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        retryDispatcher.DispatchCount.ShouldBe(0);

        // The second identity read belongs to the guarded RecordOutcomeAsync receipt restore.
        followups.ShouldBe(Enumerable.Repeat("ReadIdentityAsync", lapseOnCall));
        queue.Intents.ShouldBe([prior]);
        state.DomainRecords.ShouldHaveSingleItem().ShouldBe(beforeDomain);
        state.IdentityRecords.ShouldHaveSingleItem().ShouldBe(beforeIdentity);
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        counter.ReadAndReset().ShouldHaveSingleItem().FailureCount.ShouldBe(1);
    }

    private sealed class AuthorityExpiryPreparationStore(IIdempotencyStore real, Action advance) : IIdempotencyStore
    {
        public bool AdvanceDuringPreparation { get; set; } = true;
        public bool Prepared { get; private set; }
        public ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken token) => real.RecordAdmissionAsync(context, token);
        public async ValueTask<bool> PrepareDispatchAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token)
        {
            Prepared = await real.PrepareDispatchAsync(metadata, outcome, token).ConfigureAwait(false);
            await Task.Yield();
            if (Prepared && AdvanceDuringPreparation) { advance(); }
            return Prepared;
        }
        public ValueTask<bool> BindDispatchTargetAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, string aggregateId, CancellationToken token)
            => real.BindDispatchTargetAsync(metadata, outcome, aggregateId, token);
        public ValueTask<bool> ConfirmSdkSubmissionAcceptedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, string aggregateId, CancellationToken token)
            => real.ConfirmSdkSubmissionAcceptedAsync(metadata, outcome, aggregateId, token);
        public ValueTask RecordOutcomeAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token) => real.RecordOutcomeAsync(metadata, outcome, token);
        public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken token) => real.AbortAdmissionAsync(metadata, token);
        public ValueTask AbortUndispatchedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token) => real.AbortUndispatchedAsync(metadata, outcome, token);
    }
}
