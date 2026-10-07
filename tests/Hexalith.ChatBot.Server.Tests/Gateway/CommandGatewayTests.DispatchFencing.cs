using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Governance.Outbound;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KnownUndispatchedAbortMustMatchPreparedOutcomeAndOwner(bool durable)
    {
        FakeCoarseIdempotencyStateClient state = new();
        FixedClock clock = new();
        IIdempotencyStore store = durable ? new DaprCoarseIdempotencyStore(state, clock) : new InMemoryCoarseIdempotencyStore(clock);
        ChatBotGatewayContext context = DirectContext(new TenantScopedCommand(BoundTenant, "before"), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyDecision owner = await store.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        CommandSubmissionResponse prepared = PreparedOutcome(context, clock.UtcNow);
        (await store.PrepareDispatchAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken)).ShouldBeTrue();
        CommandSubmissionResponse different = PreparedOutcome(context, clock.UtcNow.AddSeconds(1));
        if (durable)
        {
            await Should.ThrowAsync<InvalidOperationException>(() => store.AbortUndispatchedAsync(owner.Metadata, different, TestContext.Current.CancellationToken).AsTask());
            state.IdentityRecords.ShouldHaveSingleItem().DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
        }
        else
        {
            await store.AbortUndispatchedAsync(owner.Metadata, different, TestContext.Current.CancellationToken);
            ((InMemoryCoarseIdempotencyStore)store).Records.ShouldHaveSingleItem().DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
        }
        await store.AbortUndispatchedAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken);
        CoarseIdempotencyDecision replacement = await store.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        replacement.Kind.ShouldBe(CoarseIdempotencyDecisionKind.Proceed);
        await store.AbortUndispatchedAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken);
        (await store.PrepareDispatchAsync(replacement.Metadata, prepared, TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RealPlanningFailureShouldReleaseOnlyUndispatchedPreparedOwnership(bool durable, bool cleanupUnavailable)
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        IIdempotencyStore store = durable ? new DaprCoarseIdempotencyStore(state, clock) : new InMemoryCoarseIdempotencyStore(clock);
        DurableRecoveryEventStoreClient platform = new();
        AcceptedCommandDispatcher dispatcher = new(platform, null!, null!, clock);
        RecordingAuditWriter audit = new() { OnPreCommit = () => state.RejectDeletes = cleanupUnavailable ? 3 : 0 };
        ChatBotCommandSubmission bad = Submission(Principal(BoundTenant), AssociationScoringCommand("planning-kernel") with { SourceMailboxId = string.Empty });
        ChatBotGatewayResult failure = await Gateway(dispatcher, clock: clock, idempotencyStore: store, auditWriter: audit)
            .SubmitAsync(bad, TestContext.Current.CancellationToken);
        failure.IsAccepted.ShouldBeFalse();
        failure.Problem!.Status.ShouldBe(503);
        failure.Problem.Retryable.ShouldBeTrue();
        platform.SubmissionCount.ShouldBe(0);
        if (cleanupUnavailable)
        {
            state.IdentityRecords.ShouldHaveSingleItem().DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Aborted);
            state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldBeNull();
        }
        else if (durable)
        {
            state.IdentityRecords.ShouldBeEmpty();
            state.DomainRecords.ShouldBeEmpty();
        }
        else
        {
            ((InMemoryCoarseIdempotencyStore)store).Records.ShouldBeEmpty();
        }

        state.RejectDeletes = 0;
        IIdempotencyStore replacementStore = durable ? new DaprCoarseIdempotencyStore(state, clock) : store;
        ChatBotGatewayResult corrected = await Gateway(dispatcher, clock: clock, idempotencyStore: replacementStore)
            .SubmitAsync(Submission(Principal(BoundTenant), new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ")), TestContext.Current.CancellationToken);
        corrected.IsAccepted.ShouldBeTrue();
        platform.SubmissionCount.ShouldBe(1);
        if (durable)
        {
            state.IdentityRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
            state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
        }
        else
        {
            ((InMemoryCoarseIdempotencyStore)store).Records.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
        }
    }

    [Theory]
    [InlineData(false, "eventstore")]
    [InlineData(true, "eventstore")]
    [InlineData(false, "conversation")]
    [InlineData(true, "conversation")]
    [InlineData(false, "mailbox")]
    [InlineData(true, "mailbox")]
    public async Task PossibleExternalWriteShouldRemainRecoveryPendingWithoutRedispatch(bool durable, string boundary)
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        DurableRecoveryEventStoreClient platform = new() { SubmissionUncertain = boundary == "eventstore" };
        UncertainExternalWriters writers = new();
        AcceptedCommandDispatcher dispatcher = new(platform, null!, null!, clock,
            conversationWriter: writers, outboundMailboxSender: writers);
        IIdempotencyStore store = durable ? new DaprCoarseIdempotencyStore(state, clock, eventStore: platform) : new InMemoryCoarseIdempotencyStore(clock);
        object command = boundary switch
        {
            "conversation" => ApprovedExecutionCommand(),
            "mailbox" => OutboundSendCommand("send-uncertain"),
            _ => new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"),
        };
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant,
            new Claim(ParticipantAuthorizationStage.ActorTypeClaim, ParticipantAuthorizationStage.HumanActorValue),
            new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-001"),
            new Claim(OutboundDraftAuthorityEvaluator.ProjectScopeClaim, "project-001:outbound-send"),
            new Claim(OutboundDraftAuthorityEvaluator.TenantOutboundPolicyClaim, "authenticated-user-send"),
            new Claim(OutboundSendAuthorityEvaluator.MailboxIdClaim, "mailbox-001"),
            new Claim(OutboundSendAuthorityEvaluator.MailboxOwnerClaim, "mailbox-001"),
            new Claim(OutboundSendAuthorityEvaluator.OwnMailboxMailSendClaim, "true")), command);
        ChatBotGatewayResult first = await Gateway(dispatcher, clock: clock, idempotencyStore: store)
            .SubmitAsync(submission, TestContext.Current.CancellationToken);
        first.IsAccepted.ShouldBeFalse();
        first.Problem!.Status.ShouldBe(503);
        platform.SubmissionCount.ShouldBe(boundary == "eventstore" ? 1 : 0);
        writers.Attempts.ShouldBe(boundary == "eventstore" ? 0 : 1);
        clock.UtcNow += TimeSpan.FromHours(26);
        RecordingDispatcher replacementDispatcher = new();
        IIdempotencyStore replacement = durable ? new DaprCoarseIdempotencyStore(state, clock, eventStore: platform) : store;
        ChatBotGatewayResult retry = await Gateway(replacementDispatcher, clock: clock, idempotencyStore: replacement)
            .SubmitAsync(submission, TestContext.Current.CancellationToken).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        retry.IsAccepted.ShouldBeFalse();
        retry.Problem!.Status.ShouldBe(503);
        retry.Problem.Retryable.ShouldBeTrue();
        replacementDispatcher.DispatchCount.ShouldBe(0);
        if (durable)
        {
            state.IdentityRecords.ShouldHaveSingleItem().DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
            state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldBeNull();
        }
        else
        {
            ((InMemoryCoarseIdempotencyStore)store).Records.ShouldHaveSingleItem().DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
        }
    }

    [Fact]
    public async Task InMemoryExpiredReservedLeaseShouldReleaseWaitersAndFenceResumedOwner()
    {
        MutableClock clock = new(FixedClock.FixedUtcNow);
        InMemoryCoarseIdempotencyStore store = new(clock);
        ChatBotGatewayContext context = DirectContext(new TenantScopedCommand(BoundTenant, "before"), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyDecision owner = await store.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        Task<CoarseIdempotencyDecision> waiter = store.RecordAdmissionAsync(context, TestContext.Current.CancellationToken).AsTask();
        waiter.IsCompleted.ShouldBeFalse();
        clock.UtcNow += DaprCoarseIdempotencyStore.ReservationLease;
        ChatBotGatewayContext corrected = DirectContext(new TenantScopedCommand(BoundTenant, "corrected"), context.Submission.Request.CommandId);
        CoarseIdempotencyDecision replacement = await store.RecordAdmissionAsync(corrected, TestContext.Current.CancellationToken);
        replacement.Kind.ShouldBe(CoarseIdempotencyDecisionKind.Proceed);
        (await waiter.ConfigureAwait(true)).Kind.ShouldBe(CoarseIdempotencyDecisionKind.RecoveryPending);
        (await store.PrepareDispatchAsync(owner.Metadata, PreparedOutcome(context, clock.UtcNow), TestContext.Current.CancellationToken)).ShouldBeFalse();
        await store.AbortAdmissionAsync(owner.Metadata, TestContext.Current.CancellationToken);
        store.Records.ShouldHaveSingleItem().ReservationId.ShouldBe(replacement.Metadata.ReservationId);
        CommandSubmissionResponse accepted = PreparedOutcome(corrected, clock.UtcNow);
        (await store.PrepareDispatchAsync(replacement.Metadata, accepted, TestContext.Current.CancellationToken)).ShouldBeTrue();
        Task<CoarseIdempotencyDecision> duplicate = store.RecordAdmissionAsync(corrected, TestContext.Current.CancellationToken).AsTask();
        await store.RecordOutcomeAsync(replacement.Metadata, accepted, TestContext.Current.CancellationToken);
        AssertPreparedOutcome((await duplicate.ConfigureAwait(true)).PriorOutcome!, accepted);
        AssertPreparedOutcome((await store.RecordAdmissionAsync(corrected, TestContext.Current.CancellationToken)).PriorOutcome!, accepted);
        store.Records.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
    }

    [Fact]
    public async Task LosingDomainCleanupShouldSurviveUnrelatedWinningIdentityAndEveryServiceReplacement()
    {
        using Barrier barrier = new(2);
        FakeCoarseIdempotencyStateClient state = new() { IdentityClaimBarrier = barrier, RejectDeletes = 100 };
        MutableClock clock = new(FixedClock.FixedUtcNow);
        RecordingDispatcher dispatcher = new();
        ChatBotCommandSubmission first = Submission(Principal(BoundTenant), RetryCommand() with { FailedEventId = "01ARZ3NDEKTSV4RRFFQ69G5FC3" });
        ChatBotCommandSubmission second = Submission(Principal(BoundTenant), RetryCommand() with { FailedEventId = "01ARZ3NDEKTSV4RRFFQ69G5FC4" });
        CommandGateway gateway = Gateway(dispatcher, clock: clock, idempotencyStore: new DaprCoarseIdempotencyStore(state, clock));
        ChatBotGatewayResult[] results = await Task.WhenAll(
            Task.Run(() => gateway.SubmitAsync(first, TestContext.Current.CancellationToken).AsTask()),
            Task.Run(() => gateway.SubmitAsync(second, TestContext.Current.CancellationToken).AsTask()));
        results.Count(static result => result.IsAccepted).ShouldBe(1);
        dispatcher.DispatchCount.ShouldBe(1);
        CoarseCommandIdentityRecord winner = state.IdentityRecords.ShouldHaveSingleItem();
        CoarseIdempotencyRecord winningDomain = state.DomainRecords.Single(record => record.CoarseKeyHash == winner.DomainKeyHash);
        CoarseIdempotencyRecord losingDomain = state.DomainRecords.Single(record => record.CoarseKeyHash != winner.DomainKeyHash);
        losingDomain.DispatchState.ShouldBe(CoarseDispatchState.Aborted);
        state.RejectDeletes = 0;
        state.IdentityClaimBarrier = null;
        clock.UtcNow += DaprCoarseIdempotencyStore.ReservationLease;
        ChatBotCommandSubmission loser = winningDomain.CanonicalEquivalenceHash == CoarseIdempotencyComposer.ComposeCommandExecutionRecord(
            DirectContext(first.Request.Command, first.Request.CommandId), FixedClock.FixedUtcNow).CanonicalEquivalenceHash ? second : first;
        RecordingDispatcher freshDispatcher = new();
        CommandGateway replacement = Gateway(freshDispatcher, clock: clock, idempotencyStore: new DaprCoarseIdempotencyStore(state, clock));
        ChatBotGatewayResult fresh = await replacement.SubmitAsync(loser with
        {
            Request = new CommandSubmissionRequest { CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FBB", CommandType = loser.Request.CommandType,
                Command = loser.Request.Command, RequestSchemaVersion = loser.Request.RequestSchemaVersion },
        }, TestContext.Current.CancellationToken);
        fresh.IsAccepted.ShouldBeTrue();
        freshDispatcher.DispatchCount.ShouldBe(1);
        state.IdentityRecords.Single(record => record.CommandId == winner.CommandId).ShouldBe(winner);
        state.DomainRecords.Single(record => record.CoarseKeyHash == winner.DomainKeyHash).ShouldBe(winningDomain);
        state.DomainRecords.Single(record => record.CoarseKeyHash == losingDomain.CoarseKeyHash).ReservationId.ShouldNotBe(losingDomain.ReservationId);
        state.DomainRecords.ShouldAllBe(static record => record.PriorOutcome != null);
    }

    [Fact]
    public async Task MissingRetryStatusShouldRetainAttemptPolicyAndRequireAuditReconciliationOnReplay()
    {
        FakeCoarseIdempotencyStateClient state = new();
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), RetryCommand());
        ChatBotGatewayResult accepted = await Gateway(new RecordingDispatcher(), idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock()))
            .SubmitAsync(submission, TestContext.Current.CancellationToken);
        InMemoryOperationStatusStore replacementStatus = new();
        RecordingDispatcher replacementDispatcher = new();
        ChatBotGatewayResult replay = await Gateway(replacementDispatcher, operationStatusStore: replacementStatus,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock())).SubmitAsync(submission, TestContext.Current.CancellationToken);
        replay.IsAccepted.ShouldBeTrue();
        AssertPreparedOutcome(replay.Accepted!, accepted.Accepted!);
        replacementDispatcher.DispatchCount.ShouldBe(0);
        OperationStatusRecord status = (await replacementStatus.TryGetAsync(BoundTenant, accepted.Accepted!.OperationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        status.OperationClass.ShouldBe(CoarseIdempotencyOperationClass.Retry.Code);
        status.RetryCount.ShouldBe(1);
        status.MaxAttempts.ShouldBe(5);
        status.AuditStatus.ShouldBe(OperationStatusRecord.AuditReconciling);
    }
}
