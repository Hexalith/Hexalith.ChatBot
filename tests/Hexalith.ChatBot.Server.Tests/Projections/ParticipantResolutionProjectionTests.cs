using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class ParticipantResolutionProjectionTests
{
    private const string Tenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string ResolutionId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string SourceMailboxId = "controlled-mailbox-001";
    private const string SourceParticipantId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset RecordedAt = new(2026, 5, 31, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandlerShouldProjectTenantPartitionedMetadataOnlyState()
    {
        InMemoryParticipantResolutionProjectionStore store = new();
        ParticipantResolutionProjectionHandler handler = new(store, new FixedClock());

        ParticipantResolutionProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(
            Notification(sourceVersion: 1),
            TestContext.Current.CancellationToken);

        outcome.ShouldBe(ParticipantResolutionProjectionHandler.ProjectionOutcome.Applied);
        ParticipantResolutionView view = (await store.GetAsync(Tenant, ResolutionId, SourceParticipantId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.TenantId.ShouldBe(Tenant);
        view.IntakeId.ShouldBe(IntakeId);
        view.SourceMailboxId.ShouldBe(SourceMailboxId);
        view.PartyId.ShouldBe("tenant-alpha:parties:party-001");
        view.Status.ShouldBe(ParticipantResolutionStatus.Resolved);
        view.RedactionState.ShouldBe(ParticipantResolutionView.MetadataOnlyRedactionState);
        view.CorrelationId.ShouldBe(CorrelationId);
        (await store.GetAsync(OtherTenant, ResolutionId, SourceParticipantId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task HandlerShouldIgnoreDuplicateOrStaleNotifications()
    {
        InMemoryParticipantResolutionProjectionStore store = new();
        ParticipantResolutionProjectionHandler handler = new(store, new FixedClock());

        _ = await handler.HandleAsync(Notification(2), TestContext.Current.CancellationToken);
        ParticipantResolutionProjectionHandler.ProjectionOutcome stale = await handler.HandleAsync(Notification(1), TestContext.Current.CancellationToken);

        stale.ShouldBe(ParticipantResolutionProjectionHandler.ProjectionOutcome.Ignored);
        ParticipantResolutionView view = (await store.GetAsync(Tenant, ResolutionId, SourceParticipantId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.SourceVersion.ShouldBe(2);
    }

    [Fact]
    public static void TranslatorShouldDeriveSafeNotificationFromVerifiedEnvelope()
    {
        ParticipantResolutionNotification notification =
            ParticipantResolutionProjectionTranslator.TryCreateNotification(PublishedResolved(3)).ShouldNotBeNull();

        notification.TenantId.ShouldBe(Tenant);
        notification.ResolutionId.ShouldBe(ResolutionId);
        notification.SourceMailboxId.ShouldBe(SourceMailboxId);
        notification.SourceVersion.ShouldBe(3);
        notification.Status.ShouldBe(ParticipantResolutionStatus.Resolved);
        notification.CorrelationId.ShouldBe(CorrelationId);

        ParticipantResolutionProjectionTranslator.TryCreateNotification(PublishedResolved(3) with { Domain = "folders" }).ShouldBeNull();
        ParticipantResolutionProjectionTranslator.TryCreateNotification(PublishedResolved(0)).ShouldBeNull();
    }

    private static ParticipantResolutionNotification Notification(long sourceVersion)
        => new(
            Tenant,
            ResolutionId,
            IntakeId,
            SourceMailboxId,
            SourceParticipantId,
            "tenant-alpha:parties:party-001",
            ParticipantResolutionStatus.Resolved,
            null,
            "mailbox:intake:sender",
            "evidence-sha256",
            sourceVersion,
            RecordedAt,
            CorrelationId);

    private static PublishedParticipantResolutionEvent PublishedResolved(long sourceVersion)
        => new(
            Tenant,
            "chatbot",
            ResolutionId,
            ParticipantResolutionProjectionTranslator.ResolvedEventType,
            sourceVersion,
            CorrelationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            RecordedAt,
            IntakeId,
            SourceMailboxId,
            SourceParticipantId,
            "tenant-alpha:parties:party-001",
            null,
            "mailbox:intake:sender",
            "evidence-sha256");

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);
    }
}
