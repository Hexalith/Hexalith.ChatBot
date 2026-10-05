using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.EventStore.Contracts.Commands;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LostPreparationAcknowledgementShouldAbortOnlyWithAPersistedMatchingFenceBeforeDispatch(bool abortFenceUnavailable)
    {
        FakeCoarseIdempotencyStateClient state = new();
        RecordingDispatcher originalDispatcher = new();
        RecordingAuditWriter audit = new()
        {
            // Admission has already acquired ownership before this callback enables loss
            // of the preparation write's acknowledgement.
            OnPreCommit = () =>
            {
                state.ThrowAfterIdentityClaimSave = true;
                state.RejectAbortFenceSaves = abortFenceUnavailable ? 1 : 0;
            },
        };
        ChatBotCommandSubmission original = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "before"));
        ChatBotGatewayResult failed = await Gateway(originalDispatcher, auditWriter: audit,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock())).SubmitAsync(original, TestContext.Current.CancellationToken);

        failed.IsAccepted.ShouldBeFalse();
        failed.Problem!.Code.ShouldBe(ChatBotMessageCodes.DependencyDegraded);
        failed.Problem.Status.ShouldBe(503);
        failed.Problem.Retryable.ShouldBeTrue();
        failed.Problem.SchemaVersion.ShouldBe(ChatBotMessageCatalogVersion.Current);
        originalDispatcher.DispatchCount.ShouldBe(0);

        RecordingDispatcher replacementDispatcher = new();
        CommandGateway replacement = Gateway(replacementDispatcher,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock()));
        if (abortFenceUnavailable)
        {
            state.IdentityRecords.ShouldHaveSingleItem().DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
            state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldBeNull();
            ChatBotGatewayResult retry = await replacement.SubmitAsync(original, TestContext.Current.CancellationToken);
            retry.IsAccepted.ShouldBeFalse();
            retry.Problem!.Status.ShouldBe(503);
            retry.Problem.Retryable.ShouldBeTrue();
            replacementDispatcher.DispatchCount.ShouldBe(0);
            state.IdentityRecords.Single().DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
        }
        else
        {
            state.IdentityRecords.ShouldBeEmpty();
            state.DomainRecords.ShouldBeEmpty();
            ChatBotGatewayResult corrected = await replacement.SubmitAsync(
                Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "corrected")), TestContext.Current.CancellationToken);
            corrected.IsAccepted.ShouldBeTrue();
            replacementDispatcher.DispatchCount.ShouldBe(1);
            state.IdentityRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
            state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task DurableAbortedFenceShouldRecoverCleanupAfterEveryApplicationServiceIsReplaced()
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        DaprCoarseIdempotencyStore original = new(state, clock);
        CoarseIdempotencyDecision admitted = await original.RecordAdmissionAsync(
            DirectContext(new TenantScopedCommand(BoundTenant, "before"), "01ARZ3NDEKTSV4RRFFQ69G5FAY"), TestContext.Current.CancellationToken);
        state.RejectDeletes = 3;
        await Should.ThrowAsync<InvalidOperationException>(() => original.AbortAdmissionAsync(admitted.Metadata, TestContext.Current.CancellationToken).AsTask());
        state.IdentityRecords.Single().DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Aborted);

        state.RejectDeletes = 0;
        RecordingDispatcher replacementDispatcher = new();
        CommandGateway replacement = Gateway(replacementDispatcher, clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock));
        ChatBotGatewayResult corrected = await replacement.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "corrected")), TestContext.Current.CancellationToken);

        corrected.IsAccepted.ShouldBeTrue();
        replacementDispatcher.DispatchCount.ShouldBe(1);
        state.IdentityRecords.ShouldHaveSingleItem().DomainReservation!.ReservationId.ShouldNotBe(admitted.Metadata.ReservationId);
        state.IdentityRecords.Single().PriorOutcome.ShouldNotBeNull();
        state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
    }

    [Fact]
    public async Task ActiveLeaseShouldSurviveAnotherInstanceAndExpiredOldOwnerMustNotDispatch()
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        DaprCoarseIdempotencyStore original = new(state, clock);
        ChatBotGatewayContext oldContext = DirectContext(new TenantScopedCommand(BoundTenant, "before"), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyDecision owned = await original.RecordAdmissionAsync(oldContext, TestContext.Current.CancellationToken);
        DaprCoarseIdempotencyStore second = new(state, clock);
        (await second.RecordAdmissionAsync(oldContext, TestContext.Current.CancellationToken)).Kind.ShouldBe(CoarseIdempotencyDecisionKind.RecoveryPending);
        ChatBotGatewayContext corrected = DirectContext(new TenantScopedCommand(BoundTenant, "corrected"), oldContext.Submission.Request.CommandId);
        (await second.RecordAdmissionAsync(corrected, TestContext.Current.CancellationToken)).Kind.ShouldBe(CoarseIdempotencyDecisionKind.Conflict);
        state.IdentityRecords.Single().DomainReservation!.ReservationId.ShouldBe(owned.Metadata.ReservationId);
        clock.UtcNow += DaprCoarseIdempotencyStore.ReservationLease;
        (await second.RecordAdmissionAsync(corrected, TestContext.Current.CancellationToken)).Kind.ShouldBe(CoarseIdempotencyDecisionKind.Proceed);
        (await original.PrepareDispatchAsync(owned.Metadata, PreparedOutcome(oldContext, clock.UtcNow), TestContext.Current.CancellationToken)).ShouldBeFalse();
        state.DomainRecords.ShouldHaveSingleItem().ReservationId.ShouldNotBe(owned.Metadata.ReservationId);
    }

    [Fact]
    public async Task AbortedInMemoryAdmissionShouldReleaseWaitersWithoutCancelingTheirRequestsOrDispatchingAnOldOwner()
    {
        InMemoryCoarseIdempotencyStore store = new(new FixedClock());
        ChatBotGatewayContext first = DirectContext(new TenantScopedCommand(BoundTenant, "before"), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyDecision owner = await store.RecordAdmissionAsync(first, TestContext.Current.CancellationToken);
        Task<CoarseIdempotencyDecision> duplicate = store.RecordAdmissionAsync(first, TestContext.Current.CancellationToken).AsTask();
        duplicate.IsCompleted.ShouldBeFalse();
        await store.AbortAdmissionAsync(owner.Metadata, TestContext.Current.CancellationToken);
        (await duplicate.ConfigureAwait(true)).Kind.ShouldBe(CoarseIdempotencyDecisionKind.RecoveryPending);
        store.Records.ShouldBeEmpty();
        ChatBotGatewayContext replacement = DirectContext(new TenantScopedCommand(BoundTenant, "corrected"), first.Submission.Request.CommandId);
        (await store.RecordAdmissionAsync(replacement, TestContext.Current.CancellationToken)).Kind.ShouldBe(CoarseIdempotencyDecisionKind.Proceed);
        (await store.PrepareDispatchAsync(owner.Metadata, PreparedOutcome(first, new FixedClock().UtcNow), TestContext.Current.CancellationToken)).ShouldBeFalse();
        store.Records.ShouldHaveSingleItem().ReservationId.ShouldNotBe(owner.Metadata.ReservationId);
    }

    [Fact]
    public async Task HistoricalUnknownDispatchReservationShouldRemainFailClosedAfterExpiry()
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        ChatBotGatewayContext context = DirectContext(AssociationDecisionCommand(), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyRecord historical = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(context, clock.UtcNow);
        state.SeedDomain(historical);
        clock.UtcNow += TimeSpan.FromHours(25);
        (await new DaprCoarseIdempotencyStore(state, clock).RecordAdmissionAsync(context, TestContext.Current.CancellationToken)).Kind
            .ShouldBe(CoarseIdempotencyDecisionKind.RecoveryPending);
        state.DomainRecords.ShouldHaveSingleItem().ShouldBe(historical);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnexpiredLegacyGenericRecordShouldRetainItsOutcomeOrReservationAcrossDifferentCallerIds(bool committed)
    {
        FakeCoarseIdempotencyStateClient state = new();
        FixedClock clock = new();
        ChatBotGatewayContext original = DirectContext(new TenantScopedCommand(BoundTenant, "before"), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyRecord current = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(original, clock.UtcNow);
        CommandSubmissionResponse prepared = PreparedOutcome(original, clock.UtcNow);
        state.SeedDomain(current with
        {
            CoarseKeyHash = current.LegacyKeyHash!, IdentityKeyHash = null, CallerFingerprint = null,
            LegacyKeyHash = null, PriorOutcome = committed ? prepared : null,
        });
        CoarseIdempotencyDecision retry = await new DaprCoarseIdempotencyStore(state, clock).RecordAdmissionAsync(
            DirectContext(new TenantScopedCommand(BoundTenant, "before"), "01ARZ3NDEKTSV4RRFFQ69G5FBB"), TestContext.Current.CancellationToken);
        retry.Kind.ShouldBe(committed ? CoarseIdempotencyDecisionKind.ReplayPriorOutcome : CoarseIdempotencyDecisionKind.RecoveryPending);
        state.DomainRecords.ShouldHaveSingleItem();
        if (committed)
        {
            AssertPreparedOutcome(retry.PriorOutcome!, prepared);
        }
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("events-stored")]
    [InlineData("events-published")]
    [InlineData("publish-failed")]
    [InlineData("no-op")]
    [InlineData("rejection-event")]
    [InlineData("rejected")]
    [InlineData("missing")]
    [InlineData("message")]
    [InlineData("tenant")]
    [InlineData("domain")]
    [InlineData("processing")]
    [InlineData("correlation")]
    [InlineData("status-name")]
    [InlineData("no-commit")]
    [InlineData("unavailable")]
    public async Task PreparedOutcomeShouldRecoverAcrossEveryServiceReplacementOnlyWithMatchingSdkCommitEvidence(string scenario)
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        DurableRecoveryEventStoreClient platform = new();
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), AssociationDecisionCommand());
        RecordingDispatcher originalDispatcher = new(onDispatch: () =>
        {
            state.ThrowOutcomeWrites = 100;
            platform.Evidence = new CommandStatusQueryResponse(submission.CorrelationId, nameof(CommandStatus.Completed),
                (int)CommandStatus.Completed, MessageId: submission.Request.CommandId)
            {
                TenantId = BoundTenant, Domain = "chatbot", CommittedEventSequence = 1, EventCount = 1,
            };
        });
        CommandGateway original = Gateway(originalDispatcher, clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform),
            auditWriter: new RecordingAuditWriter { PostCommitResult = AuditWriteResult.Unavailable("audit_unavailable") },
            commandAllowlist: new ChatBotSpineCommandAllowlist());
        ChatBotGatewayResult failed = await original.SubmitAsync(submission, TestContext.Current.CancellationToken);
        failed.Problem!.Code.ShouldBe(ChatBotMessageCodes.DependencyDegraded);
        CommandSubmissionResponse prepared = state.IdentityRecords.Single().DomainReservation!.PreparedOutcome.ShouldNotBeNull();
        prepared.AcceptedAt.Offset.ShouldBe(TimeSpan.Zero);
        state.DomainRecords.Single().PriorOutcome.ShouldBeNull();
        state.IdentityRecords.Single().PriorOutcome.ShouldBeNull();
        originalDispatcher.DispatchCount.ShouldBe(1);

        platform.Evidence = scenario switch
        {
            "missing" => null,
            "message" => platform.Evidence! with { MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FBB" },
            "tenant" => platform.Evidence! with { TenantId = OtherTenant },
            "domain" => platform.Evidence! with { Domain = "other" },
            "processing" => platform.Evidence! with { Status = nameof(CommandStatus.Processing), StatusCode = (int)CommandStatus.Processing },
            "correlation" => platform.Evidence! with { CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FBB" },
            "status-name" => platform.Evidence! with { Status = nameof(CommandStatus.Rejected) },
            "no-commit" => platform.Evidence! with { CommittedEventSequence = null, EventCount = null },
            "events-stored" => platform.Evidence! with { Status = nameof(CommandStatus.EventsStored), StatusCode = (int)CommandStatus.EventsStored },
            "events-published" => platform.Evidence! with { Status = nameof(CommandStatus.EventsPublished), StatusCode = (int)CommandStatus.EventsPublished },
            "publish-failed" => platform.Evidence! with { Status = nameof(CommandStatus.PublishFailed), StatusCode = (int)CommandStatus.PublishFailed },
            "no-op" => platform.Evidence! with { CommittedEventSequence = null, EventCount = 0 },
            "rejection-event" => platform.Evidence! with { RejectionEventType = "CommandRejected" },
            "rejected" => platform.Evidence! with { Status = nameof(CommandStatus.Rejected), StatusCode = (int)CommandStatus.Rejected },
            _ => platform.Evidence,
        };
        platform.Unavailable = scenario == "unavailable";
        state.ThrowOutcomeWrites = 0;
        clock.UtcNow += TimeSpan.FromHours(25);
        RecordingDispatcher replacementDispatcher = new();
        InMemoryOperationStatusStore replacementStatus = new();
        CommandGateway replacement = Gateway(replacementDispatcher, clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform),
            operationStatusStore: replacementStatus,
            commandAllowlist: new ChatBotSpineCommandAllowlist());
        ChatBotGatewayResult recovered = await replacement.SubmitAsync(submission, TestContext.Current.CancellationToken);

        replacementDispatcher.DispatchCount.ShouldBe(0);
        if (scenario is "valid" or "events-stored" or "events-published" or "publish-failed" or "no-op")
        {
            recovered.IsAccepted.ShouldBeTrue();
            AssertPreparedOutcome(recovered.Accepted!, prepared);
            AssertPreparedOutcome(state.IdentityRecords.Single().PriorOutcome!, prepared);
            AssertPreparedOutcome(state.ReceiptRecords.Single().PriorOutcome!, prepared);
            var status = (await replacementStatus.TryGetAsync(BoundTenant, prepared.OperationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
            status.OperationClass.ShouldBe(state.IdentityRecords.Single().DomainReservation!.OperationClass);
            status.AuditStatus.ShouldBe(OperationStatusRecord.AuditReconciling);
        }
        else
        {
            recovered.IsAccepted.ShouldBeFalse();
            recovered.Problem!.Status.ShouldBe(503);
            recovered.Problem.Retryable.ShouldBeTrue();
            state.IdentityRecords.Single().PriorOutcome.ShouldBeNull();
            state.ReceiptRecords.Single().PriorOutcome.ShouldBeNull();
        }
    }

    [Fact]
    public async Task FailedSpecializedReceiptCreateWithoutAnOwnerShouldBeRetryableAndLeaveNoReservation()
    {
        FakeCoarseIdempotencyStateClient state = new() { RejectReceiptCreates = 1 };
        DaprCoarseIdempotencyStore store = new(state, new FixedClock());
        CoarseIdempotencyDecision result = await store.RecordAdmissionAsync(
            DirectContext(AssociationDecisionCommand(), "01ARZ3NDEKTSV4RRFFQ69G5FAY"), TestContext.Current.CancellationToken);
        result.Kind.ShouldBe(CoarseIdempotencyDecisionKind.RecoveryPending);
        state.DomainRecords.ShouldBeEmpty();
        state.IdentityRecords.ShouldBeEmpty();
        state.ReceiptRecords.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ThrowingOrFailedPreCommitAuditShouldPreserveAuditResponseWhenCleanupIsUnavailable(bool thrown)
    {
        FakeCoarseIdempotencyStateClient state = new() { RejectDeletes = 100 };
        RecordingDispatcher dispatcher = new();
        CommandGateway gateway = Gateway(dispatcher, idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock()),
            auditWriter: new RecordingAuditWriter { ThrowPreCommit = thrown, PreCommitResult = AuditWriteResult.Unavailable("audit_unavailable") });
        ChatBotGatewayResult failed = await gateway.SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "before")), TestContext.Current.CancellationToken);
        failed.Problem!.Code.ShouldBe(ChatBotMessageCodes.AuditUnavailable);
        dispatcher.DispatchCount.ShouldBe(0);
        state.IdentityRecords.Single().DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Aborted);
        state.RejectDeletes = 0;
        ChatBotGatewayResult corrected = await Gateway(dispatcher,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock())).SubmitAsync(
                Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "corrected")), TestContext.Current.CancellationToken);
        corrected.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
    }

    [Fact]
    public async Task NotificationAndQueueSnapshotFailuresShouldNotReplaceRetryableRecoveryResponse()
    {
        FakeCoarseIdempotencyStateClient state = new();
        RecordingDispatcher dispatcher = new(onDispatch: () => state.ThrowOutcomeWrites = 100);
        CommandGateway gateway = Gateway(dispatcher, idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock()),
            replayQueue: new RecordingReplayIntentQueue { ThrowNotifications = true, ThrowSnapshot = true },
            alertSink: new RecordingOperatorAlertSink { ThrowNotifications = true });
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "before"));
        ChatBotGatewayResult first = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        ChatBotGatewayResult retry = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        first.Problem!.Code.ShouldBe(ChatBotMessageCodes.DependencyDegraded);
        retry.Problem!.Code.ShouldBe(ChatBotMessageCodes.DependencyDegraded);
        first.Problem.Retryable.ShouldBeTrue();
        retry.Problem.Retryable.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        state.IdentityRecords.Single().DomainReservation!.PreparedOutcome.ShouldNotBeNull();
    }

    [Fact]
    public async Task LifecycleExceptionShouldFenceOwnershipAndKeepDependencyCauseDuringCleanupFailure()
    {
        FakeCoarseIdempotencyStateClient state = new() { RejectDeletes = 100 };
        RecordingDispatcher dispatcher = new();
        ChatBotGatewayResult failed = await Gateway(dispatcher,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock()),
            lifecycleTransitionGuard: new RecordingLifecycleTransitionGuard([], throwValidation: true)).SubmitAsync(
                Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "before")), TestContext.Current.CancellationToken);
        failed.Problem!.Code.ShouldBe(ChatBotMessageCodes.DependencyDegraded);
        dispatcher.DispatchCount.ShouldBe(0);
        state.IdentityRecords.Single().DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Aborted);
        state.RejectDeletes = 0;
        ChatBotGatewayResult corrected = await Gateway(dispatcher,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock())).SubmitAsync(
                Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "corrected")), TestContext.Current.CancellationToken);
        corrected.IsAccepted.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(1);
        state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
    }

    [Fact]
    public async Task StateReadOutageShouldReturnDependencyFailureWithoutCreatingConflictOrAuditCause()
    {
        FakeCoarseIdempotencyStateClient state = new() { ThrowReads = 1 };
        RecordingDispatcher dispatcher = new();
        ChatBotGatewayResult failed = await Gateway(dispatcher,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock())).SubmitAsync(
                Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "before")), TestContext.Current.CancellationToken);
        failed.Problem!.Code.ShouldBe(ChatBotMessageCodes.DependencyDegraded);
        failed.Problem.Status.ShouldBe(503);
        failed.Problem.Retryable.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(0);
        state.DomainRecords.ShouldBeEmpty();
        state.IdentityRecords.ShouldBeEmpty();
    }

    private static CommandSubmissionResponse PreparedOutcome(ChatBotGatewayContext context, DateTimeOffset acceptedAt)
        => new()
        {
            CommandId = context.Submission.Request.CommandId,
            CorrelationId = context.Submission.CorrelationId,
            TaskId = context.Submission.TaskId,
            OperationId = context.Submission.TaskId ?? context.Submission.Request.CommandId,
            LifecycleState = LifecycleState.Proposed,
            AcceptedAt = acceptedAt.ToUniversalTime(),
            ReasonCode = ChatBotMessageCode.Command_accepted,
            RetryEligible = false,
        };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SpecializedDifferentCallerShouldReconcileLaterValidQueueItemAndAcknowledgeOnlyThatItem(bool productionQueue)
    {
        FakeCoarseIdempotencyStateClient state = new();
        IAuditReplayIntentQueue queue = productionQueue ? new InMemoryAuditReplayIntentQueue() : new RecordingReplayIntentQueue();
        RecordingDispatcher originalDispatcher = new(onDispatch: () => state.ThrowOutcomeWrites = 100);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), AssociationDecisionCommand());
        ChatBotGatewayResult failed = await Gateway(originalDispatcher,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock()), replayQueue: queue,
            auditWriter: new RecordingAuditWriter { PostCommitResult = AuditWriteResult.Unavailable() },
            commandAllowlist: new ChatBotSpineCommandAllowlist()).SubmitAsync(submission, TestContext.Current.CancellationToken);
        failed.IsAccepted.ShouldBeFalse();
        AuditReplayIntent valid = queue.Snapshot().ShouldHaveSingleItem();
        AuditReplayIntent earlier = valid with
        {
            QueuedAt = valid.QueuedAt.AddMinutes(-1),
            IdentityKeyHash = productionQueue ? "unresolved-earlier-identity" : valid.IdentityKeyHash,
        };
        if (productionQueue)
        {
            await queue.AcknowledgeAsync(valid, TestContext.Current.CancellationToken);
            await queue.EnqueueAsync(earlier, TestContext.Current.CancellationToken);
            await queue.EnqueueAsync(valid, TestContext.Current.CancellationToken);
            queue.Snapshot().ShouldBe([earlier, valid]);
        }
        else
        {
            RecordingReplayIntentQueue recording = (RecordingReplayIntentQueue)queue;
            recording.Intents.Insert(0, earlier);
            recording.OnSnapshot = () => state.ThrowReads = 1;
        }
        state.ThrowOutcomeWrites = 0;
        RecordingDispatcher replacementDispatcher = new();
        ChatBotGatewayResult replay = await Gateway(replacementDispatcher,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, new FixedClock()), replayQueue: queue,
            commandAllowlist: new ChatBotSpineCommandAllowlist()).SubmitAsync(
                Submission(Principal(BoundTenant), AssociationDecisionCommand(), commandId: "01ARZ3NDEKTSV4RRFFQ69G5FBB"),
                TestContext.Current.CancellationToken);

        replay.IsAccepted.ShouldBeTrue();
        AssertPreparedOutcome(replay.Accepted!, valid.AcceptedOutcome!);
        replacementDispatcher.DispatchCount.ShouldBe(0);
        queue.Snapshot().ShouldHaveSingleItem().ShouldBe(earlier);
        queue.Snapshot().ShouldNotContain(valid);
        state.IdentityRecords.Count.ShouldBe(2);
        state.ReceiptRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SoleExpiredShadowReceiptMustRetainOriginalCallerOutcomeBeforeDeletion(bool failedDelete)
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        RecordingDispatcher dispatcher = new(onDispatch: () =>
        {
            state.RemoveDomain(state.DomainRecords.Single().CoarseKeyHash);
            state.RejectIdentityOutcomeSaves = 100;
        });
        ChatBotCommandSubmission original = Submission(Principal(BoundTenant), AssociationDecisionCommand());
        ChatBotGatewayResult first = await Gateway(dispatcher, clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock),
            commandAllowlist: new ChatBotSpineCommandAllowlist()).SubmitAsync(original, TestContext.Current.CancellationToken);
        first.IsAccepted.ShouldBeTrue();
        state.DomainRecords.ShouldBeEmpty();
        state.IdentityRecords.Single().PriorOutcome.ShouldBeNull();
        state.ReceiptRecords.Single().PriorOutcome.ShouldNotBeNull();
        clock.UtcNow += TimeSpan.FromHours(25);
        state.RejectIdentityOutcomeSaves = failedDelete ? 0 : 100;
        state.RejectDeletes = failedDelete ? 1 : 0;

        ChatBotGatewayContext other = DirectContext(AssociationDecisionCommand(), "01ARZ3NDEKTSV4RRFFQ69G5FBB");
        (await new DaprCoarseIdempotencyStore(state, clock).RecordAdmissionAsync(other, TestContext.Current.CancellationToken)).Kind
            .ShouldBe(CoarseIdempotencyDecisionKind.RecoveryPending);
        state.ReceiptRecords.ShouldHaveSingleItem().PriorOutcome.ShouldNotBeNull();
        state.RejectIdentityOutcomeSaves = 0;
        state.RejectDeletes = 0;
        (await new DaprCoarseIdempotencyStore(state, clock).RecordAdmissionAsync(other, TestContext.Current.CancellationToken)).Kind
            .ShouldBe(CoarseIdempotencyDecisionKind.Proceed);
        RecordingDispatcher replacementDispatcher = new();
        ChatBotGatewayResult replay = await Gateway(replacementDispatcher, clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock),
            commandAllowlist: new ChatBotSpineCommandAllowlist()).SubmitAsync(original, TestContext.Current.CancellationToken);
        replay.IsAccepted.ShouldBeTrue();
        AssertPreparedOutcome(replay.Accepted!, first.Accepted!);
        replacementDispatcher.DispatchCount.ShouldBe(0);
        state.IdentityRecords.Single(record => record.CommandId == original.Request.CommandId).PriorOutcome.ShouldNotBeNull();
    }

    private static void AssertPreparedOutcome(CommandSubmissionResponse actual, CommandSubmissionResponse expected)
    {
        actual.CommandId.ShouldBe(expected.CommandId);
        actual.CorrelationId.ShouldBe(expected.CorrelationId);
        actual.TaskId.ShouldBe(expected.TaskId);
        actual.OperationId.ShouldBe(expected.OperationId);
        actual.LifecycleState.ShouldBe(expected.LifecycleState);
        actual.AcceptedAt.ShouldBe(expected.AcceptedAt);
        actual.AcceptedAt.Offset.ShouldBe(TimeSpan.Zero);
        actual.ReasonCode.ShouldBe(expected.ReasonCode);
        actual.RetryEligible.ShouldBe(expected.RetryEligible);
    }
}
