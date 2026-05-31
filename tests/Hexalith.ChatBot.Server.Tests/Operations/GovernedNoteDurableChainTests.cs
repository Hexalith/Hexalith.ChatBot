using System.Net;
using System.Net.Http.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Contracts.Results;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Operations;

/// <summary>
/// In-process proof of AC1's durable chain with no DAPR runtime: the Pattern-A aggregate's pure
/// <c>Handle</c> produces the recorded-note event; that event, shaped as the EventStore publishes it, is
/// delivered to the real projection subscriber endpoint on <see cref="WebApplicationFactory{TEntryPoint}"/>;
/// and the projection writes exactly one tenant-partitioned durable read-model record that stays idempotent on
/// at-least-once replay. This closes <c>execute → publish → project</c> end to end in process (the live DAPR
/// topology is exercised by the Tier-3 Aspire E2E).
/// </summary>
public sealed class GovernedNoteDurableChainTests
{
    private const string Tenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public static void AggregateHandleShouldEmitTheRecordedEventThenRejectAReRecordForFineIdempotency()
    {
        DomainResult recorded = GovernedOperationAggregate.Handle(new RecordGovernedNote(NoteId), state: null);

        recorded.IsSuccess.ShouldBeTrue();
        GovernedNoteRecorded recordedEvent = recorded.Events[0].ShouldBeOfType<GovernedNoteRecorded>();
        recordedEvent.NoteId.ShouldBe(NoteId);

        // Replay the produced event into state, then re-handle: a second record against an already-recorded
        // aggregate is a structured rejection (never a duplicate event, never a throw) so the fine-altitude
        // idempotency cache is honored and exactly one durable effect remains.
        GovernedOperationState state = new();
        state.Apply(recordedEvent);
        DomainResult reRecorded = GovernedOperationAggregate.Handle(new RecordGovernedNote(NoteId), state);
        reRecorded.IsRejection.ShouldBeTrue();
        reRecorded.Events[0].ShouldBeOfType<GovernedNoteAlreadyRecordedRejection>();
    }

    [Fact]
    public async Task PublishedRecordedEventShouldProjectExactlyOneTenantPartitionedViewIdempotently()
    {
        // The aggregate's success event feeds the projection, shaped exactly as the EventStore publishes it.
        DomainResult recorded = GovernedOperationAggregate.Handle(new RecordGovernedNote(NoteId), state: null);
        GovernedNoteRecorded recordedEvent = recorded.Events[0].ShouldBeOfType<GovernedNoteRecorded>();
        PublishedGovernedOperationEvent published = new(
            Tenant,
            ChatBotEventStore.DomainName,
            recordedEvent.NoteId,
            GovernedOperationProjectionTranslator.GovernedNoteRecordedEventType,
            SequenceNumber: 1,
            CorrelationId,
            MessageId,
            new DateTimeOffset(2026, 5, 31, 8, 0, 0, TimeSpan.Zero));

        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage delivered = await client
            .PostAsJsonAsync(GovernedOperationProjectionEndpoints.GovernedNoteRecordedRoute, published, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage redelivered = await client
            .PostAsJsonAsync(GovernedOperationProjectionEndpoints.GovernedNoteRecordedRoute, published, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        delivered.StatusCode.ShouldBe(HttpStatusCode.OK);
        redelivered.StatusCode.ShouldBe(HttpStatusCode.OK);

        IGovernedOperationProjectionStore store = factory.Services.GetRequiredService<IGovernedOperationProjectionStore>();

        // Exactly one durable read-model record at the tenant-partitioned key, derived-record shape intact.
        GovernedOperationView view = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.TenantId.ShouldBe(Tenant);
        view.NoteId.ShouldBe(NoteId);
        view.SourceVersion.ShouldBe(1);
        view.SourceProvenance.ShouldBe(GovernedOperationView.GovernedCommandProvenance);
        view.RedactionState.ShouldBe(GovernedOperationView.MetadataOnlyRedactionState);

        // Tenant isolation by construction: the read model never leaks to another tenant's partition.
        (await store.GetAsync(OtherTenant, NoteId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }
}
