using System.Net;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Adapters.Folders;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

public sealed class ProjectConversationProjectionTests
{
    private const string Tenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset DetectedAt = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AssociationHandlerShouldProjectTenantProjectPartitionedConversationItem()
    {
        InMemoryAssociationProjectionStore associationStore = new();
        InMemoryProjectConversationProjectionStore conversationStore = new();
        AssociationProjectionHandler handler = new(associationStore, new FixedClock(), conversationStore);

        AssociationProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(Notification(1), TestContext.Current.CancellationToken);
        ProjectConversationPage page = await conversationStore.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken);
        ProjectConversationPage foreign = await conversationStore.ReadPageAsync(OtherTenant, "project-001", null, 25, TestContext.Current.CancellationToken);

        outcome.ShouldBe(AssociationProjectionHandler.ProjectionOutcome.Applied);
        ProjectConversationItemView item = page.Items.ShouldHaveSingleItem();
        item.TenantId.ShouldBe(Tenant);
        item.ProjectId.ShouldBe("project-001");
        item.SourceMailboxId.ShouldBe("controlled-mailbox-001");
        item.SourceConversationId.ShouldBe("conversation-001");
        item.SourceProviderMessageId.ShouldBeNull();
        item.InternetMessageId.ShouldBeNull();
        item.Kind.ShouldBe(ProjectConversationItemKind.EmailDerived);
        item.ActorKind.ShouldBe(ProjectConversationActorKind.Mailbox);
        foreign.Items.ShouldBeEmpty();
        ProjectConversationItemView.KeyFor(Tenant, "project-001", AssociationId).ShouldStartWith("tenant-alpha:project-conversation:project-001:");
    }

    [Fact]
    public void ConversationItemViewShouldBuildSafeClassificationIntentAndReviewHistoryFromProjectedMetadata()
    {
        ProjectConversationItemView actionable = Item("item-actionable", 2, DetectedAt) with
        {
            SafeNextAction = "review-association",
            EvidenceReferenceSummary = ["mailbox:intake:subject"],
        };

        ProjectConversationItemClassification classification = actionable.BuildClassification();
        ProjectConversationDetectedIntent intent = actionable.BuildDetectedIntent().ShouldNotBeNull();
        ProjectConversationReviewHistoryEntry history = actionable.BuildReviewHistory().ShouldHaveSingleItem();

        classification.Kind.ShouldBe(ProjectConversationClassificationKind.Actionable);
        classification.KernelVersion.ShouldBe(ProjectConversationItemView.ClassificationKernelVersion);
        classification.SourceEvidenceIds.ShouldContain("mailbox:intake:subject");
        intent.ActionKind.ShouldBe(ProjectConversationDetectedActionKind.RequestDecision);
        intent.SafeNextAction.ShouldBe("review-association");
        history.ActionCode.ShouldBe("classification-projected");
        history.DecisionCode.ShouldBe("actionable");
        history.ActorKind.ShouldBe("mailbox");
        history.ReasonCode.ShouldBe("conversation_item_actionable");

        ProjectConversationReviewHistoryEntry attachmentHistory = (actionable with
        {
            Kind = ProjectConversationItemKind.Attachment,
            ActorKind = ProjectConversationActorKind.MailboxAttachment,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Pending,
        }).BuildReviewHistory().ShouldHaveSingleItem();

        attachmentHistory.DecisionCode.ShouldBe("pending");
        attachmentHistory.ActorKind.ShouldBe("mailbox-attachment");
    }

    [Fact]
    public async Task CapturedTaskIntentShouldReplacePlaceholderDetectedIntentAndRemainIdempotent()
    {
        InMemoryProjectConversationProjectionStore store = new();
        await store.UpsertAsync(Item("item-actionable", 2, DetectedAt) with
        {
            SourceProviderMessageId = "graph-message-001",
            SafeNextAction = "review-association",
            EvidenceReferenceSummary = ["placeholder:evidence"],
        }, TestContext.Current.CancellationToken);

        TaskIntentRecord record = TaskIntentRecord(8);
        await store.UpsertTaskIntentAsync(record, TestContext.Current.CancellationToken);
        await store.UpsertTaskIntentAsync(record, TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldHaveSingleItem();
        ProjectConversationDetectedIntent intent = item.BuildDetectedIntent().ShouldNotBeNull();

        intent.Summary.ShouldBe("authorized conversation item requests action");
        intent.ActionKind.ShouldBe(ProjectConversationDetectedActionKind.RequestAction);
        intent.SourceEvidenceIds.ShouldBe(["message:offset:001"]);
        intent.SafeNextAction.ShouldBe("review-task-intent-action");
        intent.MessageCode.ShouldBe("task_intent_captured");
    }

    [Fact]
    public async Task CapturedTaskIntentShouldIgnoreOlderProjectionReplay()
    {
        InMemoryProjectConversationProjectionStore store = new();
        await store.UpsertAsync(Item("item-actionable", 2, DetectedAt) with
        {
            SourceProviderMessageId = "graph-message-001",
        }, TestContext.Current.CancellationToken);

        await store.UpsertTaskIntentAsync(TaskIntentRecord(8), TestContext.Current.CancellationToken);
        await store.UpsertTaskIntentAsync(TaskIntentRecord(7) with { DetectedIntentSummary = "stale summary" }, TestContext.Current.CancellationToken);

        ProjectConversationDetectedIntent intent = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem()
            .BuildDetectedIntent()
            .ShouldNotBeNull();

        intent.Summary.ShouldBe("authorized conversation item requests action");
    }

    [Fact]
    public async Task TaskIntentHandlerShouldProjectCapturedEventIntoConversationItem()
    {
        InMemoryProjectConversationProjectionStore store = new();
        await store.UpsertAsync(Item("item-actionable", 2, DetectedAt) with
        {
            SourceProviderMessageId = "graph-message-001",
        }, TestContext.Current.CancellationToken);
        TaskIntentProjectionHandler handler = new(store);

        TaskIntentProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(
            new PublishedTaskIntentEvent(
                Tenant,
                "chatbot",
                "01ARZ3NDEKTSV4RRFFQ69G5FAD",
                typeof(TaskIntentCaptured).FullName,
                12,
                DetectedAt,
                CorrelationId,
                TaskIntentRecord(8)),
            TestContext.Current.CancellationToken);

        ProjectConversationDetectedIntent intent = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem()
            .BuildDetectedIntent()
            .ShouldNotBeNull();

        outcome.ShouldBe(TaskIntentProjectionHandler.ProjectionOutcome.Applied);
        intent.MessageCode.ShouldBe("task_intent_captured");
    }

    [Fact]
    public void AiSummaryProvenanceShouldNotFabricateModelVersionFromActorIdentity()
    {
        ProjectConversationItemView item = Item("ai:proposal-001:proposal:10", 10, DetectedAt) with
        {
            Kind = ProjectConversationItemKind.AiOutcome,
            ActorKind = ProjectConversationActorKind.AiActor,
            AiActorId = "ai-actor-001",
            AiActorType = "ai",
            AiAuthorizedContextReferences = ["evidence:summary:001"],
            AiContextPackageId = "context-package-001",
            AiContextPackageVersion = "v1",
            AiGeneratedSummaryRedactionState = "metadata_only",
        };

        ProjectConversationAiSummaryProvenance provenance = item.BuildAiSummaryProvenance().ShouldNotBeNull();

        provenance.GeneratedBy.ShouldBe("unavailable");
        provenance.SourceEvidenceIds.ShouldContain("evidence:summary:001");
        provenance.ContextPackageId.ShouldBe("context-package-001");
    }

    [Fact]
    public async Task ConversationProjectionShouldMergeIntakeSourceIdentityWhenIntakeArrivesBeforeAssociation()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), conversationStore);

        await handler.HandleAsync(IntakeCaptured(), Tenant, 1, CorrelationId, TestContext.Current.CancellationToken);
        await handler.HandleAsync(Notification(2), TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await conversationStore.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldHaveSingleItem();
        item.SourceProviderMessageId.ShouldBe("graph-message-001");
        item.InternetMessageId.ShouldBe("<internet-message-001@example.test>");
        item.SourceReceivedAtUtc.ShouldBe(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        item.SourceSentAtUtc.ShouldBe(new DateTimeOffset(2026, 5, 31, 23, 58, 0, TimeSpan.Zero));
        item.SourceCreatedAtUtc.ShouldBe(new DateTimeOffset(2026, 5, 31, 23, 57, 0, TimeSpan.Zero));
        item.SourceTimezone.ShouldBe("UTC");
        item.SourceProvenanceDisplayToken.ShouldBe("Microsoft 365 mailbox");
    }

    [Fact]
    public async Task ConversationProjectionShouldEnrichExistingAssociationWhenIntakeArrivesAfterAssociation()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), conversationStore);

        await handler.HandleAsync(Notification(2), TestContext.Current.CancellationToken);
        await handler.HandleAsync(IntakeCaptured(), Tenant, 3, CorrelationId, TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await conversationStore.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldHaveSingleItem();
        item.SourceProviderMessageId.ShouldBe("graph-message-001");
        item.InternetMessageId.ShouldBe("<internet-message-001@example.test>");
        item.SourceVersion.ShouldBe(2);
    }

    [Fact]
    public async Task ConversationStoreShouldRejectOlderAssociationAfterSourceEmailEnrichment()
    {
        InMemoryProjectConversationProjectionStore store = new();

        await store.UpsertAsync(Item("item-a", 5, DetectedAt), TestContext.Current.CancellationToken);
        await store.UpsertSourceEmailAsync(SourceEmail(10, "graph-message-current"), TestContext.Current.CancellationToken);
        await store.UpsertAsync(
            Item("item-a", 4, DetectedAt.AddMinutes(-5)) with
            {
                LifecycleState = LifecycleState.Correcting,
                SafeNextAction = "wait-for-propagation",
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldHaveSingleItem();
        item.SourceVersion.ShouldBe(5);
        item.LifecycleState.ShouldBe(LifecycleState.Associated);
        item.SourceProviderMessageId.ShouldBe("graph-message-current");
    }

    [Fact]
    public async Task ConversationStoreShouldIgnoreStaleSourceEmailReplayWhenEnrichingExistingItems()
    {
        InMemoryProjectConversationProjectionStore store = new();

        await store.UpsertAsync(Item("item-a", 5, DetectedAt), TestContext.Current.CancellationToken);
        await store.UpsertSourceEmailAsync(SourceEmail(10, "graph-message-current"), TestContext.Current.CancellationToken);
        await store.UpsertSourceEmailAsync(SourceEmail(9, "graph-message-stale"), TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldHaveSingleItem();
        item.SourceProviderMessageId.ShouldBe("graph-message-current");
    }

    [Fact]
    public async Task ConversationStoreShouldOrderByUtcSourceTimeAndIgnoreOlderReplays()
    {
        InMemoryProjectConversationProjectionStore store = new();

        await store.UpsertAsync(Item("item-b", 2, DetectedAt.AddMinutes(2)), TestContext.Current.CancellationToken);
        await store.UpsertAsync(Item("item-a", 1, DetectedAt), TestContext.Current.CancellationToken);
        await store.UpsertAsync(Item("item-b", 1, DetectedAt.AddMinutes(-5)), TestContext.Current.CancellationToken);

        ProjectConversationPage page = await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken);

        page.Items.Select(static item => item.ItemId).ShouldBe(["item-a", "item-b"], ignoreOrder: false);
        page.Items.Last().SourceVersion.ShouldBe(2);
        page.Items.Last().OccurredAt.ShouldBe(DetectedAt.AddMinutes(2));
    }

    [Fact]
    public void ConversationItemReplacementShouldRejectOlderReplays()
    {
        ProjectConversationItemView current = Item("item-b", 2, DetectedAt.AddMinutes(2));
        ProjectConversationItemView older = Item("item-b", 1, DetectedAt.AddMinutes(-5));
        ProjectConversationItemView newer = Item("item-b", 3, DetectedAt.AddMinutes(3));

        ProjectConversationItemView.ShouldReplace(current, older).ShouldBeFalse();
        ProjectConversationItemView.ShouldReplace(current, newer).ShouldBeTrue();
    }

    [Fact]
    public void ConversationItemStatusSummaryShouldUseProjectedStatusFieldsAndStableOrder()
    {
        ProjectConversationItemView item = Item("failure:operation-001:retry-queued:20", 20, DetectedAt) with
        {
            LifecycleState = LifecycleState.Failed,
            FailureStateKind = FailureStateKind.RetryQueued,
            FailureStatus = FailureStatus.Retryable,
            MessageCatalogCode = ChatBotMessageCodes.RetryQueued,
            SafeNextAction = ChatBotMessageNextActions.RetryLater,
            OperationId = "operation-001",
            AuditOperationId = "audit-001",
            AuditStatus = "reconciling",
            TaskId = "task-001",
            Retryable = true,
            RetryCount = 1,
            RetryOperationId = "retry-operation-001",
            DuplicateSafetyState = "duplicate-safe",
        };

        ProjectConversationItemStatusSummary summary = item.BuildStatusSummary();

        summary.Facets.Select(static facet => facet.Domain).ShouldBe(
            ["association", "attachment", "task", "approval", "command", "failure", "retry", "next-action"],
            ignoreOrder: false);
        summary.Facets.Single(static facet => facet.Domain == "association").Health.ShouldBe(ChatBotHealthStatus.Failed);
        summary.Facets.Single(static facet => facet.Domain == "task").Health.ShouldBe(ChatBotHealthStatus.Unknown);
        summary.Facets.Single(static facet => facet.Domain == "task").SafeNextAction.ShouldBe("none");
        ProjectConversationItemStatusFacet command = summary.Facets.Single(static facet => facet.Domain == "command");
        command.Health.ShouldBe(ChatBotHealthStatus.Degraded);
        command.OperationId.ShouldBe("operation-001");
        command.ProjectionStatus.ShouldBe("retryable");
        command.AuditStatus.ShouldBe("reconciling");
        command.CorrelationId.ShouldBe(CorrelationId);
        command.RetryCount.ShouldBe(1);
        command.DuplicateSafetyState.ShouldBe("duplicate-safe");
        ProjectConversationItemStatusFacet failure = summary.Facets.Single(static facet => facet.Domain == "failure");
        failure.SourceState.ShouldBe("retry-queued");
        failure.MessageCode.ShouldBe(ChatBotMessageCodes.RetryQueued);
        failure.SafeNextAction.ShouldBe(ChatBotMessageNextActions.RetryLater);
        ProjectConversationItemStatusFacet retry = summary.Facets.Single(static facet => facet.Domain == "retry");
        retry.Health.ShouldBe(ChatBotHealthStatus.Degraded);
        retry.SourceState.ShouldBe("retry-queued");

        string json = JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldContain("\"health\":\"degraded\"");
        json.ShouldContain("\"sourceState\":\"retry-queued\"");
        json.ShouldNotContain("commandPayload", Case.Insensitive);
        json.ShouldNotContain("auditEnvelope", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        json.ShouldNotContain("localPath", Case.Insensitive);
    }

    [Fact]
    public static void MailboxIntakeTranslatorShouldRejectUnsafeOrIncompleteEnvelope()
    {
        MailboxIntakeProjectionNotification notification = MailboxIntakeProjectionTranslator.TryCreateNotification(PublishedIntake(3)).ShouldNotBeNull();

        notification.TenantId.ShouldBe(Tenant);
        notification.Captured.ProviderMessageId.ShouldBe("graph-message-001");
        notification.SourceVersion.ShouldBe(3);

        MailboxIntakeProjectionTranslator.TryCreateNotification(PublishedIntake(3) with { Domain = "folders" }).ShouldBeNull();
        MailboxIntakeProjectionTranslator.TryCreateNotification(PublishedIntake(3) with { ReceivedAtUtc = default }).ShouldBeNull();
        MailboxIntakeProjectionTranslator.TryCreateNotification(PublishedIntake(0)).ShouldBeNull();
    }

    [Fact]
    public static void SourceEmailDisplayTokenShouldUseSafeFallbackForUnknownProvenance()
    {
        ProjectConversationSourceEmailView source = ProjectConversationSourceEmailView.FromIntake(
            Tenant,
            IntakeCaptured() with { SourceProvenance = "raw provider/source context" },
            3,
            CorrelationId);

        source.SourceProvenanceDisplayToken.ShouldBe("source-provenance-unavailable");
        source.SourceProvenanceDisplayToken.ShouldNotContain("raw provider", Case.Insensitive);
    }

    [Fact]
    public async Task CursorShouldBeProjectScopedAndNotExposeTenantOrProjectText()
    {
        InMemoryProjectConversationProjectionStore store = new();
        await store.UpsertAsync(Item("item-a", 1, DetectedAt), TestContext.Current.CancellationToken);
        await store.UpsertAsync(Item("item-b", 2, DetectedAt.AddMinutes(1)), TestContext.Current.CancellationToken);

        ProjectConversationPage first = await store.ReadPageAsync(Tenant, "project-001", null, 1, TestContext.Current.CancellationToken);
        first.HasMore.ShouldBeTrue();
        first.NextCursor.ShouldNotBeNull();
        first.NextCursor.ShouldNotContain(Tenant, Case.Sensitive);
        first.NextCursor.ShouldNotContain("project-001", Case.Sensitive);

        ProjectConversationPage second = await store.ReadPageAsync(Tenant, "project-001", first.NextCursor, 1, TestContext.Current.CancellationToken);
        second.Items.ShouldHaveSingleItem().ItemId.ShouldBe("item-b");

        ProjectConversationPage wrongProject = await store.ReadPageAsync(Tenant, "project-002", first.NextCursor, 1, TestContext.Current.CancellationToken);
        wrongProject.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task CorrectingStateShouldRenderAsSystemDecisionWithStaleSafeAction()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), conversationStore);

        await handler.HandleAsync(
            Notification(3) with
            {
                LifecycleState = LifecycleState.Correcting,
                CorrectionKind = AssociationCorrectionKind.ProjectReassignment,
                SafeNextAction = "wait-for-propagation",
                IsCorrectedContextStale = true,
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView[] items = (await conversationStore.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ToArray();
        ProjectConversationItemView source = items.Single(static item => item.Kind == ProjectConversationItemKind.EmailDerived);
        ProjectConversationItemView decision = items.Single(static item => item.Kind == ProjectConversationItemKind.SystemDecision);
        source.ItemId.ShouldBe(AssociationId);
        decision.ItemId.ShouldBe(ProjectConversationItemView.DecisionItemIdFor(AssociationId, 3));
        decision.ActorKind.ShouldBe(ProjectConversationActorKind.SystemDecision);
        decision.SafeNextAction.ShouldBe("wait-for-propagation");
        decision.LifecycleState.ShouldBe(LifecycleState.Correcting);
        decision.CorrectionKind.ShouldBe(AssociationCorrectionKind.ProjectReassignment);
        decision.IsCorrectedContextStale.ShouldBe(true);
    }

    [Fact]
    public async Task CorrectionProjectionShouldSuppressCorrectedProjectDetailFromPriorProjectHistory()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), conversationStore);

        await handler.HandleAsync(
            Notification(3) with
            {
                LifecycleState = LifecycleState.CorrectionDelayed,
                CorrectionKind = AssociationCorrectionKind.ProjectReassignment,
                PriorProjectId = "project-000",
                CorrectedProjectId = "project-001",
                CorrectionActorId = "user-001",
                CorrectionActorType = "human",
                CorrectedAt = DetectedAt.AddMinutes(1),
                CorrectionRationale = "raw correction rationale must stay off S1",
                CorrectionRationaleRedactionState = "redacted",
                SafeNextAction = "wait-for-propagation",
                IsCorrectedContextStale = true,
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView priorDecision = (await conversationStore.ReadPageAsync(Tenant, "project-000", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.SystemDecision);
        ProjectConversationItemView correctedDecision = (await conversationStore.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.SystemDecision);

        priorDecision.PriorProjectId.ShouldBe("project-000");
        priorDecision.CorrectedProjectId.ShouldBeNull();
        priorDecision.ProjectDisplayName.ShouldBeNull();
        priorDecision.CorrectionRationaleRedactionState.ShouldBe("redacted");
        correctedDecision.CorrectedProjectId.ShouldBe("project-001");
        correctedDecision.ProjectDisplayName.ShouldBe("Project One");
    }

    [Fact]
    public async Task DecisionNotificationsShouldAppendHistoryWithoutReplacingSourceEmailContext()
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), conversationStore);

        await handler.HandleAsync(Notification(2), TestContext.Current.CancellationToken);
        await handler.HandleAsync(
            Notification(3) with
            {
                DecisionKind = AssociationDecisionKind.Associate,
                DecisionActorId = "user-001",
                DecisionActorType = "human",
                DecidedAt = DetectedAt.AddMinutes(1),
                DecisionNote = "raw note must stay off S1",
                DecisionNoteRedactionState = "redacted",
                SurfaceOrigin = "ui",
                PolicySnapshotVersion = "association-thresholds.m0.default.v1",
                SafeNextAction = "none",
            },
            TestContext.Current.CancellationToken);
        await handler.HandleAsync(
            Notification(4) with
            {
                DecisionKind = AssociationDecisionKind.Defer,
                DecisionActorId = "user-002",
                DecisionActorType = "human",
                DecidedAt = DetectedAt.AddMinutes(2),
                DecisionNote = "new raw note must stay off S1",
                DecisionNoteRedactionState = "redacted",
                SurfaceOrigin = "ui",
                SafeNextAction = "review-later",
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView[] items = (await conversationStore.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ToArray();

        items.Single(static item => item.Kind == ProjectConversationItemKind.EmailDerived).ItemId.ShouldBe(AssociationId);
        ProjectConversationItemView[] decisions = items.Where(static item => item.Kind == ProjectConversationItemKind.SystemDecision).OrderBy(static item => item.SourceVersion).ToArray();
        decisions.Select(static item => item.ItemId).ShouldBe(
            [
                ProjectConversationItemView.DecisionItemIdFor(AssociationId, 3),
                ProjectConversationItemView.DecisionItemIdFor(AssociationId, 4),
            ],
            ignoreOrder: false);
        decisions[0].DecisionKind.ShouldBe(AssociationDecisionKind.Associate);
        decisions[0].DecisionNoteRedactionState.ShouldBe("redacted");
        decisions[1].DecisionKind.ShouldBe(AssociationDecisionKind.Defer);
        decisions[1].SafeNextAction.ShouldBe("review-later");
    }

    [Fact]
    public async Task ConversationStoreShouldMaterializeParticipantArrivingBeforeAssociationOnlyAfterAuthorizedAssociation()
    {
        InMemoryProjectConversationProjectionStore store = new();

        await store.UpsertParticipantResolutionAsync(ParticipantView(4), TestContext.Current.CancellationToken);
        (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldBeEmpty();

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);

        ProjectConversationItemView participant = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Participant);
        participant.ParticipantResolutionId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FBP");
        participant.SourceParticipantId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FBQ");
        participant.ParticipantDisplayKind.ShouldBe(ProjectConversationParticipantDisplayKind.InternalParticipant);
        participant.ActorKind.ShouldBe(ProjectConversationActorKind.InternalParticipant);
        participant.PartyId.ShouldBe("tenant-alpha:parties:party-001");
        participant.SourceProviderMessageId.ShouldBeNull();
        participant.InternetMessageId.ShouldBeNull();
    }

    [Fact]
    public async Task ConversationStoreShouldMaterializeAttachmentsArrivingBeforeAssociationOnlyAfterAuthorizedAssociation()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store);

        await handler.HandleAsync(IntakeCapturedWithAttachments(), Tenant, 8, CorrelationId, TestContext.Current.CancellationToken);
        (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ShouldBeEmpty();

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);

        ProjectConversationItemView attachment = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);
        attachment.ActorKind.ShouldBe(ProjectConversationActorKind.MailboxAttachment);
        attachment.SourceProviderAttachmentId.ShouldBe("graph-attachment-001");
        attachment.AttachmentDisplayName.ShouldBe("invoice.pdf");
        attachment.AttachmentContentType.ShouldBe("application/pdf");
        attachment.AttachmentSizeInBytes.ShouldBe(4096);
        attachment.AttachmentCaptureStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Pending);
        attachment.AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Pending);
        attachment.SourceProviderMessageId.ShouldBeNull();
        attachment.ItemId.ShouldNotContain("graph-attachment-001", Case.Sensitive);
    }

    [Fact]
    public async Task AssociationHandlerShouldCaptureStoredAttachmentsWhenAssociationArrivesAfterAttachmentReferences()
    {
        InMemoryProjectConversationProjectionStore store = new();
        CapturingFolderStore folders = new();
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FixedMailboxContentSource(MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "text/plain", "hashref_abc")),
            folders);
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store, coordinator);

        await handler.HandleAsync(IntakeCapturedWithAttachments(), Tenant, 8, CorrelationId, TestContext.Current.CancellationToken);
        await handler.HandleAsync(Notification(9), TestContext.Current.CancellationToken);

        folders.Requests.Count.ShouldBe(1);
        ProjectConversationItemView attachment = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);
        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentFolderId.ShouldBe("folder-project-001");
        attachment.AttachmentFileId.ShouldBe("file-graph-attachment-001-0");
    }

    [Fact]
    public async Task AssociationHandlerShouldCaptureStoredAttachmentsWhenAttachmentReferencesArriveAfterAssociation()
    {
        InMemoryProjectConversationProjectionStore store = new();
        CapturingFolderStore folders = new();
        AttachmentCaptureCoordinator coordinator = new(
            store,
            new FixedMailboxContentSource(MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "text/plain", "hashref_abc")),
            folders);
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store, coordinator);

        await handler.HandleAsync(Notification(9), TestContext.Current.CancellationToken);
        await handler.HandleAsync(IntakeCapturedWithAttachments(), Tenant, 10, CorrelationId, TestContext.Current.CancellationToken);

        folders.Requests.Count.ShouldBe(1);
        ProjectConversationItemView attachment = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);
        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentFolderId.ShouldBe("folder-project-001");
        attachment.AttachmentFileId.ShouldBe("file-graph-attachment-001-0");
    }

    [Fact]
    public async Task ApprovalProjectionShouldMaterializeAppendOnlyMetadataItemsAndPartitionByTenantProject()
    {
        InMemoryProjectConversationProjectionStore store = new();
        ApprovalProjectionHandler handler = new(store);

        ApprovalProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(ApprovalPublished(8), TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();
        ProjectConversationPage foreign = await store.ReadPageAsync(OtherTenant, "project-001", null, 25, TestContext.Current.CancellationToken);

        outcome.ShouldBe(ApprovalProjectionHandler.ProjectionOutcome.Applied);
        item.Kind.ShouldBe(ProjectConversationItemKind.ApprovalEvent);
        item.ActorKind.ShouldBe(ProjectConversationActorKind.ApprovalSystem);
        item.ItemId.ShouldBe(ProjectConversationItemView.ApprovalItemIdFor("approval-001", ApprovalEventKind.Request, 8));
        item.ApprovalId.ShouldBe("approval-001");
        item.ApprovalEventKind.ShouldBe(ApprovalEventKind.Request);
        item.ApprovalStatus.ShouldBe(ApprovalStatus.Pending);
        item.ApprovalCommandName.ShouldBe("SendExternalReply");
        item.ApprovalEvidenceFreshnessStates.ShouldBe([ApprovalEvidenceFreshness.Expired], ignoreOrder: false);
        item.ApprovalPolicySnapshotVisibility.ShouldBe("redacted");
        item.ApprovalPolicySnapshotId.ShouldBeNull();
        item.ApprovalActionSummaryRedactionState.ShouldBe("redacted");
        item.ApprovalAuditOperationId.ShouldBeNull();
        foreign.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApprovalProjectionShouldSuppressUnavailablePolicyAndAuditReferenceIds()
    {
        InMemoryProjectConversationProjectionStore store = new();
        ApprovalProjectionHandler handler = new(store);

        await handler.HandleAsync(
            ApprovalPublished(10) with
            {
                EventKind = ApprovalEventKind.Decision,
                Status = ApprovalStatus.Rejected,
                DecisionKind = ApprovalDecisionKind.Reject,
                PolicySnapshotId = "restricted-policy-snapshot",
                PolicySnapshotVisibility = "unavailable",
                AuditOperationId = "restricted-audit-operation",
                AuditStatus = "unavailable",
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();

        item.ApprovalPolicySnapshotVisibility.ShouldBe("unavailable");
        item.ApprovalPolicySnapshotId.ShouldBeNull();
        item.ApprovalAuditStatus.ShouldBe("unavailable");
        item.ApprovalAuditOperationId.ShouldBeNull();
    }

    [Fact]
    public async Task FailureStateProjectionShouldMaterializeCatalogBackedMetadataOnlyAppendOnlyItems()
    {
        InMemoryProjectConversationProjectionStore store = new();
        FailureStateProjectionHandler handler = new(store);

        FailureStateProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(FailurePublished(20), TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();
        ProjectConversationPage foreign = await store.ReadPageAsync(OtherTenant, "project-001", null, 25, TestContext.Current.CancellationToken);

        outcome.ShouldBe(FailureStateProjectionHandler.ProjectionOutcome.Applied);
        item.Kind.ShouldBe(ProjectConversationItemKind.FailureState);
        item.ActorKind.ShouldBe(ProjectConversationActorKind.SystemStatus);
        item.ItemId.ShouldBe(ProjectConversationItemView.FailureStateItemIdFor("operation-001", FailureStateKind.RetryQueued, 20));
        item.FailureStateKind.ShouldBe(FailureStateKind.RetryQueued);
        item.FailureStatus.ShouldBe(FailureStatus.Retryable);
        item.MessageCatalogCode.ShouldBe(ChatBotMessageCodes.RetryQueued);
        item.MessageCatalogVersion.ShouldBe(ChatBotMessageCatalogVersion.Current);
        item.MessageDetailVisibility.ShouldBe(ChatBotDetailVisibility.MetadataOnly);
        item.Retryable.ShouldBe(true);
        item.RetryCount.ShouldBe(1);
        item.MaxRetryCount.ShouldBe(3);
        item.OperationId.ShouldBe("operation-001");
        item.TaskId.ShouldBe("task-001");
        item.WorkflowInstanceId.ShouldBe("workflow-001");
        item.AuditOperationId.ShouldBe("audit-001");
        item.SafeNextAction.ShouldBe(ChatBotMessageNextActions.RetryLater);
        item.BlockedReason.ShouldBe(ChatBotDisabledActionReasons.ProjectionPending);
        item.SourceProviderMessageId.ShouldBeNull();
        foreign.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task FailureStateProjectionShouldSuppressUnavailableAuditIdsAndPreserveRetryBeforeFailureHistory()
    {
        InMemoryProjectConversationProjectionStore store = new();
        FailureStateProjectionHandler handler = new(store);

        await handler.HandleAsync(
            FailurePublished(22) with
            {
                FailureStateKind = FailureStateKind.TerminalFailure,
                FailureStatus = FailureStatus.Terminal,
                MessageCatalogCode = ChatBotMessageCodes.TerminalFailure,
                AuditOperationId = "restricted-audit-operation",
                AuditStatus = "unavailable",
                Retryable = false,
            },
            TestContext.Current.CancellationToken);
        await handler.HandleAsync(FailurePublished(21), TestContext.Current.CancellationToken);

        ProjectConversationItemView[] items = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .OrderBy(static item => item.SourceVersion)
            .ToArray();

        items.Select(static item => item.FailureStateKind).ShouldBe([FailureStateKind.RetryQueued, FailureStateKind.TerminalFailure], ignoreOrder: false);
        items[1].AuditStatus.ShouldBe("unavailable");
        items[1].AuditOperationId.ShouldBeNull();
        items[1].Retryable.ShouldBe(false);
        items.Select(static item => item.ItemId).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task FailureStateProjectionShouldSuppressUnsafeOptionalMetadataTokens()
    {
        InMemoryProjectConversationProjectionStore store = new();
        FailureStateProjectionHandler handler = new(store);

        await handler.HandleAsync(
            FailurePublished(23) with
            {
                FailureCategory = "raw exception /home/administrator/project-secret.txt",
                FailureScope = "tenant-alpha project-alpha",
                FailureReasonCode = "provider diagnostic C:\\temp\\secret.txt",
                AuditOperationId = "audit operation /tmp/raw",
                AuditStatus = "available",
                ClientAction = "retry later",
                DuplicateSafetyState = "duplicate safe raw exception",
                DuplicateSuppressionId = "duplicate/suppression/raw",
                DependencyName = "Graph provider raw payload",
                EscalationTargetRole = "project owner",
                ReprocessCreatedWorkflowInstanceId = "workflow raw",
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView item = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();
        string serialized = JsonSerializer.Serialize(item);

        item.FailureCategory.ShouldBeNull();
        item.FailureScope.ShouldBeNull();
        item.FailureReasonCode.ShouldBeNull();
        item.AuditOperationId.ShouldBeNull();
        item.ClientAction.ShouldBe(ChatBotMessageNextActions.RetryLater);
        item.DuplicateSafetyState.ShouldBeNull();
        item.DuplicateSuppressionId.ShouldBeNull();
        item.DependencyName.ShouldBeNull();
        item.EscalationTargetRole.ShouldBeNull();
        item.ReprocessCreatedWorkflowInstanceId.ShouldBeNull();
        serialized.ShouldNotContain("raw exception", Case.Insensitive);
        serialized.ShouldNotContain("provider diagnostic", Case.Insensitive);
        serialized.ShouldNotContain("/home/administrator", Case.Insensitive);
        serialized.ShouldNotContain("C:\\", Case.Insensitive);
    }

    [Fact]
    public async Task FailureStateProjectionShouldIgnoreUnsafeRequiredMetadataTokens()
    {
        InMemoryProjectConversationProjectionStore store = new();
        FailureStateProjectionHandler handler = new(store);

        FailureStateProjectionHandler.ProjectionOutcome outcome = await handler.HandleAsync(
            FailurePublished(24) with { OperationId = "operation raw exception /tmp/secret" },
            TestContext.Current.CancellationToken);

        ProjectConversationPage page = await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken);
        outcome.ShouldBe(FailureStateProjectionHandler.ProjectionOutcome.Ignored);
        page.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApprovalProjectionShouldBeIdempotentAndRejectStaleReplayForSameSourceVersionedItem()
    {
        InMemoryProjectConversationProjectionStore store = new();
        ApprovalProjectionHandler handler = new(store);

        await handler.HandleAsync(ApprovalPublished(9) with { Status = ApprovalStatus.Approved }, TestContext.Current.CancellationToken);
        await handler.HandleAsync(ApprovalPublished(9) with { Status = ApprovalStatus.Rejected }, TestContext.Current.CancellationToken);
        await handler.HandleAsync(ApprovalPublished(8) with { Status = ApprovalStatus.Failed }, TestContext.Current.CancellationToken);

        ProjectConversationItemView[] items = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ToArray();

        items.Select(static item => item.ItemId).ShouldBe([
            ProjectConversationItemView.ApprovalItemIdFor("approval-001", ApprovalEventKind.Request, 8),
            ProjectConversationItemView.ApprovalItemIdFor("approval-001", ApprovalEventKind.Request, 9),
        ], ignoreOrder: false);
        items.Single(static item => item.SourceVersion == 9).ApprovalStatus.ShouldBe(ApprovalStatus.Rejected);
        items.Single(static item => item.SourceVersion == 8).ApprovalStatus.ShouldBe(ApprovalStatus.Failed);
    }

    [Fact]
    public async Task ApprovalDecisionBeforeRequestShouldLaterEnrichWithoutMutatingHistoryIds()
    {
        InMemoryProjectConversationProjectionStore store = new();
        ApprovalProjectionHandler handler = new(store);

        await handler.HandleAsync(
            ApprovalPublished(10) with
            {
                EventKind = ApprovalEventKind.Decision,
                Status = ApprovalStatus.Approved,
                DecisionKind = ApprovalDecisionKind.Approve,
                DecisionActorId = "user-approver",
                DecisionActorType = "human",
                DecidedAtUtc = DetectedAt.AddMinutes(10),
                SourceConversationItemId = null,
                SourceMessageId = null,
                ProposalId = null,
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView before = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();
        before.ApprovalProposalId.ShouldBeNull();

        await handler.HandleAsync(ApprovalPublished(8), TestContext.Current.CancellationToken);

        ProjectConversationItemView[] items = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken)).Items.ToArray();
        ProjectConversationItemView decision = items.Single(static item => item.ApprovalEventKind == ApprovalEventKind.Decision);

        items.Select(static item => item.ItemId).ShouldContain(ProjectConversationItemView.ApprovalItemIdFor("approval-001", ApprovalEventKind.Request, 8));
        decision.ItemId.ShouldBe(ProjectConversationItemView.ApprovalItemIdFor("approval-001", ApprovalEventKind.Decision, 10));
        decision.ApprovalProposalId.ShouldBe("proposal-001");
        decision.ApprovalSourceConversationItemId.ShouldBe("decision:source:001");
        decision.ApprovalRequesterId.ShouldBe("requester-001");
        decision.ApprovalDecisionKind.ShouldBe(ApprovalDecisionKind.Approve);
    }

    [Fact]
    public async Task ApprovalOutcomeAcceptedProjectionPendingShouldNotClaimExecutedDone()
    {
        InMemoryProjectConversationProjectionStore store = new();
        ApprovalProjectionHandler handler = new(store);

        await handler.HandleAsync(
            ApprovalPublished(11) with
            {
                EventKind = ApprovalEventKind.Outcome,
                Status = ApprovalStatus.Approved,
                CommandOutcomeStatus = "accepted-projection-pending",
                AuditStatus = "reconciling",
                OutcomeAtUtc = DetectedAt.AddMinutes(11),
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView outcome = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .ShouldHaveSingleItem();

        outcome.ApprovalEventKind.ShouldBe(ApprovalEventKind.Outcome);
        outcome.ApprovalCommandOutcomeStatus.ShouldBe("accepted-projection-pending");
        outcome.ApprovalStatus.ShouldNotBe(ApprovalStatus.Executed);
    }

    [Fact]
    public async Task ConversationStoreShouldMaterializeAttachmentsArrivingAfterAssociationAndRejectStaleReplay()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store);

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await handler.HandleAsync(IntakeCapturedWithAttachments("current.pdf"), Tenant, 8, CorrelationId, TestContext.Current.CancellationToken);
        await handler.HandleAsync(IntakeCapturedWithAttachments("stale.pdf"), Tenant, 7, CorrelationId, TestContext.Current.CancellationToken);

        ProjectConversationItemView attachment = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);
        attachment.AttachmentDisplayName.ShouldBe("current.pdf");
        attachment.SourceVersion.ShouldBe(8);
    }

    [Fact]
    public async Task ConversationStoreShouldMaterializeDuplicateProviderAttachmentIdsAsDistinctMetadataOnlyItems()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store);

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await handler.HandleAsync(
            IntakeCaptured() with
            {
                AttachmentReferences =
                [
                    new MailboxAttachmentReference("graph-attachment-duplicate", "one.pdf", "application/pdf", 100),
                    new MailboxAttachmentReference("graph-attachment-duplicate", "two.pdf", "application/pdf", 200),
                ],
            },
            Tenant,
            9,
            CorrelationId,
            TestContext.Current.CancellationToken);

        ProjectConversationItemView[] attachments = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Where(static item => item.Kind == ProjectConversationItemKind.Attachment)
            .OrderBy(static item => item.AttachmentDisplayName, StringComparer.Ordinal)
            .ToArray();

        attachments.Length.ShouldBe(2);
        attachments.Select(static item => item.AttachmentDisplayName).ShouldBe(["one.pdf", "two.pdf"], ignoreOrder: false);
        attachments.Select(static item => item.ItemId).Distinct(StringComparer.Ordinal).Count().ShouldBe(2);
    }

    [Fact]
    public async Task ConversationStoreShouldRedactRestrictedAttachmentMetadataWithoutLeakingSize()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store);

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await handler.HandleAsync(
            IntakeCaptured() with
            {
                AttachmentReferences =
                [
                    new MailboxAttachmentReference("graph-attachment-002", "C:\\restricted\\secret.pdf", "application/pdf", 4096),
                ],
                RedactionState = "redacted",
            },
            Tenant,
            9,
            CorrelationId,
            TestContext.Current.CancellationToken);

        ProjectConversationItemView attachment = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);

        attachment.AttachmentDisplayName.ShouldBeNull();
        attachment.AttachmentContentType.ShouldBeNull();
        attachment.AttachmentSizeInBytes.ShouldBeNull();
        attachment.AttachmentDuplicateState.ShouldBe("redacted");
        attachment.AttachmentRetryState.ShouldBe("redacted");
        attachment.AttachmentAiContextEligibility.ShouldBe("redacted");
        attachment.AttachmentRedactionState.ShouldBe("redacted");
        ProjectConversationItemStatusFacet retry = attachment.BuildStatusSummary().Facets.Single(static facet => facet.Domain == "retry");
        retry.Health.ShouldBe(ChatBotHealthStatus.Unknown);
        retry.SourceState.ShouldBe("redacted");
    }

    [Fact]
    public async Task ConversationStoreShouldProjectSafetyOutcomeBeforeStorageWithoutFolderReferences()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store);

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await handler.HandleAsync(IntakeCapturedWithAttachments("unsafe.pdf"), Tenant, 8, CorrelationId, TestContext.Current.CancellationToken);
        await store.UpsertAttachmentSafetyOutcomeAsync(
            SafetyOutcome(ProjectConversationAttachmentStatus.Unsafe, 9, "not-eligible", [], "not-retryable", "quarantine-review"),
            TestContext.Current.CancellationToken);

        ProjectConversationItemView attachment = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);

        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Pending);
        attachment.AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Unsafe);
        attachment.AttachmentFolderId.ShouldBeNull();
        attachment.AttachmentFileId.ShouldBeNull();
        attachment.AttachmentAiContextEligibility.ShouldBe("not-eligible");
        attachment.AttachmentAllowedActions.ShouldBeEmpty();
        attachment.SafeNextAction.ShouldBe("quarantine-review");
    }

    [Fact]
    public async Task ConversationStoreShouldRejectCleanSafetyAfterTerminalUnsafeStateWithoutExplicitSupersession()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store);

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await handler.HandleAsync(IntakeCapturedWithAttachments("unsafe.pdf"), Tenant, 8, CorrelationId, TestContext.Current.CancellationToken);
        await store.UpsertAttachmentSafetyOutcomeAsync(
            SafetyOutcome(ProjectConversationAttachmentStatus.Unsafe, 10, "not-eligible", [], "not-retryable", "quarantine-review"),
            TestContext.Current.CancellationToken);
        await store.UpsertAttachmentSafetyOutcomeAsync(
            SafetyOutcome(ProjectConversationAttachmentStatus.Captured, 11, "eligible", ["open-governed-file"], "not-retryable", "none"),
            TestContext.Current.CancellationToken);

        ProjectConversationItemView attachment = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);

        attachment.AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Unsafe);
        attachment.AttachmentAiContextEligibility.ShouldBe("not-eligible");
        attachment.AttachmentAllowedActions.ShouldBeEmpty();
        attachment.SafeNextAction.ShouldBe("quarantine-review");
    }

    [Fact]
    public async Task ConversationStoreShouldPreserveCapturedStorageReferencesWhenSafetyOutcomeArrives()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store);

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await handler.HandleAsync(IntakeCapturedWithAttachments("release-notes.pdf"), Tenant, 8, CorrelationId, TestContext.Current.CancellationToken);
        ProjectConversationAttachmentStorageCandidate candidate = (await store
            .GetAttachmentStorageCandidatesAsync(Tenant, "01ARZ3NDEKTSV4RRFFQ69G5FAY", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();
        await store.UpsertAttachmentStorageOutcomeAsync(
            ProjectConversationAttachmentStorageOutcomeView.Stored(candidate, "folder-current", "file-current", "unique", "not-retryable", "pending-scan", [], 9, CorrelationId),
            TestContext.Current.CancellationToken);
        await store.UpsertAttachmentSafetyOutcomeAsync(
            SafetyOutcome(ProjectConversationAttachmentStatus.Captured, 10, "eligible", ["open-governed-file", "add-to-ai-context"], "not-retryable", "none"),
            TestContext.Current.CancellationToken);

        ProjectConversationItemView attachment = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);

        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentFolderId.ShouldBe("folder-current");
        attachment.AttachmentFileId.ShouldBe("file-current");
        attachment.AttachmentAiContextEligibility.ShouldBe("eligible");
        attachment.AttachmentAllowedActions.ShouldBe(["add-to-ai-context", "open-governed-file"], ignoreOrder: true);
    }

    [Fact]
    public async Task ConversationStoreShouldHideStoredReferencesWhenLaterSafetyOutcomeBlocksAttachment()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store);

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await handler.HandleAsync(IntakeCapturedWithAttachments("release-notes.pdf"), Tenant, 8, CorrelationId, TestContext.Current.CancellationToken);
        ProjectConversationAttachmentStorageCandidate candidate = (await store
            .GetAttachmentStorageCandidatesAsync(Tenant, "01ARZ3NDEKTSV4RRFFQ69G5FAY", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();
        await store.UpsertAttachmentStorageOutcomeAsync(
            ProjectConversationAttachmentStorageOutcomeView.Stored(candidate, "folder-current", "file-current", "unique", "not-retryable", "pending-scan", [], 9, CorrelationId),
            TestContext.Current.CancellationToken);
        await store.UpsertAttachmentSafetyOutcomeAsync(
            SafetyOutcome(ProjectConversationAttachmentStatus.Unsafe, 10, "not-eligible", [], "not-retryable", "quarantine-review"),
            TestContext.Current.CancellationToken);

        ProjectConversationItemView attachment = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);

        attachment.AttachmentStorageStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Unsafe);
        attachment.AttachmentFolderId.ShouldBeNull();
        attachment.AttachmentFileId.ShouldBeNull();
        attachment.AttachmentAiContextEligibility.ShouldBe("not-eligible");
        attachment.AttachmentAllowedActions.ShouldBeEmpty();
    }

    [Fact]
    public async Task ConversationStoreShouldAllowExplicitTerminalSafetySupersession()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store);

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await handler.HandleAsync(IntakeCapturedWithAttachments("release-notes.pdf"), Tenant, 8, CorrelationId, TestContext.Current.CancellationToken);
        ProjectConversationAttachmentStorageCandidate candidate = (await store
            .GetAttachmentStorageCandidatesAsync(Tenant, "01ARZ3NDEKTSV4RRFFQ69G5FAY", TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();
        await store.UpsertAttachmentStorageOutcomeAsync(
            ProjectConversationAttachmentStorageOutcomeView.Stored(candidate, "folder-current", "file-current", "unique", "not-retryable", "pending-scan", [], 9, CorrelationId),
            TestContext.Current.CancellationToken);
        await store.UpsertAttachmentSafetyOutcomeAsync(
            SafetyOutcome(ProjectConversationAttachmentStatus.Unsafe, 10, "not-eligible", [], "not-retryable", "quarantine-review"),
            TestContext.Current.CancellationToken);
        await store.UpsertAttachmentSafetyOutcomeAsync(
            SafetyOutcome(ProjectConversationAttachmentStatus.Captured, 11, "eligible", ["open-governed-file", "add-to-ai-context"], "not-retryable", "none", supersedesTerminalState: true),
            TestContext.Current.CancellationToken);

        ProjectConversationItemView attachment = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);

        attachment.AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        attachment.AttachmentFolderId.ShouldBe("folder-current");
        attachment.AttachmentFileId.ShouldBe("file-current");
        attachment.AttachmentAiContextEligibility.ShouldBe("eligible");
        attachment.AttachmentAllowedActions.ShouldBe(["add-to-ai-context", "open-governed-file"], ignoreOrder: true);
    }

    [Fact]
    public async Task ConversationStoreShouldScopeSafetyOutcomesToMatchingProviderAttachmentOrdinal()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store);

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await handler.HandleAsync(
            IntakeCaptured() with
            {
                AttachmentReferences =
                [
                    new MailboxAttachmentReference("graph-attachment-duplicate", "first.pdf", "application/pdf", 4096),
                    new MailboxAttachmentReference("graph-attachment-duplicate", "second.pdf", "application/pdf", 4096),
                ],
            },
            Tenant,
            8,
            CorrelationId,
            TestContext.Current.CancellationToken);
        await store.UpsertAttachmentSafetyOutcomeAsync(
            SafetyOutcome(
                ProjectConversationAttachmentStatus.Retryable,
                9,
                "not-eligible",
                [],
                "retryable",
                "retry-scan",
                ordinal: 1,
                providerAttachmentId: "graph-attachment-duplicate",
                reasonCode: "attachment_scan_retryable"),
            TestContext.Current.CancellationToken);

        ProjectConversationItemView[] attachments = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Where(static item => item.Kind == ProjectConversationItemKind.Attachment)
            .OrderBy(static item => item.AttachmentDisplayName, StringComparer.Ordinal)
            .ToArray();

        attachments.Length.ShouldBe(2);
        attachments[0].AttachmentDisplayName.ShouldBe("first.pdf");
        attachments[0].AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Pending);
        attachments[0].AttachmentAiContextEligibility.ShouldBe("not-evaluated");
        attachments[1].AttachmentDisplayName.ShouldBe("second.pdf");
        attachments[1].AttachmentScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Retryable);
        attachments[1].AttachmentRetryState.ShouldBe("retryable");
        attachments[1].SafeNextAction.ShouldBe("retry-scan");
    }

    [Fact]
    public async Task ConversationStoreShouldStripPathSegmentsFromAttachmentDisplayNames()
    {
        InMemoryProjectConversationProjectionStore store = new();
        AssociationProjectionHandler handler = new(new InMemoryAssociationProjectionStore(), new FixedClock(), store);

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await handler.HandleAsync(
            IntakeCapturedWithAttachments("C:\\mailbox-cache\\invoice.pdf"),
            Tenant,
            9,
            CorrelationId,
            TestContext.Current.CancellationToken);

        ProjectConversationItemView attachment = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Attachment);

        attachment.AttachmentDisplayName.ShouldBe("invoice.pdf");
    }

    [Fact]
    public async Task ConversationStoreShouldMaterializeParticipantArrivingAfterAssociationAndRejectStaleReplay()
    {
        InMemoryProjectConversationProjectionStore store = new();

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await store.UpsertParticipantResolutionAsync(ParticipantView(5) with { SafeDisplayLabel = "Current participant" }, TestContext.Current.CancellationToken);
        await store.UpsertParticipantResolutionAsync(ParticipantView(4) with { SafeDisplayLabel = "Stale participant" }, TestContext.Current.CancellationToken);

        ProjectConversationItemView participant = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Single(static item => item.Kind == ProjectConversationItemKind.Participant);
        participant.ActorLabel.ShouldBe("Current participant");
        participant.SourceVersion.ShouldBe(5);
    }

    [Fact]
    public async Task ParticipantItemsShouldFollowParentCorrectionSafeNextActionWithoutUpdatingParentSourceVersion()
    {
        InMemoryProjectConversationProjectionStore store = new();

        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);
        await store.UpsertParticipantResolutionAsync(ParticipantView(5), TestContext.Current.CancellationToken);
        await store.UpsertAsync(
            Item(AssociationId, 3, DetectedAt.AddMinutes(1)) with
            {
                LifecycleState = LifecycleState.CorrectionDelayed,
                SafeNextAction = "wait-for-propagation",
            },
            TestContext.Current.CancellationToken);

        ProjectConversationPage page = await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken);
        ProjectConversationItemView parent = page.Items.Single(static item => item.Kind != ProjectConversationItemKind.Participant);
        ProjectConversationItemView participant = page.Items.Single(static item => item.Kind == ProjectConversationItemKind.Participant);
        parent.SourceVersion.ShouldBe(3);
        participant.SourceVersion.ShouldBe(5);
        participant.LifecycleState.ShouldBe(LifecycleState.CorrectionDelayed);
        participant.SafeNextAction.ShouldBe("wait-for-propagation");
    }

    [Fact]
    public async Task ConversationStoreShouldMaterializeExternalUnresolvedAndRestrictedParticipantClasses()
    {
        InMemoryProjectConversationProjectionStore store = new();
        await store.UpsertAsync(Item(AssociationId, 2, DetectedAt), TestContext.Current.CancellationToken);

        await store.UpsertParticipantResolutionAsync(
            ParticipantView(5) with
            {
                SourceParticipantId = "01ARZ3NDEKTSV4RRFFQ69G5FC1",
                PartyId = "tenant-alpha:parties:party-002",
                DisplayKind = ProjectConversationParticipantDisplayKind.ExternalParticipant,
                SafeDisplayLabel = "External participant",
            },
            TestContext.Current.CancellationToken);
        await store.UpsertParticipantResolutionAsync(
            ParticipantView(6) with
            {
                SourceParticipantId = "01ARZ3NDEKTSV4RRFFQ69G5FC2",
                PartyId = null,
                Status = ParticipantResolutionStatus.Unresolved,
                Reason = ParticipantResolutionBlockedReason.NotFound,
                AllowedReviewActions = [ParticipantReviewAction.Link, ParticipantReviewAction.CreatePending],
                DisplayKind = ProjectConversationParticipantDisplayKind.UnresolvedParticipant,
                SafeDisplayLabel = "Unresolved participant",
            },
            TestContext.Current.CancellationToken);
        await store.UpsertParticipantResolutionAsync(
            ParticipantView(7) with
            {
                SourceParticipantId = "01ARZ3NDEKTSV4RRFFQ69G5FC3",
                Status = ParticipantResolutionStatus.Resolved,
                Reason = ParticipantResolutionBlockedReason.RestrictedParty,
                DisplayKind = ProjectConversationParticipantDisplayKind.RestrictedParticipant,
                SafeDisplayLabel = "Restricted participant",
            },
            TestContext.Current.CancellationToken);

        ProjectConversationItemView[] participants = (await store.ReadPageAsync(Tenant, "project-001", null, 25, TestContext.Current.CancellationToken))
            .Items
            .Where(static item => item.Kind == ProjectConversationItemKind.Participant)
            .ToArray();

        participants.Single(static item => item.SourceParticipantId == "01ARZ3NDEKTSV4RRFFQ69G5FC1").ActorKind.ShouldBe(ProjectConversationActorKind.ExternalParticipant);
        ProjectConversationItemView unresolved = participants.Single(static item => item.SourceParticipantId == "01ARZ3NDEKTSV4RRFFQ69G5FC2");
        unresolved.ActorKind.ShouldBe(ProjectConversationActorKind.UnresolvedParticipant);
        unresolved.ParticipantAllowedReviewActions.ShouldBe([ParticipantReviewAction.Link, ParticipantReviewAction.CreatePending], ignoreOrder: false);
        ProjectConversationItemView restricted = participants.Single(static item => item.SourceParticipantId == "01ARZ3NDEKTSV4RRFFQ69G5FC3");
        restricted.ActorKind.ShouldBe(ProjectConversationActorKind.RestrictedParticipant);
        restricted.ParticipantBlockedReason.ShouldBe(ParticipantResolutionBlockedReason.RestrictedParty);
    }

    [Fact]
    public async Task ProjectConversationEndpointShouldReturnEmptyStateOnlyForAuthorizedEmptyProject()
    {
        using WebApplicationFactory<Program> factory = CreateFactoryWithProjectClaim("empty-project");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage authorized = await client
            .GetAsync("/api/v1/projects/empty-project/conversation", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string authorizedBody = await authorized.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        authorized.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(authorizedBody);
        document.RootElement.GetProperty("projectId").GetString().ShouldBe("empty-project");
        document.RootElement.GetProperty("status").GetString().ShouldBe("empty");
        document.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);

        using HttpResponseMessage unauthorized = await client
            .GetAsync("/api/v1/projects/other-project/conversation", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        unauthorized.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static AssociationNotification Notification(long sourceVersion)
        => new(
            Tenant,
            AssociationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            "project-001",
            "Project One",
            LifecycleState.Associated,
            AssociationScoringOutcome.AutoAssociated,
            AssociationThresholdBand.Auto,
            0.9,
            [new AssociationCandidate("project-001", "Project One", 0.9, 1, [AssociationReasonCode.ExplicitProjectIdentifierMatched], [], [], true)],
            [],
            "association-thresholds.m0.default.v1",
            "association-deterministic.kernel.m0.v1",
            "metadata_only",
            "collaboration_input",
            sourceVersion,
            DetectedAt,
            CorrelationId);

    private static ProjectConversationItemView Item(string itemId, long sourceVersion, DateTimeOffset occurredAt)
        => new(
            Tenant,
            "project-001",
            "Project One",
            itemId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            ProjectConversationItemKind.EmailDerived,
            ProjectConversationActorKind.Mailbox,
            "Mailbox event",
            occurredAt,
            LifecycleState.Associated,
            AssociationThresholdBand.Auto,
            0.9,
            AssociationId,
            "controlled-mailbox-001",
            null,
            null,
            "conversation-001",
            "thread-001",
            null,
            null,
            null,
            null,
            null,
            AssociationCandidateView.MailboxSourceProvenance,
            "metadata_only",
            "collaboration_input",
            ProjectConversationItemView.CurrentSchemaVersion,
            sourceVersion,
            CorrelationId);

    private static TaskIntentRecord TaskIntentRecord(long sourceVersion)
        => new(
            "task-intent:abc",
            Tenant,
            "project-001",
            "graph-message-001",
            "party-001",
            "authorized conversation item requests action",
            ProjectConversationDetectedActionKind.RequestAction,
            [new TaskIntentSourceEvidenceOffset("message:offset:001", 10, 40, "safe-token")],
            "chatbot.task-intent.kernel.m0.v1",
            0.82,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            TaskIntentState.Captured,
            "chatbot.task-intent-record.v1",
            "task_intent_captured",
            "authorized-project-conversation",
            "metadata_only",
            "collaboration_input",
            sourceVersion,
            CorrelationId,
            "policy-001",
            ConversionReadinessBlocked: false,
            SafeNextAction: "review-task-intent-action");

    private static MailboxMessageIntakeCaptured IntakeCaptured()
        => new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "graph-message-001",
            "<internet-message-001@example.test>",
            "conversation-001",
            "thread-001",
            "controlled-mailbox-001",
            new MailboxParticipantIdentity("sender-safe-label", "redacted"),
            [],
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 31, 23, 58, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 31, 23, 57, 0, TimeSpan.Zero),
            [],
            "UTC",
            "opaque-graph-delta-context",
            "m365-mailbox-intake",
            "association-deterministic.kernel.m0.v1",
            "metadata_only",
            "collaboration_input",
            1);

    private static MailboxMessageIntakeCaptured IntakeCapturedWithAttachments(string attachmentName = "invoice.pdf")
        => IntakeCaptured() with
        {
            AttachmentReferences =
            [
                new MailboxAttachmentReference("graph-attachment-001", attachmentName, "application/pdf", 4096),
            ],
        };

    private static ProjectConversationAttachmentSafetyOutcomeView SafetyOutcome(
        ProjectConversationAttachmentStatus status,
        long sourceVersion,
        string aiContextEligibility,
        IReadOnlyList<string> allowedActions,
        string retryState,
        string safeNextAction,
        int ordinal = 0,
        string providerAttachmentId = "graph-attachment-001",
        string reasonCode = "attachment_policy_quarantined",
        bool supersedesTerminalState = false)
        => new(
            Tenant,
            "project-001",
            AssociationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            providerAttachmentId,
            ordinal,
            status,
            aiContextEligibility,
            allowedActions,
            retryState,
            safeNextAction,
            reasonCode,
            sourceVersion,
            CorrelationId,
            "quarantine",
            supersedesTerminalState);

    private static ProjectConversationSourceEmailView SourceEmail(long sourceVersion, string providerMessageId)
        => ProjectConversationSourceEmailView.FromIntake(
            Tenant,
            IntakeCaptured() with { ProviderMessageId = providerMessageId },
            sourceVersion,
            CorrelationId);

    private static ParticipantResolutionView ParticipantView(long sourceVersion)
        => new(
            Tenant,
            "01ARZ3NDEKTSV4RRFFQ69G5FBP",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "controlled-mailbox-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FBQ",
            "tenant-alpha:parties:party-001",
            ParticipantResolutionStatus.Resolved,
            null,
            [],
            ProjectConversationParticipantDisplayKind.InternalParticipant,
            "Internal participant",
            "mailbox:intake:sender",
            "evidence-sha256",
            ParticipantResolutionView.CurrentSchemaVersion,
            ParticipantResolutionView.MailboxSourceProvenance,
            ParticipantResolutionView.CurrentDerivationKernelVersion,
            ParticipantResolutionView.MetadataOnlyRedactionState,
            ParticipantResolutionView.CollaborationRetentionClass,
            sourceVersion,
            CorrelationId,
            DetectedAt.AddMinutes(1),
            DetectedAt.AddMinutes(1));

    private static PublishedMailboxIntakeEvent PublishedIntake(long sequenceNumber)
        => new(
            Tenant,
            ChatBotEventStore.DomainName,
            MailboxIntakeProjectionTranslator.IntakeCapturedEventType,
            sequenceNumber,
            CorrelationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "graph-message-001",
            "<internet-message-001@example.test>",
            "conversation-001",
            "thread-001",
            "controlled-mailbox-001",
            new MailboxParticipantIdentity("sender-safe-label", "redacted"),
            [],
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 31, 23, 58, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 31, 23, 57, 0, TimeSpan.Zero),
            [],
            "UTC",
            "opaque-graph-delta-context",
            AssociationCandidateView.MailboxSourceProvenance,
            "association-deterministic.kernel.m0.v1",
            "metadata_only",
            "collaboration_input",
            1);

    private static PublishedApprovalEvent ApprovalPublished(long sourceVersion)
        => new(
            Tenant,
            ApprovalProjectionTranslator.ApprovalDomain,
            "approval-aggregate-001",
            sourceVersion,
            DetectedAt.AddMinutes(sourceVersion),
            CorrelationId,
            "project-001",
            "approval-001",
            ApprovalEventKind.Request,
            ApprovalStatus.Pending,
            "proposal-001",
            "graph-message-001",
            "decision:source:001",
            "requester-001",
            "human",
            DetectedAt.AddMinutes(8),
            "SendExternalReply",
            "allowlist.v1",
            RiskClass.High,
            ["externally-visible"],
            "policy-snapshot-001",
            "redacted",
            ["evidence:summary:001"],
            [ApprovalEvidenceFreshness.Expired],
            ["project:project-001"],
            ["recipient:external:001"],
            "on-behalf-of",
            "metadata_only",
            "redacted",
            SafeNextAction: "await-approval");

    private static PublishedFailureStateEvent FailurePublished(long sourceVersion)
        => new(
            Tenant,
            FailureStateProjectionTranslator.FailureStateDomain,
            "operation-aggregate-001",
            sourceVersion,
            DetectedAt.AddMinutes(sourceVersion),
            CorrelationId,
            "project-001",
            FailureStateKind.RetryQueued,
            FailureStatus.Retryable,
            ChatBotMessageCodes.RetryQueued,
            "operation-001",
            SourceConversationItemId: "decision:source:001",
            AssociationId: AssociationId,
            SourceMessageId: "graph-message-001",
            WorkflowInstanceId: "workflow-001",
            TaskId: "task-001",
            AuditOperationId: "audit-001",
            AuditStatus: "available",
            ClientAction: ChatBotMessageNextActions.RetryLater,
            FailureCategory: "projection",
            FailureScope: "project-conversation",
            FailureReasonCode: "projection-retryable",
            Retryable: true,
            RetryCount: 1,
            MaxRetryCount: 3,
            NextRetryAtUtc: DetectedAt.AddMinutes(sourceVersion + 5),
            RetryOperationId: "retry-operation-001",
            DuplicateSafetyState: "duplicate-safe",
            DependencyName: "projection",
            SafeNextAction: ChatBotMessageNextActions.RetryLater);

    private static WebApplicationFactory<Program> CreateFactoryWithProjectClaim(string projectId)
    {
        InMemoryProjectConversationProjectionStore conversationStore = new();
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.AddSingleton<IProjectConversationProjectionStore>(conversationStore);
                services.AddSingleton<IStartupFilter>(new TestPrincipalStartupFilter(projectId));
            }));
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 6, 1, 1, 0, 0, TimeSpan.Zero);
    }

    private sealed class FixedMailboxContentSource(MailboxAttachmentContentResult result) : IMailboxAttachmentContentSource
    {
        public ValueTask<MailboxAttachmentContentResult> FetchAttachmentContentAsync(
            MailboxAttachmentContentRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CapturingFolderStore : IFolderStore
    {
        public List<StoreMailboxAttachmentRequest> Requests { get; } = [];

        public ValueTask<MailboxAttachmentStorageResult> StoreMailboxAttachmentAsync(
            StoreMailboxAttachmentRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            return ValueTask.FromResult(MailboxAttachmentStorageResult.Succeeded(new StoredMailboxAttachmentReference(
                $"folder-{request.ProjectId}",
                $"file-{request.ProviderAttachmentId}-{request.Ordinal}",
                "unique",
                "not-retryable",
                "pending-scan",
                [],
                $"operation-{request.ProviderAttachmentId}-{request.Ordinal}",
                $"idempotency-{request.ProviderAttachmentId}-{request.Ordinal}")));
        }
    }

    private sealed class TestPrincipalStartupFilter(string projectId) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (context, continuation) =>
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim("sub", "actor-001"),
                            new Claim("eventstore:tenant", Tenant),
                            new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, projectId),
                        ],
                        "test"));
                    await continuation().ConfigureAwait(false);
                });
                next(app);
            };
    }
}
