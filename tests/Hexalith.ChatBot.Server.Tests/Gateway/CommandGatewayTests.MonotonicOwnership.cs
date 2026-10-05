using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PausedPreparationMustLoseItsCasAfterLogicalReleaseAndReplacement(bool productionClient)
    {
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        FakeCoarseIdempotencyStateClient fake = new() { OwnershipWriteEntered = entered, OwnershipWriteRelease = release };
        using ConditionalDaprStateClient production = new() { OwnershipWriteEntered = entered, OwnershipWriteRelease = release };
        ICoarseIdempotencyStateClient state = productionClient ? new DaprCoarseIdempotencyStateClient(production) : fake;
        MutableClock clock = new(FixedClock.FixedUtcNow);
        ChatBotGatewayContext context = DirectContext(new TenantScopedCommand(BoundTenant, "before"), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        DaprCoarseIdempotencyStore oldStore = new(state, clock);
        CoarseIdempotencyDecision oldOwner = await oldStore.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        CommandSubmissionResponse oldOutcome = PreparedOutcome(context, clock.UtcNow);
        fake.BlockNextWriteKind = production.BlockNextWriteKind = "prepare";
        Task<bool> preparing = Task.Run(async () => await oldStore.PrepareDispatchAsync(oldOwner.Metadata, oldOutcome, TestContext.Current.CancellationToken).ConfigureAwait(true), TestContext.Current.CancellationToken);
        try
        {
            entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
            clock.UtcNow = clock.UtcNow.Add(DaprCoarseIdempotencyStore.ReservationLease).AddTicks(1);
            DaprCoarseIdempotencyStore replacement = new(state, clock);
            CoarseIdempotencyDecision currentOwner = await replacement.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
            currentOwner.Kind.ShouldBe(CoarseIdempotencyDecisionKind.Proceed);
            currentOwner.Metadata.ReservationId.ShouldNotBe(oldOwner.Metadata.ReservationId);
            release.Set();
            (await preparing.ConfigureAwait(true)).ShouldBeFalse();
            CommandSubmissionResponse currentOutcome = PreparedOutcome(context, clock.UtcNow);
            (await replacement.PrepareDispatchAsync(currentOwner.Metadata, currentOutcome, TestContext.Current.CancellationToken)).ShouldBeTrue();
            await replacement.RecordOutcomeAsync(currentOwner.Metadata, currentOutcome, TestContext.Current.CancellationToken);
            await Should.ThrowAsync<InvalidOperationException>(() => oldStore.RecordOutcomeAsync(oldOwner.Metadata, PreparedOutcome(context, FixedClock.FixedUtcNow), TestContext.Current.CancellationToken).AsTask());
            await AssertCurrentDurableOwnerAndReplayAsync(state, clock, context, currentOwner, currentOutcome).ConfigureAwait(true);
            (fake.PhysicalDeletes + production.PhysicalDeletes).ShouldBe(0);
        }
        finally { release.Set(); }
    }

    [Theory]
    [InlineData("identity-release", false, false)]
    [InlineData("identity-release", false, true)]
    [InlineData("domain-release", false, false)]
    [InlineData("domain-release", false, true)]
    [InlineData("domain-release", true, false)]
    [InlineData("domain-release", true, true)]
    public async Task PausedCleanupMustNotRemoveOrOverwriteAReplacement(string writeKind, bool specialized, bool productionClient)
    {
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        FakeCoarseIdempotencyStateClient fake = new() { OwnershipWriteEntered = entered, OwnershipWriteRelease = release };
        using ConditionalDaprStateClient production = new() { OwnershipWriteEntered = entered, OwnershipWriteRelease = release };
        ICoarseIdempotencyStateClient state = productionClient ? new DaprCoarseIdempotencyStateClient(production) : fake;
        MutableClock clock = new(FixedClock.FixedUtcNow);
        ChatBotGatewayContext context = DirectContext(specialized ? AssociationDecisionCommand() : new TenantScopedCommand(BoundTenant, "before"), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        DaprCoarseIdempotencyStore oldStore = new(state, clock);
        CoarseIdempotencyDecision oldOwner = await oldStore.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        fake.BlockNextWriteKind = production.BlockNextWriteKind = writeKind;
        Task cleanup = Task.Run(async () => await oldStore.AbortAdmissionAsync(oldOwner.Metadata, TestContext.Current.CancellationToken).ConfigureAwait(true), TestContext.Current.CancellationToken);
        try
        {
            entered.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ShouldBeTrue();
            clock.UtcNow = clock.UtcNow.Add(DaprCoarseIdempotencyStore.ReservationLease).AddTicks(1);
            DaprCoarseIdempotencyStore replacement = new(state, clock);
            CoarseIdempotencyDecision currentOwner = await replacement.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
            currentOwner.Kind.ShouldBe(CoarseIdempotencyDecisionKind.Proceed);
            CommandSubmissionResponse currentOutcome = PreparedOutcome(context, clock.UtcNow);
            (await replacement.PrepareDispatchAsync(currentOwner.Metadata, currentOutcome, TestContext.Current.CancellationToken)).ShouldBeTrue();
            await replacement.RecordOutcomeAsync(currentOwner.Metadata, currentOutcome, TestContext.Current.CancellationToken);
            release.Set();
            await cleanup.ConfigureAwait(true);
            (await oldStore.PrepareDispatchAsync(oldOwner.Metadata, PreparedOutcome(context, clock.UtcNow), TestContext.Current.CancellationToken)).ShouldBeFalse();
            await AssertCurrentDurableOwnerAndReplayAsync(state, clock, context, currentOwner, currentOutcome).ConfigureAwait(true);
            (fake.PhysicalDeletes + production.PhysicalDeletes).ShouldBe(0);
        }
        finally { release.Set(); }
    }

    private static async Task AssertCurrentDurableOwnerAndReplayAsync(ICoarseIdempotencyStateClient state, MutableClock clock,
        ChatBotGatewayContext context, CoarseIdempotencyDecision owner, CommandSubmissionResponse outcome)
    {
        (CoarseCommandIdentityRecord? identity, _) = await state.ReadIdentityAsync(owner.Metadata.IdentityKeyHash!, TestContext.Current.CancellationToken).ConfigureAwait(true);
        (CoarseIdempotencyRecord? domain, _) = await state.ReadDomainAsync(owner.Metadata.CoarseKeyHash, TestContext.Current.CancellationToken).ConfigureAwait(true);
        identity.ShouldNotBeNull().Released.ShouldBeFalse();
        domain.ShouldNotBeNull().Released.ShouldBeFalse();
        identity!.DomainReservation!.ReservationId.ShouldBe(owner.Metadata.ReservationId);
        domain!.ReservationId.ShouldBe(owner.Metadata.ReservationId);
        AssertPreparedOutcome(identity.PriorOutcome!, outcome);
        AssertPreparedOutcome(domain.PriorOutcome!, outcome);
        RecordingDispatcher dispatcher = new();
        CommandGateway newServices = Gateway(dispatcher, clock: clock, idempotencyStore: new DaprCoarseIdempotencyStore(state, clock));
        ChatBotGatewayResult replay = await newServices.SubmitAsync(context.Submission, TestContext.Current.CancellationToken).ConfigureAwait(true);
        replay.IsAccepted.ShouldBeTrue();
        AssertPreparedOutcome(replay.Accepted!, outcome);
        PriorCommandOutcome prior = replay.Accepted!.PriorOutcome.ShouldNotBeNull();
        prior.CommandId.ShouldBe(outcome.CommandId);
        prior.OperationId.ShouldBe(outcome.OperationId);
        prior.CorrelationId.ShouldBe(outcome.CorrelationId);
        prior.TaskId.ShouldBe(outcome.TaskId);
        prior.AcceptedAt.ShouldBe(outcome.AcceptedAt);
        prior.LifecycleState.ShouldBe(outcome.LifecycleState);
        prior.ReasonCode.ShouldBe(outcome.ReasonCode);
        prior.RetryEligible.ShouldBe(outcome.RetryEligible);
        dispatcher.DispatchCount.ShouldBe(0);
    }

    [Fact]
    public async Task StoreWithoutPreparationImplementationMustFailClosedBeforeDispatch()
    {
        RecordingDispatcher dispatcher = new();
        IIdempotencyStore store = new UnpreparedIdempotencyStore();
        ChatBotGatewayResult result = await Gateway(dispatcher, idempotencyStore: store).SubmitAsync(
            Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "before")), TestContext.Current.CancellationToken);
        result.IsAccepted.ShouldBeFalse();
        result.Problem!.Status.ShouldBe(503);
        result.Problem.Retryable.ShouldBeTrue();
        dispatcher.DispatchCount.ShouldBe(0);
    }

    private sealed class UnpreparedIdempotencyStore : IIdempotencyStore
    {
        public ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            var metadata = CoarseIdempotencyMetadata.UnsafeCreateForTesting("command-execution", "unprepared", "equivalent", DateTimeOffset.MaxValue);
            context.SetIdempotency(metadata);
            return ValueTask.FromResult(CoarseIdempotencyDecision.Proceed(metadata));
        }
        public ValueTask RecordOutcomeAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}
