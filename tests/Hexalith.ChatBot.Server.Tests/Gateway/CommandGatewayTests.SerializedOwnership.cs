using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

public sealed partial class CommandGatewayTests
{
    [Fact]
    public async Task ProductionStateAdapterMustRoundTripReservationPreparationAndReleaseAcrossFreshServices()
    {
        using ConditionalDaprStateClient backend = new();
        FixedClock clock = new();
        const string aggregateId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
        DaprCoarseIdempotencyStore admission = new(new DaprCoarseIdempotencyStateClient(backend), clock);
        ChatBotGatewayContext context = DirectContext(new RecordGovernedNote(aggregateId), "01ARZ3NDEKTSV4RRFFQ69G5FAY");
        CoarseIdempotencyDecision owner = await admission.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        DaprCoarseIdempotencyStateClient reservationReader = new(backend);
        (CoarseCommandIdentityRecord? reserved, _) = await reservationReader.ReadIdentityAsync(owner.Metadata.IdentityKeyHash!, TestContext.Current.CancellationToken).ConfigureAwait(true);
        reserved.ShouldNotBeNull().DomainReservation!.ReservationId.ShouldBe(owner.Metadata.ReservationId);
        reserved!.DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Reserved);
        reserved.DomainReservation.ReservationLeaseExpiresAt.ShouldBe(clock.UtcNow.Add(DaprCoarseIdempotencyStore.ReservationLease));
        reserved.DomainReservation.PreparedOutcome.ShouldBeNull();
        reserved.DomainReservation.SdkSubmissionAccepted.ShouldBeFalse();
        reserved.Released.ShouldBeFalse();

