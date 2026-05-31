using System.Net;
using System.Net.Http.Json;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class GovernedOperationProjectionTests
{
    private const string Tenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    private static readonly DateTimeOffset RecordedAt = new(2026, 5, 31, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleNewNotificationShouldProjectDerivedRecordShape()
    {
        InMemoryGovernedOperationProjectionStore store = new();
        FixedClock clock = new();
        GovernedOperationProjectionHandler handler = new(store, clock);

        GovernedOperationProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(
            Notification(sourceVersion: 1),
            TestContext.Current.CancellationToken);

        outcome.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Applied);
        GovernedOperationView view = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.TenantId.ShouldBe(Tenant);
        view.NoteId.ShouldBe(NoteId);
        view.SchemaVersion.ShouldBe(GovernedOperationView.CurrentSchemaVersion);
        view.SourceProvenance.ShouldBe(GovernedOperationView.GovernedCommandProvenance);
        view.DerivationKernelVersion.ShouldBe(GovernedOperationView.CurrentDerivationKernelVersion);
        view.RedactionState.ShouldBe(GovernedOperationView.MetadataOnlyRedactionState);
        view.RetentionClass.ShouldBe(GovernedOperationView.GovernedOperationalRetentionClass);
        view.SourceVersion.ShouldBe(1);
        view.RecordedAt.ShouldBe(RecordedAt);
        view.LastUpdatedAt.ShouldBe(FixedClock.FixedUtcNow);
    }

    [Fact]
    public async Task DuplicateNotificationShouldBeIgnoredLeavingExactlyOneDurableEffect()
    {
        InMemoryGovernedOperationProjectionStore store = new();
        GovernedOperationProjectionHandler handler = new(store, new FixedClock());

        GovernedOperationProjectionHandler.ProjectionOutcome first = await handler.HandleAsync(Notification(1), TestContext.Current.CancellationToken);
        GovernedOperationView afterFirst = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        GovernedOperationProjectionHandler.ProjectionOutcome replay = await handler.HandleAsync(Notification(1), TestContext.Current.CancellationToken);
        GovernedOperationView afterReplay = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        first.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Applied);
        replay.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Ignored);
        afterReplay.ShouldBe(afterFirst);
    }

    [Fact]
    public async Task OutOfOrderNotificationShouldBeDroppedAndHigherVersionShouldAdvanceWhilePreservingRecordedAt()
    {
        InMemoryGovernedOperationProjectionStore store = new();
        GovernedOperationProjectionHandler handler = new(store, new FixedClock());

        _ = await handler.HandleAsync(Notification(2), TestContext.Current.CancellationToken);
        GovernedOperationView afterTwo = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        // Stale (lower version) → dropped, last-writer-wins by source version.
        GovernedOperationProjectionHandler.ProjectionOutcome stale = await handler.HandleAsync(
            Notification(1, recordedAt: RecordedAt.AddMinutes(-5)),
            TestContext.Current.CancellationToken);
        GovernedOperationView afterStale = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        // Newer version → advances, RecordedAt preserved from the first applied event.
        GovernedOperationProjectionHandler.ProjectionOutcome newer = await handler.HandleAsync(
            Notification(3, recordedAt: RecordedAt.AddMinutes(5)),
            TestContext.Current.CancellationToken);
        GovernedOperationView afterNewer = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();

        stale.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Ignored);
        afterStale.ShouldBe(afterTwo);
        newer.ShouldBe(GovernedOperationProjectionHandler.ProjectionOutcome.Applied);
        afterNewer.SourceVersion.ShouldBe(3);
        afterNewer.RecordedAt.ShouldBe(afterTwo.RecordedAt);
    }

    [Fact]
    public async Task ProjectionShouldBeTenantPartitionedAndNeverLeakAcrossTenants()
    {
        InMemoryGovernedOperationProjectionStore store = new();
        GovernedOperationProjectionHandler handler = new(store, new FixedClock());

        _ = await handler.HandleAsync(Notification(1), TestContext.Current.CancellationToken);

        (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        (await store.GetAsync(OtherTenant, NoteId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ProjectionEndpointShouldApplyPublishedEventAndStayIdempotentOnReplay()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage first = await client
            .PostAsJsonAsync(
                GovernedOperationProjectionEndpoints.GovernedNoteRecordedRoute,
                PublishedEvent(1),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using HttpResponseMessage replay = await client
            .PostAsJsonAsync(
                GovernedOperationProjectionEndpoints.GovernedNoteRecordedRoute,
                PublishedEvent(1),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        replay.StatusCode.ShouldBe(HttpStatusCode.OK);

        IGovernedOperationProjectionStore store = factory.Services.GetRequiredService<IGovernedOperationProjectionStore>();
        GovernedOperationView view = (await store.GetAsync(Tenant, NoteId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.SourceVersion.ShouldBe(1);
        view.RedactionState.ShouldBe(GovernedOperationView.MetadataOnlyRedactionState);
    }

    [Fact]
    public static void TranslatorShouldDeriveTenantAndSourceVersionFromTheVerifiedEnvelopeOnly()
    {
        // M1/M2: tenant and source version come from the EventStore-stamped envelope, not a caller body. A
        // forged tenant on a separate field cannot exist — the envelope IS the verified source.
        GovernedNoteRecordedNotification notification =
            GovernedOperationProjectionTranslator.TryCreateNotification(PublishedEvent(sourceVersion: 7)).ShouldNotBeNull();

        notification.TenantId.ShouldBe(Tenant);
        notification.NoteId.ShouldBe(NoteId);
        notification.SourceVersion.ShouldBe(7);
        notification.CorrelationId.ShouldBe(CorrelationId);
    }

    [Fact]
    public static void TranslatorShouldIgnoreEventsFromAnotherDomainOrType()
    {
        // A foreign domain or an unrelated event type delivered on the topic is ignored (no projection),
        // and a missing tenant/aggregate or non-positive version is treated as malformed.
        GovernedOperationProjectionTranslator.TryCreateNotification(PublishedEvent(1) with { Domain = "folders" }).ShouldBeNull();
        GovernedOperationProjectionTranslator.TryCreateNotification(PublishedEvent(1) with { EventTypeName = "Some.Other.Event" }).ShouldBeNull();
        GovernedOperationProjectionTranslator.TryCreateNotification(PublishedEvent(1) with { TenantId = " " }).ShouldBeNull();
        GovernedOperationProjectionTranslator.TryCreateNotification(PublishedEvent(0)).ShouldBeNull();
        GovernedOperationProjectionTranslator.TryCreateNotification(null).ShouldBeNull();
    }

    private static GovernedNoteRecordedNotification Notification(long sourceVersion, DateTimeOffset? recordedAt = null)
        => new(Tenant, NoteId, MessageId, sourceVersion, recordedAt ?? RecordedAt, CorrelationId);

    private static PublishedGovernedOperationEvent PublishedEvent(long sourceVersion)
        => new(
            Tenant,
            "chatbot",
            NoteId,
            GovernedOperationProjectionTranslator.GovernedNoteRecordedEventType,
            sourceVersion,
            CorrelationId,
            MessageId,
            RecordedAt);

    private sealed class FixedClock : ISystemClock
    {
        public static DateTimeOffset FixedUtcNow { get; } = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);

        public DateTimeOffset UtcNow => FixedUtcNow;
    }
}
