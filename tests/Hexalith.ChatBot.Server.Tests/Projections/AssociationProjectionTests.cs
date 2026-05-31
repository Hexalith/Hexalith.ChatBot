using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class AssociationProjectionTests
{
    private const string Tenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset DetectedAt = new(2026, 5, 31, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandlerShouldProjectTenantPartitionedMetadataOnlyAssociationState()
    {
        InMemoryAssociationProjectionStore store = new();
        AssociationProjectionHandler handler = new(store, new FixedClock());

        AssociationProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(Notification(1), TestContext.Current.CancellationToken);

        outcome.ShouldBe(AssociationProjectionHandler.ProjectionOutcome.Applied);
        AssociationCandidateView view = (await store.GetAsync(Tenant, AssociationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.TenantId.ShouldBe(Tenant);
        view.IntakeId.ShouldBe(IntakeId);
        view.ProjectId.ShouldBe("project-001");
        view.LifecycleState.ShouldBe(LifecycleState.Associated);
        view.Outcome.ShouldBe(AssociationScoringOutcome.AutoAssociated);
        view.RedactionState.ShouldBe("metadata_only");
        view.CorrelationId.ShouldBe(CorrelationId);
        (await store.GetAsync(OtherTenant, AssociationId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task HandlerShouldIgnoreDuplicateOrStaleAssociationNotifications()
    {
        InMemoryAssociationProjectionStore store = new();
        AssociationProjectionHandler handler = new(store, new FixedClock());

        _ = await handler.HandleAsync(Notification(2), TestContext.Current.CancellationToken);
        AssociationProjectionHandler.ProjectionOutcome stale = await handler.HandleAsync(Notification(1), TestContext.Current.CancellationToken);

        stale.ShouldBe(AssociationProjectionHandler.ProjectionOutcome.Ignored);
        AssociationCandidateView view = (await store.GetAsync(Tenant, AssociationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.SourceVersion.ShouldBe(2);
    }

    [Fact]
    public static void TranslatorShouldDeriveSafeAssociationNotificationFromVerifiedEnvelope()
    {
        AssociationNotification notification = AssociationProjectionTranslator.TryCreateNotification(Published(3)).ShouldNotBeNull();

        notification.TenantId.ShouldBe(Tenant);
        notification.AssociationId.ShouldBe(AssociationId);
        notification.SourceMailboxId.ShouldBe("controlled-mailbox-001");
        notification.SourceVersion.ShouldBe(3);
        notification.LifecycleState.ShouldBe(LifecycleState.Associated);
        notification.Outcome.ShouldBe(AssociationScoringOutcome.AutoAssociated);
        notification.CorrelationId.ShouldBe(CorrelationId);

        AssociationProjectionTranslator.TryCreateNotification(Published(3) with { Domain = "folders" }).ShouldBeNull();
        AssociationProjectionTranslator.TryCreateNotification(Published(0)).ShouldBeNull();
    }

    [Fact]
    public async Task HandlerShouldProjectNeedsReviewAssociationContextWithoutUnsafePayload()
    {
        InMemoryAssociationProjectionStore store = new();
        AssociationProjectionHandler handler = new(store, new FixedClock());

        AssociationProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(
            Notification(4) with
            {
                LifecycleState = LifecycleState.NeedsReview,
                ProjectId = null,
                ProjectDisplayName = null,
                Outcome = AssociationScoringOutcome.CandidatesGenerated,
                ThresholdBand = AssociationThresholdBand.Ambiguous,
                ConfidenceScore = 0.75,
            },
            TestContext.Current.CancellationToken);

        outcome.ShouldBe(AssociationProjectionHandler.ProjectionOutcome.Applied);
        AssociationCandidateView view = (await store.GetAsync(Tenant, AssociationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.LifecycleState.ShouldBe(LifecycleState.NeedsReview);
        view.IntakeId.ShouldBe(IntakeId);
        view.SourceMailboxId.ShouldBe("controlled-mailbox-001");
        view.SourceConversationId.ShouldBe("conversation-001");
        view.SourceThreadId.ShouldBe("thread-001");
        view.Candidates.ShouldHaveSingleItem().ProjectId.ShouldBe("project-001");

        string serialized = System.Text.Json.JsonSerializer.Serialize(view);
        serialized.ShouldNotContain("sender@example.test", Case.Insensitive);
        serialized.ShouldNotContain("raw-body", Case.Insensitive);
    }

    [Fact]
    public static void TranslatorShouldPreserveExplicitNeedsReviewLifecycleFromRoutedEvents()
    {
        AssociationNotification notification = AssociationProjectionTranslator.TryCreateNotification(
            Published(5) with
            {
                EventTypeName = AssociationProjectionTranslator.CandidatesGeneratedEventType,
                LifecycleState = LifecycleState.NeedsReview,
                ProjectId = null,
                ProjectDisplayName = null,
                Outcome = AssociationScoringOutcome.CandidatesGenerated,
                ThresholdBand = AssociationThresholdBand.FailClosed,
                ConfidenceScore = 0.55,
            }).ShouldNotBeNull();

        notification.LifecycleState.ShouldBe(LifecycleState.NeedsReview);
        notification.ProjectId.ShouldBeNull();
        notification.ThresholdBand.ShouldBe(AssociationThresholdBand.FailClosed);
    }

    private static AssociationNotification Notification(long sourceVersion)
        => new(
            Tenant,
            AssociationId,
            IntakeId,
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            "project-001",
            "Project One",
            LifecycleState.Associated,
            AssociationScoringOutcome.AutoAssociated,
            AssociationThresholdBand.Auto,
            0.9,
            [Candidate()],
            [],
            "association-thresholds.m0.default.v1",
            "association-deterministic.kernel.m0.v1",
            "metadata_only",
            "collaboration_input",
            sourceVersion,
            DetectedAt,
            CorrelationId);

    private static PublishedAssociationEvent Published(long sourceVersion)
        => new(
            Tenant,
            "chatbot",
            AssociationId,
            AssociationProjectionTranslator.AutoAssociatedEventType,
            sourceVersion,
            CorrelationId,
            DetectedAt,
            IntakeId,
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            "project-001",
            "Project One",
            [Candidate()],
            [],
            0.9,
            AssociationThresholdBand.Auto,
            null,
            null,
            "association-thresholds.m0.default.v1",
            "association-deterministic.kernel.m0.v1",
            DetectedAt,
            "metadata_only",
            "collaboration_input");

    private static AssociationCandidate Candidate()
        => new(
            "project-001",
            "Project One",
            0.9,
            1,
            [AssociationReasonCode.ExplicitProjectIdentifierMatched],
            [],
            [],
            true);

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 5, 31, 10, 0, 0, TimeSpan.Zero);
    }
}
