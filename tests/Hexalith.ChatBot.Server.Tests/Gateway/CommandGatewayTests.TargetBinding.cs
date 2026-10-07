using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.Outbound;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Fact]
    public async Task ProductionDispatcherWithoutGatewayTargetBindingMustFailClosedBeforeSdkSubmission()
    {
        DurableRecoveryEventStoreClient platform = new();
        ChatBotGatewayContext context = DirectContext(new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        await Should.ThrowAsync<CommandNotSubmittedException>(() => new AcceptedCommandDispatcher(platform, null!, null!, new FixedClock())
            .DispatchAsync(context, TestContext.Current.CancellationToken).AsTask());
        platform.SubmissionCount.ShouldBe(0);
        context.ExternalEffectAttempted.ShouldBeFalse();
        context.PreparedAggregateId.ShouldBeNull();
    }

    [Fact]
    public async Task ActualEnrichedDispatchPlanMustBindItsStateOwnerTargetBeforeTheSdkPost()
    {
        FakeCoarseIdempotencyStateClient state = new();
        FixedClock clock = new();
        const string stateOwner = "01ARZ3NDEKTSV4RRFFQ69G5FBB";
        DurableRecoveryEventStoreClient platform = new()
        {
            OnReceivedSubmission = request =>
            {
                request.AggregateId.ShouldBe(stateOwner);
                CoarseIdempotencyRecord bound = state.IdentityRecords.ShouldHaveSingleItem().DomainReservation!;
                bound.PreparedAggregateId.ShouldBe(request.AggregateId);
                bound.DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
                bound.PreparedOutcome.ShouldNotBeNull().CommandId.ShouldBe(request.MessageId);
            },
        };
        AcceptedCommandDispatcher dispatcher = new(platform, null!, null!, clock,
            conversationWriter: new UncertainExternalWriters { AcknowledgeWrites = true });
        ChatBotGatewayResult accepted = await Gateway(dispatcher, clock: clock, idempotencyStore: new DaprCoarseIdempotencyStore(state, clock))
            .SubmitAsync(Submission(Principal(BoundTenant), ApprovedExecutionCommand() with { StateOwnerAggregateId = stateOwner }), TestContext.Current.CancellationToken);
        accepted.IsAccepted.ShouldBeTrue();
        platform.SubmissionCount.ShouldBe(1);
        state.IdentityRecords.Single().DomainReservation!.PreparedAggregateId.ShouldBe(stateOwner);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatchTargetBindingMustMatchPreparedOutcomeAndCurrentOwner(bool durable)
    {
        FakeCoarseIdempotencyStateClient state = new();
        FixedClock clock = new();
        IIdempotencyStore store = durable ? new DaprCoarseIdempotencyStore(state, clock) : new InMemoryCoarseIdempotencyStore(clock);
        ChatBotGatewayContext context = DirectContext(new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyDecision owner = await store.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        CommandSubmissionResponse prepared = PreparedOutcome(context, clock.UtcNow);
        const string aggregate = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
        (await store.BindDispatchTargetAsync(owner.Metadata, prepared, aggregate, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await store.PrepareDispatchAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await store.BindDispatchTargetAsync(owner.Metadata, prepared, " ", TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await store.BindDispatchTargetAsync(owner.Metadata, PreparedOutcome(context, clock.UtcNow.AddTicks(1)), aggregate, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await store.BindDispatchTargetAsync(owner.Metadata, prepared, aggregate, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await store.BindDispatchTargetAsync(owner.Metadata, prepared, "01ARZ3NDEKTSV4RRFFQ69G5FBB", TestContext.Current.CancellationToken)).ShouldBeFalse();
        CoarseIdempotencyRecord bound = durable ? state.IdentityRecords.Single().DomainReservation! : ((InMemoryCoarseIdempotencyStore)store).Records.Single();
        bound.PreparedAggregateId.ShouldBe(aggregate);
        AssertPreparedOutcome(bound.PreparedOutcome!, prepared);

        await store.AbortUndispatchedAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken);
        CoarseIdempotencyDecision replacement = await store.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        replacement.Metadata.ReservationId.ShouldNotBe(owner.Metadata.ReservationId);
        (await store.PrepareDispatchAsync(replacement.Metadata, prepared, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await store.BindDispatchTargetAsync(owner.Metadata, prepared, aggregate, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await store.BindDispatchTargetAsync(replacement.Metadata, prepared, aggregate, TestContext.Current.CancellationToken)).ShouldBeTrue();
        await store.RecordOutcomeAsync(replacement.Metadata, prepared, TestContext.Current.CancellationToken);
        (await store.BindDispatchTargetAsync(replacement.Metadata, prepared, aggregate, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("none", false)]
    [InlineData("none", true)]
    [InlineData("conversation", false)]
    [InlineData("conversation", true)]
    [InlineData("mailbox", false)]
    [InlineData("mailbox", true)]
    public async Task FailedTargetBindingMustPreventSdkSubmissionAndPreserveAttemptedWriterOwnership(string writer, bool acknowledgementLost)
    {
        FakeCoarseIdempotencyStateClient state = new()
        {
            RejectTargetBindings = acknowledgementLost ? 0 : 1,
            ThrowAfterTargetBindingSave = acknowledgementLost,
        };
        MutableClock clock = new(FixedClock.FixedUtcNow);
        DurableRecoveryEventStoreClient platform = new();
        UncertainExternalWriters writers = new() { AcknowledgeWrites = true };
        AcceptedCommandDispatcher dispatcher = new(platform, null!, null!, clock, conversationWriter: writers, outboundMailboxSender: writers);
        object command = writer switch
        {
            "conversation" => ApprovedExecutionCommand(),
            "mailbox" => OutboundSendCommand("target-fence-send"),
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
        ChatBotGatewayResult first = await Gateway(dispatcher, clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform)).SubmitAsync(submission, TestContext.Current.CancellationToken);
        if (writer == "mailbox")
        {
            first.IsAccepted.ShouldBeFalse();
            platform.SubmissionCount.ShouldBe(0);
            writers.Attempts.ShouldBe(0);
            state.IdentityRecords.ShouldBeEmpty();
            state.DomainRecords.ShouldBeEmpty();

            return;
        }
        first.IsAccepted.ShouldBeFalse();
        first.Problem.ShouldNotBeNull().Status.ShouldBe(503);
        platform.SubmissionCount.ShouldBe(0);
        writers.Attempts.ShouldBe(writer == "none" ? 0 : 1);

        if (writer == "none")
        {
            state.IdentityRecords.ShouldBeEmpty();
            state.DomainRecords.ShouldBeEmpty();
            ChatBotGatewayResult corrected = await Gateway(new AcceptedCommandDispatcher(platform, null!, null!, clock), clock: clock,
                idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform)).SubmitAsync(submission, TestContext.Current.CancellationToken);
            corrected.IsAccepted.ShouldBeTrue();
            platform.SubmissionCount.ShouldBe(1);
            state.IdentityRecords.Single().DomainReservation!.PreparedAggregateId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAZ");
        }
        else
        {
            CoarseCommandIdentityRecord retained = state.IdentityRecords.ShouldHaveSingleItem();
            retained.DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
            retained.PriorOutcome.ShouldBeNull();
            retained.DomainReservation.PreparedOutcome.ShouldNotBeNull();
            retained.DomainReservation.PreparedAggregateId.ShouldBe(acknowledgementLost ?
                writer == "conversation" ? ApprovedExecutionCommand().StateOwnerAggregateId ?? ApprovedExecutionCommand().ProjectId : OutboundSendCommand("target-fence-send").DraftId : null);
            clock.UtcNow += TimeSpan.FromHours(25);
            RecordingDispatcher replacementDispatcher = new();
            ChatBotGatewayResult retry = await Gateway(replacementDispatcher, clock: clock,
                idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform)).SubmitAsync(submission, TestContext.Current.CancellationToken);
            retry.IsAccepted.ShouldBeFalse();
            retry.Problem.ShouldNotBeNull().Status.ShouldBe(503);
            retry.Problem.Retryable.ShouldBeTrue();
            replacementDispatcher.DispatchCount.ShouldBe(0);
            platform.SubmissionCount.ShouldBe(0);
            writers.Attempts.ShouldBe(1);
            state.IdentityRecords.Single().ShouldBe(retained);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PausedTargetBindingMustLoseItsCasAfterExactPreparedReleaseAndReplacement(bool productionClient)
    {
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        FakeCoarseIdempotencyStateClient fake = new() { OwnershipWriteEntered = entered, OwnershipWriteRelease = release };
        using ConditionalDaprStateClient backend = new() { OwnershipWriteEntered = entered, OwnershipWriteRelease = release };
        ICoarseIdempotencyStateClient state = productionClient ? new DaprCoarseIdempotencyStateClient(backend) : fake;
        MutableClock clock = new(FixedClock.FixedUtcNow);
        const string aggregate = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
        ChatBotGatewayContext context = DirectContext(new RecordGovernedNote(aggregate), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        DaprCoarseIdempotencyStore oldStore = new(state, clock);
        CoarseIdempotencyDecision oldOwner = await oldStore.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        CommandSubmissionResponse oldOutcome = PreparedOutcome(context, clock.UtcNow);
        (await oldStore.PrepareDispatchAsync(oldOwner.Metadata, oldOutcome, TestContext.Current.CancellationToken)).ShouldBeTrue();
        context.SetDispatchTargetBinding((target, token) => oldStore.BindDispatchTargetAsync(oldOwner.Metadata, oldOutcome, target, token));
        fake.BlockNextWriteKind = backend.BlockNextWriteKind = "target";
        DurableRecoveryEventStoreClient platform = new();
        Task oldDispatch = Task.Run(() => new AcceptedCommandDispatcher(platform, null!, null!, clock)
            .DispatchAsync(context, TestContext.Current.CancellationToken).AsTask(), TestContext.Current.CancellationToken);
        try
        {
            entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
            await new DaprCoarseIdempotencyStore(state, clock).AbortUndispatchedAsync(oldOwner.Metadata, oldOutcome, TestContext.Current.CancellationToken);
            clock.UtcNow += DaprCoarseIdempotencyStore.ReservationLease;
            DaprCoarseIdempotencyStore replacement = new(state, clock);
            CoarseIdempotencyDecision owner = await replacement.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
            owner.Metadata.ReservationId.ShouldNotBe(oldOwner.Metadata.ReservationId);
            CommandSubmissionResponse outcome = PreparedOutcome(context, clock.UtcNow);
            (await replacement.PrepareDispatchAsync(owner.Metadata, outcome, TestContext.Current.CancellationToken)).ShouldBeTrue();
            (await replacement.BindDispatchTargetAsync(owner.Metadata, outcome, aggregate, TestContext.Current.CancellationToken)).ShouldBeTrue();
            await replacement.RecordOutcomeAsync(owner.Metadata, outcome, TestContext.Current.CancellationToken);
            release.Set();
            await Should.ThrowAsync<CommandNotSubmittedException>(() => oldDispatch);
            platform.SubmissionCount.ShouldBe(0);
            await oldStore.AbortUndispatchedAsync(oldOwner.Metadata, oldOutcome, TestContext.Current.CancellationToken);
            await AssertCurrentDurableOwnerAndReplayAsync(state, clock, context, owner, outcome).ConfigureAwait(true);
            (CoarseCommandIdentityRecord? retained, _) = await state.ReadIdentityAsync(owner.Metadata.IdentityKeyHash!, TestContext.Current.CancellationToken);
            retained!.DomainReservation!.PreparedAggregateId.ShouldBe(aggregate);
            (fake.PhysicalDeletes + backend.PhysicalDeletes).ShouldBe(0);
        }
        finally
        {
            release.Set();
        }
    }

    [Theory]
    [InlineData("matching", true)]
    [InlineData("wrong", true)]
    [InlineData("absent", true)]
    [InlineData("matching", false)]
    public async Task SdkBackedQueuedReconciliationMustAlsoMatchThePreparedAggregateTarget(string aggregateProof, bool confirmationPresent)
    {
        FakeCoarseIdempotencyStateClient state = new();
        FixedClock clock = new();
        DurableRecoveryEventStoreClient platform = new() { RetainCommittedEvidence = true, OnReceivedSubmission = _ => state.ThrowOutcomeWrites = 100 };
        RecordingReplayIntentQueue queue = new();
        const string aggregate = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new RecordGovernedNote(aggregate));
        ChatBotGatewayResult failed = await Gateway(new AcceptedCommandDispatcher(platform, null!, null!, clock), clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform), replayQueue: queue,
            auditWriter: new RecordingAuditWriter { PostCommitResult = Hexalith.ChatBot.Server.Audit.AuditWriteResult.Unavailable("audit_unavailable") })
            .SubmitAsync(submission, TestContext.Current.CancellationToken);
        failed.IsAccepted.ShouldBeFalse();
        Hexalith.ChatBot.Server.Audit.AuditReplayIntent intent = queue.Intents.ShouldHaveSingleItem();
        CommandSubmissionResponse prepared = state.IdentityRecords.Single().DomainReservation!.PreparedOutcome.ShouldNotBeNull();
        state.ThrowOutcomeWrites = 0;
        platform.Evidence = platform.Evidence! with
        {
            AggregateId = aggregateProof switch { "wrong" => "01ARZ3NDEKTSV4RRFFQ69G5FBB", "absent" => null, _ => aggregate },
        };
        if (!confirmationPresent)
        {
            CoarseCommandIdentityRecord identity = state.IdentityRecords.Single();
            (CoarseCommandIdentityRecord? _, string etag) = await state.ReadIdentityAsync(identity.DomainReservation!.IdentityKeyHash!, TestContext.Current.CancellationToken);
            (await state.TrySaveIdentityAsync(identity.DomainReservation.IdentityKeyHash!, identity with
            {
                DomainReservation = identity.DomainReservation with { SdkSubmissionAccepted = false },
            }, etag, TestContext.Current.CancellationToken)).ShouldBeTrue();
        }
        bool reconciled = await new DaprCoarseIdempotencyStore(state, clock, eventStore: platform)
            .ReconcileOutcomeAsync(intent, TestContext.Current.CancellationToken);
        reconciled.ShouldBe(aggregateProof == "matching" && confirmationPresent);
        state.IdentityRecords.Single().DomainReservation!.PreparedAggregateId.ShouldBe(aggregate);
        if (reconciled)
        {
            AssertPreparedOutcome(state.IdentityRecords.Single().PriorOutcome!, prepared);
        }
        else
        {
            state.IdentityRecords.Single().PriorOutcome.ShouldBeNull();
            state.DomainRecords.Single().PriorOutcome.ShouldBeNull();
        }
        platform.SubmissionCount.ShouldBe(1);
    }
}
