using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Contracts.Commands;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Theory]
    [InlineData("conversation", "before-arrival")]
    [InlineData("conversation", "lost-sdk-ack")]
    [InlineData("conversation", "receipt-failure")]
    [InlineData("policy", "before-arrival")]
    [InlineData("policy", "lost-sdk-ack")]
    [InlineData("policy", "receipt-failure")]
    public async Task SameTargetLegacyStatusMustRequireObservedSdkAcceptanceOfThePreparedOwner(string operation, string failure)
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        DurableRecoveryEventStoreClient platform = new() { RetainCommittedEvidence = true };
        const string aggregate = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
        const string commandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
        object firstCommand = SameTargetLegacyCommand(operation, aggregate, "original");
        object changedCommand = SameTargetLegacyCommand(operation, aggregate, "changed");
        ChatBotGatewayContext firstContext = DirectContext(firstCommand, commandId);
        CoarseIdempotencyRecord current = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(firstContext, clock.UtcNow);
        CoarseIdempotencyRecord legacy = current with
        {
            CoarseKeyHash = current.LegacyKeyHash!, IdentityKeyHash = null, CallerFingerprint = null,
            LegacyKeyHash = null, PriorOutcome = PreparedOutcome(firstContext, clock.UtcNow),
        };
        state.SeedDomain(legacy);
        _ = await platform.SubmitCommandAsync(new SubmitCommandRequest(commandId, BoundTenant, ChatBotEventStore.DomainName,
            aggregate, firstCommand.GetType().Name, JsonSerializer.SerializeToElement(firstCommand), firstContext.Submission.CorrelationId),
            TestContext.Current.CancellationToken);
        CommandStatusQueryResponse committedA = platform.Evidence.ShouldNotBeNull();
        state.IdentityRecords.ShouldBeEmpty();

        // B has the same SDK identity and aggregate but a different body. Only an actual successful SDK response
        // may supply the new local confirmation; A's retained status alone cannot say whether B even arrived.
        platform.SubmissionFailure = failure == "before-arrival" ? new HttpRequestException("B never reached EventStore.") : null;
        platform.SubmissionUncertain = failure == "lost-sdk-ack";
        platform.OnReceivedSubmission = failure == "receipt-failure" ? _ => state.ThrowOutcomeWrites = 100 : null;
        ChatBotCommandSubmission submission = Submission(operation == "policy" ? AdminPrincipal("policy-admin") : Principal(BoundTenant, new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, aggregate)), changedCommand, commandId: commandId);
        ChatBotGatewayResult first = await Gateway(new AcceptedCommandDispatcher(platform, null!, null!, clock), clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform),
            auditWriter: new RecordingAuditWriter { PostCommitResult = AuditWriteResult.Unavailable() })
            .SubmitAsync(submission, TestContext.Current.CancellationToken);
        first.IsAccepted.ShouldBeFalse();
        first.Problem.ShouldNotBeNull().Status.ShouldBe(503);
        CoarseCommandIdentityRecord identity = state.IdentityRecords.ShouldHaveSingleItem();
        CoarseIdempotencyRecord preparedReservation = identity.DomainReservation.ShouldNotBeNull();
        preparedReservation.PreparedAggregateId.ShouldBe(aggregate);
        preparedReservation.SdkSubmissionAccepted.ShouldBe(failure == "receipt-failure");
        CommandSubmissionResponse prepared = preparedReservation.PreparedOutcome.ShouldNotBeNull();
        identity.PriorOutcome.ShouldBeNull();
        platform.SubmissionCount.ShouldBe(2);
        platform.ReceivedSubmissionCount.ShouldBe(failure == "before-arrival" ? 1 : 2);
        platform.SubmittedRequests.Select(static request => request.MessageId).Distinct().ShouldHaveSingleItem();
        platform.SubmittedRequests.Select(static request => request.Tenant).Distinct().ShouldHaveSingleItem();
        platform.SubmittedRequests.Select(static request => request.CorrelationId).Distinct().ShouldHaveSingleItem();
        platform.SubmittedRequests.Select(static request => request.AggregateId).Distinct().ShouldHaveSingleItem();
        platform.SubmittedRequests[0].Payload.GetRawText().ShouldNotBe(platform.SubmittedRequests[1].Payload.GetRawText());
        if (failure == "before-arrival")
        {
            platform.Evidence.ShouldBeSameAs(committedA);
        }

        // Every application service is replaced. Only durable coarse state and the platform's retained evidence survive.
        state.ThrowOutcomeWrites = 0;
        clock.UtcNow += TimeSpan.FromHours(25);
        RecordingDispatcher replacementDispatcher = new();
        ChatBotGatewayResult replay = await Gateway(replacementDispatcher, clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform))
            .SubmitAsync(submission, TestContext.Current.CancellationToken);
        replacementDispatcher.DispatchCount.ShouldBe(0);
        platform.SubmissionCount.ShouldBe(2);
        state.DomainRecords.Single(record => record.CoarseKeyHash == legacy.CoarseKeyHash).ShouldBe(legacy);
        CoarseCommandIdentityRecord retained = state.IdentityRecords.ShouldHaveSingleItem();
        retained.DomainReservation!.ReservationId.ShouldBe(preparedReservation.ReservationId);
        retained.DomainReservation.PreparedAggregateId.ShouldBe(aggregate);
        retained.DomainReservation.SdkSubmissionAccepted.ShouldBe(failure == "receipt-failure");
        replay.IsAccepted.ShouldBe(failure == "receipt-failure");
        if (replay.IsAccepted)
        {
            AssertPreparedOutcome(replay.Accepted!, prepared);
            AssertPreparedOutcome(retained.PriorOutcome!, prepared);
            AssertPreparedOutcome(state.DomainRecords.Single(record => record.CoarseKeyHash == identity.DomainKeyHash).PriorOutcome!, prepared);
        }
        else
        {
            replay.Problem.ShouldNotBeNull().Status.ShouldBe(503);
            replay.Problem.Retryable.ShouldBeTrue();
            replay.Problem.SchemaVersion.ShouldBe(ChatBotMessageCatalogVersion.Current);
            retained.PriorOutcome.ShouldBeNull();
            state.DomainRecords.Single(record => record.CoarseKeyHash == identity.DomainKeyHash).PriorOutcome.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpiredGenericLegacyUnknownMustRemainPendingAcrossOriginalAndReplacementStores(bool sameCaller)
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        ChatBotGatewayContext context = DirectContext(new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyRecord current = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(context, clock.UtcNow);
        CoarseIdempotencyRecord legacy = current with
        {
            CoarseKeyHash = current.LegacyKeyHash!, IdentityKeyHash = null, CallerFingerprint = null, LegacyKeyHash = null,
        };
        state.SeedDomain(legacy);
        DaprCoarseIdempotencyStore original = new(state, clock);
        clock.UtcNow = legacy.ExpiresAt.AddTicks(1);
        ChatBotGatewayContext retry = sameCaller ? context : DirectContext(new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), "01ARZ3NDEKTSV4RRFFQ69G5FBB");
        (await original.RecordAdmissionAsync(retry, TestContext.Current.CancellationToken)).Kind.ShouldBe(CoarseIdempotencyDecisionKind.RecoveryPending);
        clock.UtcNow += TimeSpan.FromHours(25);
        (await new DaprCoarseIdempotencyStore(state, clock).RecordAdmissionAsync(retry, TestContext.Current.CancellationToken)).Kind
            .ShouldBe(CoarseIdempotencyDecisionKind.RecoveryPending);
        state.DomainRecords.ShouldHaveSingleItem().ShouldBe(legacy);
        state.IdentityRecords.ShouldBeEmpty();
        state.ReceiptRecords.ShouldBeEmpty();
        state.PhysicalDeletes.ShouldBe(0);
    }

    [Fact]
    public async Task CommittedLegacyGenericExpiryMustStillPermitFreshCallerOwnership()
    {
        FakeCoarseIdempotencyStateClient state = new();
        MutableClock clock = new(FixedClock.FixedUtcNow);
        ChatBotGatewayContext context = DirectContext(new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyRecord current = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(context, clock.UtcNow);
        CoarseIdempotencyRecord legacy = current with
        {
            CoarseKeyHash = current.LegacyKeyHash!, IdentityKeyHash = null, CallerFingerprint = null,
            LegacyKeyHash = null, PriorOutcome = PreparedOutcome(context, clock.UtcNow),
        };
        state.SeedDomain(legacy);
        clock.UtcNow = legacy.ExpiresAt.AddTicks(1);
        ChatBotGatewayContext retry = DirectContext(new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), "01ARZ3NDEKTSV4RRFFQ69G5FBB");
        CoarseIdempotencyDecision owner = await new DaprCoarseIdempotencyStore(state, clock).RecordAdmissionAsync(retry, TestContext.Current.CancellationToken);
        owner.Kind.ShouldBe(CoarseIdempotencyDecisionKind.Proceed);
        state.DomainRecords.Single(record => record.CoarseKeyHash == legacy.CoarseKeyHash).ShouldBe(legacy);
        state.IdentityRecords.ShouldHaveSingleItem().DomainReservation!.ReservationId.ShouldBe(owner.Metadata.ReservationId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SdkConfirmationMustMatchThePreparedResponseActualTargetAndCurrentOwner(bool durable)
    {
        FakeCoarseIdempotencyStateClient state = new();
        FixedClock clock = new();
        IIdempotencyStore store = durable ? new DaprCoarseIdempotencyStore(state, clock) : new InMemoryCoarseIdempotencyStore(clock);
        const string aggregate = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
        ChatBotGatewayContext context = DirectContext(new RecordGovernedNote(aggregate), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyDecision owner = await store.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        CommandSubmissionResponse prepared = PreparedOutcome(context, clock.UtcNow);
        (await store.ConfirmSdkSubmissionAcceptedAsync(owner.Metadata, prepared, aggregate, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await store.PrepareDispatchAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await store.ConfirmSdkSubmissionAcceptedAsync(owner.Metadata, prepared, aggregate, TestContext.Current.CancellationToken)).ShouldBeFalse();
        context.SetPreparedAcceptedAt(prepared.AcceptedAt);
        context.SetDispatchTargetBinding((target, token) => store.BindDispatchTargetAsync(owner.Metadata, prepared, target, token));
        DurableRecoveryEventStoreClient platform = new();
        _ = await new AcceptedCommandDispatcher(platform, null!, null!, clock).DispatchAsync(context, TestContext.Current.CancellationToken);
        context.SdkSubmissionAccepted.ShouldBeTrue();
        platform.ReceivedSubmissionCount.ShouldBe(1);
        (await store.ConfirmSdkSubmissionAcceptedAsync(owner.Metadata, prepared, "01ARZ3NDEKTSV4RRFFQ69G5FBB", TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await store.ConfirmSdkSubmissionAcceptedAsync(owner.Metadata, PreparedOutcome(context, clock.UtcNow.AddTicks(1)), aggregate, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await store.ConfirmSdkSubmissionAcceptedAsync(owner.Metadata with { ReservationId = "stale-owner" }, prepared, aggregate, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await store.ConfirmSdkSubmissionAcceptedAsync(owner.Metadata, prepared, aggregate, TestContext.Current.CancellationToken)).ShouldBeTrue();
        CoarseIdempotencyRecord confirmed = durable ? state.IdentityRecords.Single().DomainReservation! : ((InMemoryCoarseIdempotencyStore)store).Records.Single();
        confirmed.SdkSubmissionAccepted.ShouldBeTrue();
        AssertPreparedOutcome(confirmed.PreparedOutcome!, prepared);
        if (durable)
        {
            await Should.ThrowAsync<InvalidOperationException>(() => store.AbortUndispatchedAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken).AsTask());
        }
        else
        {
            await store.AbortUndispatchedAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken);
            ((InMemoryCoarseIdempotencyStore)store).Records.ShouldHaveSingleItem().ShouldBe(confirmed);
        }
        await store.RecordOutcomeAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken);
        (await store.ConfirmSdkSubmissionAcceptedAsync(owner.Metadata, prepared, aggregate, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("false-cas")]
    [InlineData("lost-write-ack")]
    public async Task ConfirmationPersistenceFailureMustUseTheExistingReceiptBoundaryAndRetainItsDurableProof(string failure)
    {
        FakeCoarseIdempotencyStateClient state = new()
        {
            RejectSdkConfirmations = failure == "false-cas" ? 1 : 0,
            ThrowAfterSdkConfirmationSave = failure == "lost-write-ack",
        };
        MutableClock clock = new(FixedClock.FixedUtcNow);
        DurableRecoveryEventStoreClient platform = new() { RetainCommittedEvidence = true };
        RecordingReplayIntentQueue queue = new();
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"));
        ChatBotGatewayResult failed = await Gateway(new AcceptedCommandDispatcher(platform, null!, null!, clock), clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform), replayQueue: queue)
            .SubmitAsync(submission, TestContext.Current.CancellationToken);
        failed.IsAccepted.ShouldBeFalse();
        failed.Problem.ShouldNotBeNull().Status.ShouldBe(503);
        failed.Problem.Retryable.ShouldBeTrue();
        queue.Intents.ShouldHaveSingleItem().Kind.ShouldBe(AuditReplayIntentKind.PostCommitAuditReconciliation);
        CoarseCommandIdentityRecord identity = state.IdentityRecords.ShouldHaveSingleItem();
        identity.DomainReservation!.SdkSubmissionAccepted.ShouldBe(failure == "lost-write-ack");
        identity.PriorOutcome.ShouldBeNull();
        state.DomainRecords.ShouldHaveSingleItem().PriorOutcome.ShouldBeNull();
        platform.ReceivedSubmissionCount.ShouldBe(1);

        clock.UtcNow += TimeSpan.FromHours(25);
        RecordingDispatcher replacementDispatcher = new();
        ChatBotGatewayResult replay = await Gateway(replacementDispatcher, clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform))
            .SubmitAsync(submission, TestContext.Current.CancellationToken);
        replacementDispatcher.DispatchCount.ShouldBe(0);
        platform.SubmissionCount.ShouldBe(1);
        replay.IsAccepted.ShouldBe(failure == "lost-write-ack");
        if (replay.IsAccepted)
        {
            AssertPreparedOutcome(replay.Accepted!, identity.DomainReservation.PreparedOutcome!);
        }
        else
        {
            replay.Problem.ShouldNotBeNull().Status.ShouldBe(503);
            replay.Problem.Retryable.ShouldBeTrue();
            state.IdentityRecords.Single().PriorOutcome.ShouldBeNull();
            state.DomainRecords.Single().PriorOutcome.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PausedSdkConfirmationMustLoseItsCasWhenThePreparedOwnerChanges(bool productionClient)
    {
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        FakeCoarseIdempotencyStateClient fake = new() { OwnershipWriteEntered = entered, OwnershipWriteRelease = release };
        using ConditionalDaprStateClient backend = new() { OwnershipWriteEntered = entered, OwnershipWriteRelease = release };
        ICoarseIdempotencyStateClient state = productionClient ? new DaprCoarseIdempotencyStateClient(backend) : fake;
        FixedClock clock = new();
        DurableRecoveryEventStoreClient platform = new() { RetainCommittedEvidence = true };
        fake.BlockNextWriteKind = backend.BlockNextWriteKind = "sdk-confirmation";
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"));
        Task<ChatBotGatewayResult> oldSubmission = Task.Run(() => Gateway(new AcceptedCommandDispatcher(platform, null!, null!, clock), clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform)).SubmitAsync(submission, TestContext.Current.CancellationToken).AsTask(),
            TestContext.Current.CancellationToken);
        try
        {
            entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
            ChatBotGatewayContext context = DirectContext(new RecordGovernedNote("01ARZ3NDEKTSV4RRFFQ69G5FAZ"), submission.Request.CommandId);
            string key = CoarseIdempotencyComposer.ComposeCommandExecutionRecord(context, clock.UtcNow).IdentityKeyHash!;
            (CoarseCommandIdentityRecord? current, string etag) = await state.ReadIdentityAsync(key, TestContext.Current.CancellationToken);
            CoarseCommandIdentityRecord replacement = current! with
            {
                DomainReservation = current!.DomainReservation! with { ReservationId = "replacement-owner" },
            };
            // Controlled ownership replacement increments the retained key's ETag. It neither releases the unproven
            // command nor submits another effect; it tests a paused confirmation's stale-CAS boundary directly.
            (await state.TrySaveIdentityAsync(key, replacement, etag, TestContext.Current.CancellationToken)).ShouldBeTrue();
            release.Set();
            ChatBotGatewayResult failed = await oldSubmission.ConfigureAwait(true);
            failed.IsAccepted.ShouldBeFalse();
            failed.Problem.ShouldNotBeNull().Status.ShouldBe(503);
            (CoarseCommandIdentityRecord? retained, _) = await new DaprCoarseIdempotencyStore(state, clock).StateClient.ReadIdentityAsync(key, TestContext.Current.CancellationToken);
            retained!.DomainReservation!.ReservationId.ShouldBe("replacement-owner");
            retained.DomainReservation.SdkSubmissionAccepted.ShouldBeFalse();
            retained.PriorOutcome.ShouldBeNull();
            AssertPreparedOutcome(retained.DomainReservation.PreparedOutcome!, replacement.DomainReservation!.PreparedOutcome!);
            platform.SubmissionCount.ShouldBe(1);
            (fake.PhysicalDeletes + backend.PhysicalDeletes).ShouldBe(0);
        }
        finally
        {
            release.Set();
        }
    }

    [Fact]
    public async Task FailedConfirmationMustNotPrecedeRequiredWorkflowStartup()
    {
        FakeCoarseIdempotencyStateClient state = new();
        FixedClock clock = new();
        DurableRecoveryEventStoreClient platform = new() { RetainCommittedEvidence = true };
        SdkConfirmationWorkflowCoordinator coordinator = new(() =>
        {
            platform.ReceivedSubmissionCount.ShouldBe(1);
            state.IdentityRecords.Single().DomainReservation!.SdkSubmissionAccepted.ShouldBeFalse();
            state.RejectSdkConfirmations = 1;
        });
        ChatBotGatewayResult failed = await Gateway(new AcceptedCommandDispatcher(platform, null!, null!, clock, correctionPropagation: coordinator), clock: clock,
            idempotencyStore: new DaprCoarseIdempotencyStore(state, clock, eventStore: platform))
            .SubmitAsync(Submission(Principal(BoundTenant), AssociationCorrectionCommand()), TestContext.Current.CancellationToken);
        failed.IsAccepted.ShouldBeFalse();
        failed.Problem.ShouldNotBeNull().Status.ShouldBe(503);
        coordinator.Starts.ShouldBe(1);
        state.IdentityRecords.Single().DomainReservation!.SdkSubmissionAccepted.ShouldBeFalse();
        state.IdentityRecords.Single().PriorOutcome.ShouldBeNull();
    }

    private static object SameTargetLegacyCommand(string operation, string aggregate, string body)
        => operation == "conversation"
            ? new RecordProjectConversationMessage(aggregate, body == "original" ? "01ARZ3NDEKTSV4RRFFQ69G5FBB" : "01ARZ3NDEKTSV4RRFFQ69G5FBC",
                "sha256:" + body, 10, "en", 0, "01ARZ3NDEKTSV4RRFFQ69G5FAW")
            : new Hexalith.ChatBot.Contracts.Commands.ApproveTenantPolicyChange(aggregate, "pending-snapshot", "snapshot-" + body, 1, ["association.t-high"],
                "test", "requester", "approver", "tenant-policy-schema.m0.v1", "01ARZ3NDEKTSV4RRFFQ69G5FAW");

}
