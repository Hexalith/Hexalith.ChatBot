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

    [Fact]
    public async Task HandlerShouldProjectDecisionSnapshotWithoutReplacingEvidence()
    {
        InMemoryAssociationProjectionStore store = new();
        AssociationProjectionHandler handler = new(store, new FixedClock());

        AssociationNotification notification = AssociationProjectionTranslator.TryCreateNotification(
            Published(6) with
            {
                EventTypeName = AssociationProjectionTranslator.DecisionConfirmedEventType,
                DecisionKind = AssociationDecisionKind.Associate,
                ActorId = "actor-alpha",
                ActorType = "human",
                DecidedAt = new DateTimeOffset(2026, 5, 31, 9, 15, 0, TimeSpan.Zero),
                DecisionNote = "Reviewed safe metadata.",
                DecisionNoteRedactionState = "metadata_only",
                SurfaceOrigin = "ui",
                PolicySnapshotVersion = "association-thresholds.m0.default.v1",
            }).ShouldNotBeNull();

        AssociationProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(notification, TestContext.Current.CancellationToken);

        outcome.ShouldBe(AssociationProjectionHandler.ProjectionOutcome.Applied);
        AssociationCandidateView view = (await store.GetAsync(Tenant, AssociationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.LifecycleState.ShouldBe(LifecycleState.Associated);
        view.DecisionKind.ShouldBe(AssociationDecisionKind.Associate);
        view.DecisionNote.ShouldBe("Reviewed safe metadata.");
        view.DecisionNoteRedactionState.ShouldBe("metadata_only");
        view.Candidates.ShouldHaveSingleItem().ProjectId.ShouldBe("project-001");
    }

    [Fact]
    public static void TranslatorShouldRebuildDecisionSnapshotEvidenceFromDecisionEventPayload()
    {
        AssociationNotification notification = AssociationProjectionTranslator.TryCreateNotification(
            Published(7) with
            {
                EventTypeName = AssociationProjectionTranslator.DecisionConfirmedEventType,
                Candidates = null,
                CandidateProjectIds = ["project-001"],
                EvidenceRefs = [new AssociationEvidenceReference("mailbox:project-id", "hash-project", "ExplicitProjectIdentifier")],
                ConfidenceInputs =
                [
                    new AssociationConfidenceInput(
                        AssociationSignalClass.ExplicitProjectIdentifier,
                        AssociationReasonCode.ExplicitProjectIdentifierMatched,
                        0.9,
                        "mailbox:project-id",
                        "hash-project"),
                ],
                DecisionKind = AssociationDecisionKind.Associate,
                ActorId = "actor-alpha",
                ActorType = "human",
                DecidedAt = new DateTimeOffset(2026, 5, 31, 9, 15, 0, TimeSpan.Zero),
            }).ShouldNotBeNull();

        AssociationCandidate candidate = notification.Candidates.ShouldHaveSingleItem();
        candidate.ProjectId.ShouldBe("project-001");
        candidate.EvidenceRefs.ShouldHaveSingleItem().EvidenceFingerprint.ShouldBe("hash-project");
        candidate.ConfidenceInputs.ShouldHaveSingleItem().EvidenceFingerprint.ShouldBe("hash-project");
        notification.DecisionKind.ShouldBe(AssociationDecisionKind.Associate);
        notification.LifecycleState.ShouldBe(LifecycleState.Associated);
    }

    [Fact]
    public async Task HandlerShouldIgnoreStaleCandidateSnapshotsAfterDecision()
    {
        InMemoryAssociationProjectionStore store = new();
        AssociationProjectionHandler handler = new(store, new FixedClock());

        _ = await handler.HandleAsync(Notification(6) with { DecisionKind = AssociationDecisionKind.Reject }, TestContext.Current.CancellationToken);
        AssociationProjectionHandler.ProjectionOutcome stale = await handler.HandleAsync(Notification(5), TestContext.Current.CancellationToken);

        stale.ShouldBe(AssociationProjectionHandler.ProjectionOutcome.Ignored);
        AssociationCandidateView view = (await store.GetAsync(Tenant, AssociationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.SourceVersion.ShouldBe(6);
        view.DecisionKind.ShouldBe(AssociationDecisionKind.Reject);
    }

    [Fact]
    public async Task HandlerShouldProjectCorrectionSupersessionLinksAndCorrectedStatus()
    {
        InMemoryAssociationProjectionStore store = new();
        AssociationProjectionHandler handler = new(store, new FixedClock());

        AssociationNotification notification = AssociationProjectionTranslator.TryCreateNotification(
            Published(8) with
            {
                EventTypeName = AssociationProjectionTranslator.CorrectionAcceptedEventType,
                LifecycleState = LifecycleState.Corrected,
                ProjectId = null,
                ProjectDisplayName = null,
                CandidateProjectIds = ["project-001", "project-002"],
                EvidenceRefs = [new AssociationEvidenceReference("mailbox:project-alias", "hash-project-002", "ProjectAlias")],
                CorrectionKind = AssociationCorrectionKind.ProjectReassignment,
                PriorProjectId = "project-001",
                CorrectedProjectId = "project-002",
                PredecessorAssociationId = AssociationId,
                SupersedesAssociationId = AssociationId,
                CorrectionRationale = "Wrong project selected from safe metadata.",
                CorrectionActorId = "actor-alpha",
                CorrectionActorType = "human",
                CorrectedAt = new DateTimeOffset(2026, 5, 31, 9, 30, 0, TimeSpan.Zero),
                DownstreamImpactStatus = "preview-only",
            }).ShouldNotBeNull();

        AssociationProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(notification, TestContext.Current.CancellationToken);

        outcome.ShouldBe(AssociationProjectionHandler.ProjectionOutcome.Applied);
        AssociationCandidateView view = (await store.GetAsync(Tenant, AssociationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.LifecycleState.ShouldBe(LifecycleState.Corrected);
        view.ProjectId.ShouldBe("project-002");
        view.CorrectionKind.ShouldBe(AssociationCorrectionKind.ProjectReassignment);
        view.PriorProjectId.ShouldBe("project-001");
        view.CorrectedProjectId.ShouldBe("project-002");
        view.PredecessorAssociationId.ShouldBe(AssociationId);
        view.SupersedesAssociationId.ShouldBe(AssociationId);
        view.CorrectionRationale.ShouldBe("Wrong project selected from safe metadata.");
        view.DownstreamImpactStatus.ShouldBe("preview-only");
    }

    [Fact]
    public async Task HandlerShouldMergePropagationProgressAndIgnoreStaleSourceVersions()
    {
        InMemoryAssociationProjectionStore store = new();
        AssociationProjectionHandler handler = new(store, new FixedClock());

        AssociationNotification started = AssociationProjectionTranslator.TryCreateNotification(
            Published(9) with
            {
                EventTypeName = AssociationProjectionTranslator.CorrectionPropagationStartedEventType,
                LifecycleState = LifecycleState.Correcting,
                ProjectId = null,
                CorrectedProjectId = "project-002",
                PriorProjectId = "project-001",
                CorrectionId = "correction-001",
                WorkflowInstanceId = "workflow-001",
                RequiredStoreKeys = ["association-routing", "evidence-snapshot"],
                PropagationStartedAtUtc = DetectedAt,
                PropagationEstimatedCompletionAtUtc = DetectedAt.AddMinutes(10),
                DownstreamImpactStatus = "correcting",
            }).ShouldNotBeNull();

        _ = await handler.HandleAsync(started, TestContext.Current.CancellationToken);
        _ = await handler.HandleAsync(
            AssociationProjectionTranslator.TryCreateNotification(
                Published(9) with
                {
                    EventTypeName = AssociationProjectionTranslator.CorrectionStoreInvalidatedEventType,
                    LifecycleState = LifecycleState.Correcting,
                    ProjectId = null,
                    CorrectedProjectId = "project-002",
                    PriorProjectId = "project-001",
                    CorrectionId = "correction-001",
                    WorkflowInstanceId = "workflow-001",
                    StoreKey = "association-routing",
                    StoreOutcome = "success",
                    PropagationStartedAtUtc = DetectedAt,
                    PropagationCompletedAtUtc = DetectedAt.AddSeconds(5),
                }).ShouldNotBeNull(),
            TestContext.Current.CancellationToken);

        AssociationCandidateView view = (await store.GetAsync(Tenant, AssociationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.LifecycleState.ShouldBe(LifecycleState.Correcting);
        view.DownstreamImpactStatus.ShouldBe("correcting");
        view.IsCorrectedContextStale.ShouldBeTrue();
        view.PropagationProgressNumerator.ShouldBe(1);
        view.PropagationProgressDenominator.ShouldBe(2);
        view.CompletedStoreKeys.ShouldBe(["association-routing"]);

        AssociationProjectionHandler.ProjectionOutcome stale = await handler.HandleAsync(Notification(8), TestContext.Current.CancellationToken);
        stale.ShouldBe(AssociationProjectionHandler.ProjectionOutcome.Ignored);
        (await store.GetAsync(Tenant, AssociationId, TestContext.Current.CancellationToken)).ShouldNotBeNull().LifecycleState.ShouldBe(LifecycleState.Correcting);
    }

    [Fact]
    public async Task HandlerShouldNotLetSameVersionDelayedNotificationRollBackCompletedPropagation()
    {
        InMemoryAssociationProjectionStore store = new();
        AssociationProjectionHandler handler = new(store, new FixedClock());

        AssociationNotification started = AssociationProjectionTranslator.TryCreateNotification(
            Published(10) with
            {
                EventTypeName = AssociationProjectionTranslator.CorrectionPropagationStartedEventType,
                LifecycleState = LifecycleState.Correcting,
                ProjectId = null,
                CorrectedProjectId = "project-002",
                PriorProjectId = "project-001",
                CorrectionId = "correction-001",
                WorkflowInstanceId = "workflow-001",
                RequiredStoreKeys = ["association-routing", "evidence-snapshot"],
                PropagationStartedAtUtc = DetectedAt,
                PropagationEstimatedCompletionAtUtc = DetectedAt.AddMinutes(10),
            }).ShouldNotBeNull();
        AssociationNotification failed = AssociationProjectionTranslator.TryCreateNotification(
            Published(10) with
            {
                EventTypeName = AssociationProjectionTranslator.CorrectionStoreInvalidatedEventType,
                LifecycleState = LifecycleState.Correcting,
                ProjectId = null,
                CorrectedProjectId = "project-002",
                PriorProjectId = "project-001",
                CorrectionId = "correction-001",
                WorkflowInstanceId = "workflow-001",
                StoreKey = "evidence-snapshot",
                StoreOutcome = "failed",
                PropagationStartedAtUtc = DetectedAt,
                PropagationCompletedAtUtc = DetectedAt.AddSeconds(15),
            }).ShouldNotBeNull();
        AssociationNotification completed = AssociationProjectionTranslator.TryCreateNotification(
            Published(10) with
            {
                EventTypeName = AssociationProjectionTranslator.CorrectionPropagationCompletedEventType,
                LifecycleState = LifecycleState.Corrected,
                ProjectId = null,
                CorrectedProjectId = "project-002",
                PriorProjectId = "project-001",
                CorrectionId = "correction-001",
                WorkflowInstanceId = "workflow-001",
                CompletedStoreKeys = ["association-routing", "evidence-snapshot"],
                PropagationCompletedAtUtc = DetectedAt.AddSeconds(30),
            }).ShouldNotBeNull();
        AssociationNotification delayed = AssociationProjectionTranslator.TryCreateNotification(
            Published(10) with
            {
                EventTypeName = AssociationProjectionTranslator.CorrectionPropagationDelayedEventType,
                LifecycleState = LifecycleState.CorrectionDelayed,
                ProjectId = null,
                CorrectedProjectId = "project-002",
                PriorProjectId = "project-001",
                CorrectionId = "correction-001",
                WorkflowInstanceId = "workflow-001",
                ResponsibleOwnerRole = "operations",
                SafeNextAction = "escalate-to-operations",
            }).ShouldNotBeNull();

        _ = await handler.HandleAsync(started, TestContext.Current.CancellationToken);
        _ = await handler.HandleAsync(failed, TestContext.Current.CancellationToken);
        _ = await handler.HandleAsync(completed, TestContext.Current.CancellationToken);
        AssociationProjectionHandler.ProjectionOutcome delayedOutcome = await handler.HandleAsync(delayed, TestContext.Current.CancellationToken);

        delayedOutcome.ShouldBe(AssociationProjectionHandler.ProjectionOutcome.Ignored);
        AssociationCandidateView view = (await store.GetAsync(Tenant, AssociationId, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        view.LifecycleState.ShouldBe(LifecycleState.Corrected);
        view.PropagationStatus.ShouldBe(CorrectionPropagationStatuses.Complete);
        view.IsCorrectedContextStale.ShouldBeFalse();
        view.FailedStoreKeys.ShouldBeEmpty();
        view.CompletedStoreKeys.ShouldBe(["association-routing", "evidence-snapshot"]);
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