        CommandSubmissionResponse prepared = PreparedOutcome(context, clock.UtcNow);
        DaprCoarseIdempotencyStore preparing = new(new DaprCoarseIdempotencyStateClient(backend), new FixedClock());
        (await preparing.PrepareDispatchAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken)).ShouldBeTrue();
        using System.Text.Json.JsonDocument beforeBinding = System.Text.Json.JsonDocument.Parse(backend.StoredJson(owner.Metadata.IdentityKeyHash!));
        string preparedBytes = beforeBinding.RootElement.GetProperty("domainReservation").GetProperty("preparedOutcome").GetRawText();
        (await preparing.BindDispatchTargetAsync(owner.Metadata, prepared, aggregateId, TestContext.Current.CancellationToken)).ShouldBeTrue();
        using System.Text.Json.JsonDocument afterBinding = System.Text.Json.JsonDocument.Parse(backend.StoredJson(owner.Metadata.IdentityKeyHash!));
        afterBinding.RootElement.GetProperty("domainReservation").GetProperty("preparedOutcome").GetRawText().ShouldBe(preparedBytes);
        DaprCoarseIdempotencyStateClient preparedReader = new(backend);
        (CoarseCommandIdentityRecord? firstRead, _) = await preparedReader.ReadIdentityAsync(owner.Metadata.IdentityKeyHash!, TestContext.Current.CancellationToken).ConfigureAwait(true);
        firstRead.ShouldNotBeSameAs(reserved);
        firstRead!.DomainReservation!.DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
        firstRead.DomainReservation.PreparedAggregateId.ShouldBe(aggregateId);
        firstRead.DomainReservation.SdkSubmissionAccepted.ShouldBeFalse();
        AssertPreparedOutcome(firstRead.DomainReservation.PreparedOutcome!, prepared);
        // Mutating a returned CLR object must never mutate the persisted bytes.
        firstRead.DomainReservation.PreparedOutcome!.CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
        (CoarseCommandIdentityRecord? secondRead, _) = await new DaprCoarseIdempotencyStateClient(backend)
            .ReadIdentityAsync(owner.Metadata.IdentityKeyHash!, TestContext.Current.CancellationToken).ConfigureAwait(true);
        secondRead.ShouldNotBeSameAs(firstRead);
        secondRead!.DomainReservation!.PreparedOutcome.ShouldNotBeSameAs(firstRead.DomainReservation.PreparedOutcome);
        secondRead.DomainReservation.ReservationId.ShouldBe(owner.Metadata.ReservationId);
        secondRead.DomainReservation.DispatchState.ShouldBe(CoarseDispatchState.Dispatching);
        secondRead.DomainReservation.PreparedAggregateId.ShouldBe(aggregateId);
        AssertPreparedOutcome(secondRead.DomainReservation.PreparedOutcome!, prepared);

        DaprCoarseIdempotencyStore releasing = new(new DaprCoarseIdempotencyStateClient(backend), new FixedClock());
        await releasing.AbortUndispatchedAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken);
        DaprCoarseIdempotencyStateClient releasedReader = new(backend);
        (CoarseCommandIdentityRecord? released, _) = await releasedReader.ReadIdentityAsync(owner.Metadata.IdentityKeyHash!, TestContext.Current.CancellationToken).ConfigureAwait(true);
        (CoarseIdempotencyRecord? domain, _) = await releasedReader.ReadDomainAsync(owner.Metadata.CoarseKeyHash, TestContext.Current.CancellationToken).ConfigureAwait(true);
        released.ShouldNotBeNull().Released.ShouldBeTrue();
        released!.DomainReservation!.ReservationId.ShouldBe(owner.Metadata.ReservationId);
        released.DomainReservation.DispatchState.ShouldBe(CoarseDispatchState.Aborted);
        released.DomainReservation.PreparedAggregateId.ShouldBe(aggregateId);
        AssertPreparedOutcome(released.DomainReservation.PreparedOutcome!, prepared);
        domain.ShouldNotBeNull().Released.ShouldBeTrue();
        domain!.ReservationId.ShouldBe(owner.Metadata.ReservationId);
        backend.PhysicalDeletes.ShouldBe(0);

        DaprCoarseIdempotencyStore replacement = new(new DaprCoarseIdempotencyStateClient(backend), new FixedClock());
        CoarseIdempotencyDecision current = await replacement.RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        current.Kind.ShouldBe(CoarseIdempotencyDecisionKind.Proceed);
        current.Metadata.ReservationId.ShouldNotBe(owner.Metadata.ReservationId);
        (await releasing.PrepareDispatchAsync(owner.Metadata, prepared, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await replacement.PrepareDispatchAsync(current.Metadata, prepared, TestContext.Current.CancellationToken)).ShouldBeTrue();
        context.SetDispatchTargetBinding((target, token) => replacement.BindDispatchTargetAsync(current.Metadata, prepared, target, token));
        DurableRecoveryEventStoreClient platform = new();
        _ = await new AcceptedCommandDispatcher(platform, null!, null!, clock).DispatchAsync(context, TestContext.Current.CancellationToken);
        context.SdkSubmissionAccepted.ShouldBeTrue();
        (await replacement.ConfirmSdkSubmissionAcceptedAsync(current.Metadata, prepared, aggregateId, TestContext.Current.CancellationToken)).ShouldBeTrue();
        using System.Text.Json.JsonDocument afterConfirmation = System.Text.Json.JsonDocument.Parse(backend.StoredJson(current.Metadata.IdentityKeyHash!));
        afterConfirmation.RootElement.GetProperty("domainReservation").GetProperty("sdkSubmissionAccepted").GetBoolean().ShouldBeTrue();
        afterConfirmation.RootElement.GetProperty("domainReservation").GetProperty("preparedOutcome").GetRawText().ShouldBe(preparedBytes);
        (CoarseCommandIdentityRecord? confirmed, _) = await new DaprCoarseIdempotencyStateClient(backend)
            .ReadIdentityAsync(current.Metadata.IdentityKeyHash!, TestContext.Current.CancellationToken);
        confirmed!.DomainReservation!.SdkSubmissionAccepted.ShouldBeTrue();
        confirmed.DomainReservation.ReservationId.ShouldBe(current.Metadata.ReservationId);
        await replacement.RecordOutcomeAsync(current.Metadata, prepared, TestContext.Current.CancellationToken);
        CoarseIdempotencyDecision replay = await new DaprCoarseIdempotencyStore(new DaprCoarseIdempotencyStateClient(backend), new FixedClock())
            .RecordAdmissionAsync(context, TestContext.Current.CancellationToken);
        replay.Kind.ShouldBe(CoarseIdempotencyDecisionKind.ReplayPriorOutcome);
        AssertPreparedOutcome(replay.PriorOutcome!, prepared);

        // Generated enums are persisted as their stable wire values, never as generation-dependent ordinals.
        using System.Text.Json.JsonDocument persisted = System.Text.Json.JsonDocument.Parse(backend.StoredJson(owner.Metadata.IdentityKeyHash!));
        System.Text.Json.JsonElement priorOutcome = persisted.RootElement.GetProperty("priorOutcome");
        priorOutcome.GetProperty("reasonCode").GetString().ShouldBe("command_accepted");
        priorOutcome.GetProperty("lifecycleState").GetString().ShouldBe(prepared.LifecycleState.ToString());
        System.Text.Json.JsonElement dispatchState = persisted.RootElement.GetProperty("domainReservation").GetProperty("dispatchState");
        persisted.RootElement.GetProperty("domainReservation").GetProperty("preparedAggregateId").GetString().ShouldBe(aggregateId);
        persisted.RootElement.GetProperty("domainReservation").GetProperty("sdkSubmissionAccepted").GetBoolean().ShouldBeTrue();
        dispatchState.ValueKind.ShouldBe(System.Text.Json.JsonValueKind.String);
        Enum.TryParse(dispatchState.GetString(), ignoreCase: false, out CoarseDispatchState _).ShouldBeTrue();
    }
}
