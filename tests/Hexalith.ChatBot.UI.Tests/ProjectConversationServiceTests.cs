using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.ProjectConversation;

using Shouldly;

using ContractApprovalDecisionKind = Hexalith.ChatBot.Contracts.Enums.ApprovalDecisionKind;
using ChatBotSurfaceOrigin = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin;
using DecideAiActionApproval = Hexalith.ChatBot.Contracts.Commands.DecideAiActionApproval;
using IChatBotCommand = Hexalith.ChatBot.Contracts.Commands.IChatBotCommand;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ProjectConversationServiceTests
{
    [Fact]
    public async Task ServiceShouldReadProjectConversationThroughClientAndMapMetadataOnlyItems()
    {
        FakeChatBotClient client = new();
        ProjectConversationService service = new(client);

        ProjectConversationModel conversation = await service.GetProjectConversationAsync("project-001", cancellationToken: TestContext.Current.CancellationToken);

        client.LastProjectId.ShouldBe("project-001");
        conversation.ProjectDisplayName.ShouldBe("Authorized Project");
        conversation.Status.ShouldBe("Current");
        ProjectConversationItemModel item = conversation.Items.ShouldHaveSingleItem();
        item.ActorKind.ShouldBe("SystemDecision");
        item.DecisionLabel.ShouldBe("Associate");
        item.SourceMailboxId.ShouldBe("controlled-mailbox-001");
        item.SourceConversationId.ShouldBe("conversation-001");
        item.SourceProviderMessageId.ShouldBe("graph-message-001");
        item.InternetMessageId.ShouldBe("<internet-message-001@example.test>");
        item.SourceReceivedAtUtc.ShouldBe(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
        item.SourceSentAtUtc.ShouldBe(new DateTimeOffset(2026, 5, 31, 23, 58, 0, TimeSpan.Zero));
        item.SourceCreatedAtUtc.ShouldBe(new DateTimeOffset(2026, 5, 31, 23, 57, 0, TimeSpan.Zero));
        item.SourceTimezone.ShouldBe("UTC");
        item.SourceProvenanceDisplayToken.ShouldBe("Microsoft 365 mailbox");
        item.DecisionKind.ShouldBe("associate");
        item.DecisionActorType.ShouldBe("Human");
        item.DecisionNoteRedactionState.ShouldBe("redacted");
        item.CorrectionKind.ShouldBe("project-reassignment");
        item.CorrectionRationaleRedactionState.ShouldBe("redacted");
        item.EvidenceReferenceSummary.ShouldBe(["mailbox:intake:subject"], ignoreOrder: false);
        item.RequiredStoreKeys.ShouldBe(["project-conversation", "participants"], ignoreOrder: false);
        item.PropagationProgressNumerator.ShouldBe(1);
        item.PropagationProgressDenominator.ShouldBe(2);
        item.IsCorrectedContextStale.ShouldBe(true);
        item.StatusSummary.ShouldNotBeNull().Facets.Select(static facet => facet.Domain).ShouldBe(
            ["association", "command", "task"],
            ignoreOrder: false);
        ProjectConversationItemStatusFacetModel commandFacet = item.StatusSummary.Facets.Single(static facet => facet.Domain == "command");
        commandFacet.Health.ShouldBe("degraded");
        commandFacet.ProjectionStatus.ShouldBe("accepted-projection-pending");
        commandFacet.AuditStatus.ShouldBe("reconciling");
        commandFacet.SafeNextAction.ShouldBe("wait-for-projection");
        item.Classification.ShouldNotBeNull().Kind.ShouldBe("actionable");
        item.DetectedIntent.ShouldNotBeNull().ActionKind.ShouldBe("request-decision");
        item.DetectedIntent.SourceEvidenceIds.ShouldBe(["mailbox:intake:subject"], ignoreOrder: false);
        item.DetectedIntent.SafeNextAction.ShouldBe("review-association");
        item.DetectedIntent.MessageCode.ShouldBe("detected_intent_request_decision");
        item.DetectedIntent.RedactionState.ShouldBe("metadata_only");
        item.ReviewHistory.ShouldHaveSingleItem().ActionCode.ShouldBe("classification-projected");

        client.ReturnParticipant = true;
        ProjectConversationModel participantConversation = await service.GetProjectConversationAsync("project-001", cancellationToken: TestContext.Current.CancellationToken);
        ProjectConversationItemModel participant = participantConversation.Items.Single(static model => model.IsParticipant);
        participant.ParticipantStatus.ShouldBe("Unresolved");
        participant.ParticipantDisplayKind.ShouldBe("UnresolvedParticipant");
        participant.ParticipantAllowedReviewActions.ShouldBe(["Link", "CreatePending"], ignoreOrder: false);
        participant.ParticipantEvidenceReference.ShouldBe("mailbox:intake:sender");

        client.ReturnAttachment = true;
        ProjectConversationModel attachmentConversation = await service.GetProjectConversationAsync("project-001", cancellationToken: TestContext.Current.CancellationToken);
        ProjectConversationItemModel attachment = attachmentConversation.Items.Single(static model => model.IsAttachment);
        attachment.ActorKind.ShouldBe("MailboxAttachment");
        attachment.SourceProviderAttachmentId.ShouldBe("graph-attachment-001");
        attachment.AttachmentDisplayName.ShouldBe("invoice.pdf");
        attachment.AttachmentContentType.ShouldBe("application/pdf");
        attachment.AttachmentCaptureStatus.ShouldBe("Captured");
        attachment.AttachmentStorageStatus.ShouldBe("Pending");
        attachment.AttachmentScanStatus.ShouldBe("Pending");
        attachment.AttachmentAllowedActions.ShouldBeEmpty();

        client.ReturnAttachment = false;
        client.ReturnStoredAttachment = true;
        ProjectConversationModel storedAttachmentConversation = await service.GetProjectConversationAsync("project-001", cancellationToken: TestContext.Current.CancellationToken);
        ProjectConversationItemModel storedAttachment = storedAttachmentConversation.Items.Single(static model => model.IsAttachment);
        storedAttachment.AttachmentStorageStatus.ShouldBe("Captured");
        storedAttachment.AttachmentScanStatus.ShouldBe("Captured");
        storedAttachment.AttachmentFolderId.ShouldBe("folder-reference-001");
        storedAttachment.AttachmentFileId.ShouldBe("file-reference-001");
        storedAttachment.AttachmentDuplicateState.ShouldBe("unique");
        storedAttachment.AttachmentRetryState.ShouldBe("not-retryable");
        storedAttachment.AttachmentAiContextEligibility.ShouldBe("eligible");
        storedAttachment.AttachmentAllowedActions.ShouldBe(["open-governed-file", "add-to-ai-context"], ignoreOrder: false);

        client.ReturnApproval = true;
        ProjectConversationModel approvalConversation = await service.GetProjectConversationAsync("project-001", cancellationToken: TestContext.Current.CancellationToken);
        ProjectConversationItemModel approval = approvalConversation.Items.Single(static model => model.IsApprovalEvent);
        approval.Kind.ShouldBe("ApprovalEvent");
        approval.ActorKind.ShouldBe("ApprovalSystem");
        approval.ApprovalEventKind.ShouldBe("request");
        approval.ApprovalStatus.ShouldBe("pending");
        approval.ApprovalRiskClass.ShouldBe("high");
        approval.ApprovalEvidenceFreshnessStates.ShouldBe(["expired"], ignoreOrder: false);
        approval.ApprovalPolicySnapshotVisibility.ShouldBe("redacted");
        approval.ApprovalDisabledReason.ShouldBe("evidence-expired");
        approval.ApprovalActionSummaryRedactionState.ShouldBe("redacted");

        client.ReturnFailure = true;
        ProjectConversationModel failureConversation = await service.GetProjectConversationAsync("project-001", cancellationToken: TestContext.Current.CancellationToken);
        ProjectConversationItemModel failure = failureConversation.Items.Single(static model => model.IsFailureState);
        failure.Kind.ShouldBe("FailureState");
        failure.ActorKind.ShouldBe("SystemStatus");
        failure.FailureStateKind.ShouldBe("retry-queued");
        failure.FailureStatus.ShouldBe("retryable");
        failure.MessageCatalogCode.ShouldBe("retry_queued");
        failure.MessageCatalogVersion.ShouldBe("chatbot.message-catalog.v1");
        failure.MessageDetailVisibility.ShouldBe("metadata_only");
        failure.BlockedReason.ShouldBe("projection-pending");
        failure.Retryable.ShouldBe(true);
        failure.RetryCount.ShouldBe(1);
        failure.MaxRetryCount.ShouldBe(3);
        failure.OperationId.ShouldBe("operation-001");
        failure.AuditOperationId.ShouldBe("audit-001");
        failure.DuplicateSafetyState.ShouldBe("duplicate-safe");

        client.ReturnAiOutcome = true;
        ProjectConversationModel aiConversation = await service.GetProjectConversationAsync("project-001", cancellationToken: TestContext.Current.CancellationToken);
        ProjectConversationItemModel ai = aiConversation.Items.Single(static model => model.IsAiOutcome);
        ai.Kind.ShouldBe("AiOutcome");
        ai.ActorKind.ShouldBe("AiActor");
        ai.AiOutcomeKind.ShouldBe("proposal");
        ai.AiOutcomeStatus.ShouldBe("proposed");
        ai.AiActorType.ShouldBe("ai");
        ai.AiProposalId.ShouldBe("proposal-001");
        ai.AiRiskClass.ShouldBe("approval-required");
        ai.AiRiskActionClasses.ShouldBe(["invokes-tools"], ignoreOrder: false);
        ai.AiAuthorizedContextReferences.ShouldBe(["evidence:summary:001"], ignoreOrder: false);
        ai.AiSafeNextAction.ShouldBe("review-ai-action");
        ai.SupersedesAiOutcomeId.ShouldBe("ai:proposal-000:proposal:9");
        ai.AiSummaryProvenance.ShouldNotBeNull().GeneratedBy.ShouldBe("ai-model.v1");
        ai.AiSummaryProvenance.SourceEvidenceIds.ShouldBe(["evidence:summary:001"], ignoreOrder: false);
    }

    [Fact]
    public async Task ServiceShouldPassOpaqueCursorAndMapCursorPageMetadata()
    {
        FakeChatBotClient client = new()
        {
            ResponseNextCursor = "opaque-next-cursor",
            ResponseHasMore = true,
            ResponsePageSize = 10,
        };
        ProjectConversationService service = new(client);

        ProjectConversationModel conversation = await service.GetProjectConversationAsync(
            "project-001",
            "opaque-input-cursor",
            TestContext.Current.CancellationToken);

        client.LastProjectId.ShouldBe("project-001");
        client.LastCursor.ShouldBe("opaque-input-cursor");
        client.LastPageSize.ShouldBe(25);
        conversation.NextCursor.ShouldBe("opaque-next-cursor");
        conversation.HasMore.ShouldBeTrue();
        conversation.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task ServiceShouldSubmitApprovalDecisionThroughClientWithServerOwnedMetadata()
    {
        FakeChatBotClient client = new() { ReturnApproval = true };
        ProjectConversationService service = new(client);
        ProjectConversationItemModel approval = (await service
                .GetProjectConversationAsync("project-001", cancellationToken: TestContext.Current.CancellationToken))
            .Items
            .Single(static model => model.IsApprovalEvent);

        CommandSubmissionResponse response = await service.SubmitApprovalDecisionAsync(
            approval,
            ContractApprovalDecisionKind.RequestRevision,
            rationaleRedactionState: "redacted",
            cancellationToken: TestContext.Current.CancellationToken);

        response.CommandId.ShouldBe("accepted-command-001");
        client.LastSubmitCorrelationId.ShouldBe(approval.CorrelationId);
        client.LastSubmitOrigin.ShouldBe(ChatBotSurfaceOrigin.Ui);
        DecideAiActionApproval command = client.LastSubmittedCommand.ShouldBeOfType<DecideAiActionApproval>();
        command.ProjectId.ShouldBe("project-001");
        command.ApprovalId.ShouldBe("approval-001");
        command.ProposalId.ShouldBe("proposal-001");
        command.SourceMessageId.ShouldBe("graph-message-001");
        command.Decision.ShouldBe(ContractApprovalDecisionKind.RequestRevision);
        command.ExpectedApprovalSourceVersion.ShouldBe(approval.SourceVersion);
        command.CorrelationId.ShouldBe(approval.CorrelationId);
        command.DecisionId.ShouldBe("approval-decision:approval-001:RequestRevision:8");
        command.RationaleRedactionState.ShouldBe("redacted");
    }

    [Fact]
    public async Task ServiceShouldReadWhyPanelThroughRoutingStatusAndMapSafeEvidence()
    {
        FakeChatBotClient client = new();
        ProjectConversationService service = new(client);

        ProjectAssociationWhyPanelModel panel = await service.GetAssociationWhyPanelAsync(
            "project-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        client.LastAssociationId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        panel.ProjectId.ShouldBe("project-001");
        panel.DecisionActorId.ShouldBe("actor-safe");
        panel.DecisionActorType.ShouldBe("human");
        panel.ThresholdPolicyVersion.ShouldBe("association-thresholds.m0.default.v1");
        panel.KernelVersion.ShouldBe("association-deterministic.kernel.m0.v1");

        // Panel-level enum metadata must surface stable wire tokens (AC1 references the
        // auto/ambiguous/fail-closed band), not PascalCase .NET enum names.
        panel.LifecycleState.ShouldBe("Associated");
        panel.Outcome.ShouldBe("auto-associated");
        panel.ThresholdBand.ShouldBe("auto");
        panel.SourceProvenance.ShouldBe("m365-mailbox-intake");
        panel.RedactionState.ShouldBe("metadata_only");
        panel.SupersedingCorrectionId.ShouldBe("correction-002");
        panel.CorrectionPanelAvailable.ShouldBeTrue();
        ProjectAssociationWhyEvidenceModel evidence = panel.Evidence.ShouldHaveSingleItem();
        evidence.SignalClass.ShouldBe("explicit-project-identifier");
        evidence.DisplayToken.ShouldBe("mailbox:metadata");
        evidence.VisibilityState.ShouldBe("available");
        evidence.RedactionState.ShouldBe("metadata_only");
        evidence.FreshnessState.ShouldBe("fresh");
        evidence.ConfidenceContribution.ShouldBe(0.42);
    }

    [Fact]
    public async Task ServiceShouldMapTaskIntentReviewWithoutParsingSourceInBrowser()
    {
        FakeChatBotClient client = new();
        ProjectConversationService service = new(client);

        TaskIntentReviewModel review = await service.GetTaskIntentReviewAsync(
            "project-001",
            "task-intent:abc",
            TestContext.Current.CancellationToken);

        client.LastTaskIntentId.ShouldBe("task-intent:abc");
        review.Available.ShouldBeTrue();
        review.SourceMessageContent.ShouldBe("authorized body for review");
        review.AvailableTransitions.Select(static transition => transition.Transition).ShouldBe(
            ["convert", "duplicate"],
            ignoreOrder: false);
        review.AvailableTransitions.Single(static transition => transition.Transition == "duplicate").RequiresPredecessorTaskIntentId
            .ShouldBeTrue();
        review.AuditHistory.ShouldHaveSingleItem().OperationId.ShouldBe("audit-transition-001");
        review.CurrentState.ShouldBe("Captured");
        review.RedactionState.ShouldBe("Metadata_only");
    }

    [Fact]
    public void WhyPanelReducersShouldIgnoreLateResponsesForAnotherProjectOrAssociation()
    {
        ProjectConversationState loading = ProjectConversationReducers.ReduceOpenWhyPanel(
            new ProjectConversationState(false, null, null),
            new OpenProjectAssociationWhyPanelAction("project-current", "assoc-current"));
        ProjectAssociationWhyPanelModel stalePanel = new(
            "project-old",
            "assoc-old",
            "intake",
            "mailbox",
            "conversation",
            null,
            "Associated",
            "AutoAssociated",
            "auto",
            0.9,
            "policy",
            "kernel",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            null,
            null,
            "m365-mailbox-intake",
            "metadata_only",
            "schema",
            1,
            "correlation",
            [],
            [],
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            false,
            "none");

        ProjectConversationState afterLateLoad = ProjectConversationReducers.ReduceWhyPanelLoaded(
            loading,
            new ProjectAssociationWhyPanelLoadedAction("project-old", "assoc-old", stalePanel));
        ProjectConversationState afterLateFailure = ProjectConversationReducers.ReduceWhyPanelFailed(
            loading,
            new ProjectAssociationWhyPanelFailedAction("project-old", "assoc-old", "authorization_denied"));

        afterLateLoad.ShouldBe(loading);
        afterLateFailure.ShouldBe(loading);
    }

    private sealed class FakeChatBotClient : IChatBotClient
    {
        public string? LastProjectId { get; private set; }

        public string? LastCursor { get; private set; }

        public int LastPageSize { get; private set; }

        public string? LastAssociationId { get; private set; }

        public string? LastTaskIntentId { get; private set; }

        public IChatBotCommand? LastSubmittedCommand { get; private set; }

        public string? LastSubmitCorrelationId { get; private set; }

        public ChatBotSurfaceOrigin? LastSubmitOrigin { get; private set; }

        public bool ReturnParticipant { get; set; }

        public bool ReturnAttachment { get; set; }

        public bool ReturnStoredAttachment { get; set; }

        public bool ReturnApproval { get; set; }

        public bool ReturnFailure { get; set; }

        public bool ReturnAiOutcome { get; set; }

        public string? ResponseNextCursor { get; set; }

        public bool ResponseHasMore { get; set; }

        public int ResponsePageSize { get; set; } = 25;

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
        {
            LastProjectId = projectId;
            LastCursor = cursor;
            LastPageSize = pageSize;
            return Task.FromResult(new ProjectConversationResponse
            {
                ProjectId = projectId,
                ProjectDisplayName = "Authorized Project",
                Status = ProjectConversationReadStatus.Current,
                ConversationState = LifecycleState.Associated,
                Items = ProjectConversationItems(projectId),
                Page = new ProjectConversationCursorPage
                {
                    NextCursor = ResponseNextCursor,
                    HasMore = ResponseHasMore,
                    PageSize = ResponsePageSize,
                },
                SourceProvenance = ProjectConversationResponseSourceProvenance.M365MailboxIntake,
                RedactionState = ProjectConversationResponseRedactionState.Metadata_only,
                RetentionClass = ProjectConversationResponseRetentionClass.Collaboration_input,
                SchemaVersion = ProjectConversationResponseSchemaVersion.Chatbot_projectConversationResponse_v1,
                CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                SafeNextAction = "none",
            });
        }

        public Task<TaskIntentReview> GetTaskIntentReviewAsync(
            string projectId,
            string taskIntentId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
        {
            LastProjectId = projectId;
            LastTaskIntentId = taskIntentId;
            return Task.FromResult(new TaskIntentReview
            {
                ProjectId = projectId,
                TaskIntentId = taskIntentId,
                Available = true,
                ReasonCode = "task_intent_captured",
                SourceMessage = new TaskIntentReviewSourceMessage
                {
                    SourceMessageId = "graph-message-001",
                    Content = "authorized body for review",
                    ContentType = "text/plain",
                    RedactionState = TaskIntentReviewSourceMessageRedactionState.Metadata_only,
                    SourceVersion = "8",
                    EvidenceReferences = ["message:offset:001"],
                },
                AvailableTransitions =
                [
                    new TaskIntentAvailableTransition
                    {
                        Transition = "convert",
                        Label = "Convert to AI action",
                        Enabled = true,
                    },
                    new TaskIntentAvailableTransition
                    {
                        Transition = "duplicate",
                        Label = "Duplicate",
                        Enabled = true,
                        RequiresPredecessorTaskIntentId = true,
                    },
                ],
                AuditHistory =
                [
                    new TaskIntentTransitionAuditSummary
                    {
                        OperationId = "audit-transition-001",
                        Status = "recorded",
                        ActorId = "actor-001",
                        DecidedAtUtc = new DateTimeOffset(2026, 6, 1, 0, 7, 0, TimeSpan.Zero),
                        ReasonCode = "task_intent_captured",
                        CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                        RedactionState = TaskIntentTransitionAuditSummaryRedactionState.Metadata_only,
                    },
                ],
                CurrentState = TaskIntentState.Captured,
                SourceVersion = 8,
                CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                RedactionState = TaskIntentReviewRedactionState.Metadata_only,
                SchemaVersion = "chatbot.task-intent-review.v1",
            });
        }

        private ICollection<ProjectConversationItem> ProjectConversationItems(string projectId)
        {
            List<ProjectConversationItem> items = [ProjectConversationItem(projectId)];
            if (ReturnParticipant)
            {
                items.Add(
                    new ProjectConversationItem
                    {
                        ItemId = "participant:01ARZ3NDEKTSV4RRFFQ69G5FAY:01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                        Kind = ProjectConversationItemKind.Participant,
                        ActorKind = ProjectConversationActorKind.UnresolvedParticipant,
                        ActorLabel = "Unresolved participant",
                        OccurredAt = new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
                        LifecycleState = LifecycleState.Associated,
                        ThresholdBand = AssociationThresholdBand.Auto,
                        ConfidenceScore = 0.91,
                        AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                        SourceMailboxId = "controlled-mailbox-001",
                        SourceConversationId = "conversation-001",
                        SourceProvenance = ProjectConversationItemSourceProvenance.M365MailboxIntake,
                        RedactionState = ProjectConversationItemRedactionState.Metadata_only,
                        RetentionClass = ProjectConversationItemRetentionClass.Collaboration_input,
                        SchemaVersion = ProjectConversationItemSchemaVersion.Chatbot_projectConversationItem_v1,
                        SourceVersion = 5,
                        CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                        ProjectId = projectId,
                        ProjectDisplayName = "Authorized Project",
                        ParticipantResolutionId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                        SourceParticipantId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                        ParticipantStatus = ParticipantResolutionStatus.Unresolved,
                        ParticipantBlockedReason = ParticipantResolutionBlockedReason.NotFound,
                        ParticipantDisplayKind = ProjectConversationParticipantDisplayKind.UnresolvedParticipant,
                        ParticipantEvidenceReference = "mailbox:intake:sender",
                        ParticipantEvidenceFingerprint = "evidence-sha256",
                        ParticipantAllowedReviewActions = [ParticipantReviewAction.Link, ParticipantReviewAction.CreatePending],
                        ParticipantRedactionState = ProjectConversationItemParticipantRedactionState.Metadata_only,
                    });
            }

            if (ReturnAttachment)
            {
                items.Add(
                    new ProjectConversationItem
                    {
                        ItemId = "attachment:01ARZ3NDEKTSV4RRFFQ69G5FAW:0:826F",
                        Kind = ProjectConversationItemKind.Attachment,
                        ActorKind = ProjectConversationActorKind.MailboxAttachment,
                        ActorLabel = "Mailbox attachment",
                        OccurredAt = new DateTimeOffset(2026, 6, 1, 0, 2, 0, TimeSpan.Zero),
                        LifecycleState = LifecycleState.Associated,
                        ThresholdBand = AssociationThresholdBand.Auto,
                        ConfidenceScore = 0.91,
                        AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                        SourceMailboxId = "controlled-mailbox-001",
                        SourceConversationId = "conversation-001",
                        SourceProvenance = ProjectConversationItemSourceProvenance.M365MailboxIntake,
                        RedactionState = ProjectConversationItemRedactionState.Metadata_only,
                        RetentionClass = ProjectConversationItemRetentionClass.Collaboration_input,
                        SchemaVersion = ProjectConversationItemSchemaVersion.Chatbot_projectConversationItem_v1,
                        SourceVersion = 6,
                        CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                        ProjectId = projectId,
                        ProjectDisplayName = "Authorized Project",
                        SourceProviderAttachmentId = "graph-attachment-001",
                        AttachmentDisplayName = "invoice.pdf",
                        AttachmentContentType = "application/pdf",
                        AttachmentSizeInBytes = 4096,
                        AttachmentCaptureStatus = ProjectConversationAttachmentStatus.Captured,
                        AttachmentStorageStatus = ProjectConversationAttachmentStatus.Pending,
                        AttachmentScanStatus = ProjectConversationAttachmentStatus.Pending,
                        AttachmentDuplicateState = "not-evaluated",
                        AttachmentRetryState = "not-retryable",
                        AttachmentAiContextEligibility = "pending",
                        AttachmentAllowedActions = [],
                        AttachmentRedactionState = ProjectConversationItemAttachmentRedactionState.Metadata_only,
                    });
            }

            if (ReturnStoredAttachment)
            {
                items.Add(
                    new ProjectConversationItem
                    {
                        ItemId = "attachment:01ARZ3NDEKTSV4RRFFQ69G5FAW:1:4A1B",
                        Kind = ProjectConversationItemKind.Attachment,
                        ActorKind = ProjectConversationActorKind.MailboxAttachment,
                        ActorLabel = "Mailbox attachment",
                        OccurredAt = new DateTimeOffset(2026, 6, 1, 0, 2, 30, TimeSpan.Zero),
                        LifecycleState = LifecycleState.Associated,
                        ThresholdBand = AssociationThresholdBand.Auto,
                        ConfidenceScore = 0.91,
                        AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                        SourceMailboxId = "controlled-mailbox-001",
                        SourceConversationId = "conversation-001",
                        SourceProvenance = ProjectConversationItemSourceProvenance.M365MailboxIntake,
                        RedactionState = ProjectConversationItemRedactionState.Metadata_only,
                        RetentionClass = ProjectConversationItemRetentionClass.Collaboration_input,
                        SchemaVersion = ProjectConversationItemSchemaVersion.Chatbot_projectConversationItem_v1,
                        SourceVersion = 7,
                        CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                        ProjectId = projectId,
                        ProjectDisplayName = "Authorized Project",
                        SourceProviderAttachmentId = "graph-attachment-002",
                        AttachmentDisplayName = "release-notes.pdf",
                        AttachmentContentType = "application/pdf",
                        AttachmentSizeInBytes = 8192,
                        AttachmentCaptureStatus = ProjectConversationAttachmentStatus.Captured,
                        AttachmentStorageStatus = ProjectConversationAttachmentStatus.Captured,
                        AttachmentScanStatus = ProjectConversationAttachmentStatus.Captured,
                        AttachmentFolderId = "folder-reference-001",
                        AttachmentFileId = "file-reference-001",
                        AttachmentDuplicateState = "unique",
                        AttachmentRetryState = "not-retryable",
                        AttachmentAiContextEligibility = "eligible",
                        AttachmentAllowedActions = ["open-governed-file", "add-to-ai-context"],
                        AttachmentRedactionState = ProjectConversationItemAttachmentRedactionState.Metadata_only,
                    });
            }

            if (ReturnApproval)
            {
                items.Add(
                    new ProjectConversationItem
                    {
                        ItemId = "approval:approval-001:request:8",
                        Kind = ProjectConversationItemKind.ApprovalEvent,
                        ActorKind = ProjectConversationActorKind.ApprovalSystem,
                        ActorLabel = "Approval event",
                        OccurredAt = new DateTimeOffset(2026, 6, 1, 0, 4, 0, TimeSpan.Zero),
                        LifecycleState = LifecycleState.NeedsReview,
                        ThresholdBand = AssociationThresholdBand.Auto,
                        ConfidenceScore = 0,
                        AssociationId = "proposal-001",
                        SourceMailboxId = "approval-event",
                        SourceConversationId = "decision:source:001",
                        SourceProvenance = ProjectConversationItemSourceProvenance.M365MailboxIntake,
                        RedactionState = ProjectConversationItemRedactionState.Metadata_only,
                        RetentionClass = ProjectConversationItemRetentionClass.Collaboration_input,
                        SchemaVersion = ProjectConversationItemSchemaVersion.Chatbot_projectConversationItem_v1,
                        SourceVersion = 8,
                        CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                        ProjectId = projectId,
                        ProjectDisplayName = "Authorized Project",
                        SafeNextAction = "await-approval",
                        ApprovalId = "approval-001",
                        ApprovalEventKind = ApprovalEventKind.Request,
                        ApprovalStatus = ApprovalStatus.Pending,
                        ApprovalRequesterId = "requester-001",
                        ApprovalRequesterActorType = "human",
                        ApprovalRequestedAtUtc = new DateTimeOffset(2026, 6, 1, 0, 4, 0, TimeSpan.Zero),
                        ApprovalProposalId = "proposal-001",
                        ApprovalSourceMessageId = "graph-message-001",
                        ApprovalSourceConversationItemId = "decision:source:001",
                        ApprovalCommandName = "SendExternalReply",
                        ApprovalCommandAllowlistVersion = "allowlist.v1",
                        ApprovalRiskClass = RiskClass.High,
                        ApprovalRiskActionClasses = ["externally-visible"],
                        ApprovalPolicySnapshotId = "policy-snapshot-001",
                        ApprovalPolicySnapshotVisibility = ProjectConversationItemApprovalPolicySnapshotVisibility.Redacted,
                        ApprovalEvidenceReferences = ["evidence:summary:001"],
                        ApprovalEvidenceFreshnessStates = [ApprovalEvidenceFreshness.Expired],
                        ApprovalAffectedResourceReferences = ["project:project-001"],
                        ApprovalRecipientReferences = ["recipient:external:001"],
                        ApprovalSenderAuthorityClass = "on-behalf-of",
                        ApprovalExpectedPostStateRedactionState = ProjectConversationItemApprovalExpectedPostStateRedactionState.Metadata_only,
                        ApprovalActionSummaryRedactionState = ProjectConversationItemApprovalActionSummaryRedactionState.Redacted,
                        ApprovalDisabledReason = ProjectConversationItemApprovalDisabledReason.EvidenceExpired,
                    });
            }

            if (ReturnFailure)
            {
                items.Add(
                    new ProjectConversationItem
                    {
                        ItemId = "failure:operation-001:retry-queued:20",
                        Kind = ProjectConversationItemKind.FailureState,
                        ActorKind = ProjectConversationActorKind.SystemStatus,
                        ActorLabel = "System status",
                        OccurredAt = new DateTimeOffset(2026, 6, 1, 0, 5, 0, TimeSpan.Zero),
                        LifecycleState = LifecycleState.Failed,
                        ThresholdBand = AssociationThresholdBand.Auto,
                        ConfidenceScore = 0,
                        AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                        SourceMailboxId = "failure-state",
                        SourceConversationId = "decision:source:001",
                        SourceProvenance = ProjectConversationItemSourceProvenance.M365MailboxIntake,
                        RedactionState = ProjectConversationItemRedactionState.Metadata_only,
                        RetentionClass = ProjectConversationItemRetentionClass.Collaboration_input,
                        SchemaVersion = ProjectConversationItemSchemaVersion.Chatbot_projectConversationItem_v1,
                        SourceVersion = 20,
                        CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                        ProjectId = projectId,
                        ProjectDisplayName = "Authorized Project",
                        SafeNextAction = "retry-later",
                        WorkflowInstanceId = "workflow-001",
                        FailureStateKind = FailureStateKind.RetryQueued,
                        FailureStatus = FailureStatus.Retryable,
                        MessageCatalogCode = ChatBotMessageCode.Retry_queued,
                        MessageCatalogVersion = ProjectConversationItemMessageCatalogVersion.Chatbot_messageCatalog_v1,
                        MessageDetailVisibility = ProjectConversationItemMessageDetailVisibility.Metadata_only,
                        FailureCategory = "projection",
                        FailureScope = "project-conversation",
                        FailureReasonCode = "projection-retryable",
                        BlockedReason = ProjectConversationItemBlockedReason.ProjectionPending,
                        Retryable = true,
                        RetryCount = 1,
                        MaxRetryCount = 3,
                        RetryOperationId = "retry-operation-001",
                        TaskId = "task-001",
                        OperationId = "operation-001",
                        AuditOperationId = "audit-001",
                        AuditStatus = "available",
                        ClientAction = "retry-later",
                        DuplicateSafetyState = "duplicate-safe",
                    });
            }

            if (ReturnAiOutcome)
            {
                items.Add(
                    new ProjectConversationItem
                    {
                        ItemId = "ai:proposal-001:proposal:10",
                        Kind = ProjectConversationItemKind.AiOutcome,
                        ActorKind = ProjectConversationActorKind.AiActor,
                        ActorLabel = "AI actor",
                        OccurredAt = new DateTimeOffset(2026, 6, 1, 0, 6, 0, TimeSpan.Zero),
                        LifecycleState = LifecycleState.NeedsReview,
                        ThresholdBand = AssociationThresholdBand.Auto,
                        ConfidenceScore = 0,
                        AssociationId = "proposal-001",
                        SourceMailboxId = "ai-outcome",
                        SourceConversationId = "decision:source:001",
                        SourceProvenance = ProjectConversationItemSourceProvenance.M365MailboxIntake,
                        RedactionState = ProjectConversationItemRedactionState.Metadata_only,
                        RetentionClass = ProjectConversationItemRetentionClass.Collaboration_input,
                        SchemaVersion = ProjectConversationItemSchemaVersion.Chatbot_projectConversationItem_v1,
                        SourceVersion = 10,
                        CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                        ProjectId = projectId,
                        ProjectDisplayName = "Authorized Project",
                        SafeNextAction = "review-ai-action",
                        AiOutcomeKind = AiOutcomeKind.Proposal,
                        AiOutcomeStatus = AiOutcomeStatus.Proposed,
                        AiActorId = "ai-actor-001",
                        AiActorType = "ai",
                        AiProposalId = "proposal-001",
                        AiRiskClass = AiActionRiskClass.ApprovalRequired,
                        AiRiskActionClasses = ["invokes-tools"],
                        AiPolicySnapshotId = "policy-snapshot-001",
                        AiPolicySnapshotVisibility = "authorized",
                        AiAuthorizedContextReferences = ["evidence:summary:001"],
                        AiSafeNextAction = "review-ai-action",
                        SupersedesAiOutcomeId = "ai:proposal-000:proposal:9",
                        AiSummaryProvenance = new ProjectConversationAiSummaryProvenance
                        {
                            GeneratedBy = "ai-model.v1",
                            GeneratedAtUtc = new DateTimeOffset(2026, 6, 1, 0, 6, 0, TimeSpan.Zero),
                            SourceEvidenceIds = ["evidence:summary:001"],
                            ContextPackageId = "context-package-001",
                            ContextPackageVersion = "v1",
                            RedactionState = ProjectConversationAiSummaryProvenanceRedactionState.Metadata_only,
                        },
                    });
            }

            return items;
        }

        private static ProjectConversationItem ProjectConversationItem(string projectId)
            => new()
            {
                ItemId = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                Kind = ProjectConversationItemKind.SystemDecision,
                ActorKind = ProjectConversationActorKind.SystemDecision,
                ActorLabel = "System decision",
                OccurredAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                LifecycleState = LifecycleState.Associated,
                ThresholdBand = AssociationThresholdBand.Auto,
                ConfidenceScore = 0.91,
                AssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                SourceMailboxId = "controlled-mailbox-001",
                SourceProviderMessageId = "graph-message-001",
                InternetMessageId = "<internet-message-001@example.test>",
                SourceConversationId = "conversation-001",
                SourceReceivedAtUtc = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                SourceSentAtUtc = new DateTimeOffset(2026, 5, 31, 23, 58, 0, TimeSpan.Zero),
                SourceCreatedAtUtc = new DateTimeOffset(2026, 5, 31, 23, 57, 0, TimeSpan.Zero),
                SourceTimezone = "UTC",
                SourceProvenanceDisplayToken = "Microsoft 365 mailbox",
                SourceProvenance = ProjectConversationItemSourceProvenance.M365MailboxIntake,
                RedactionState = ProjectConversationItemRedactionState.Metadata_only,
                RetentionClass = ProjectConversationItemRetentionClass.Collaboration_input,
                SchemaVersion = ProjectConversationItemSchemaVersion.Chatbot_projectConversationItem_v1,
                SourceVersion = 4,
                CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                ProjectId = projectId,
                ProjectDisplayName = "Authorized Project",
                DecisionLabel = "Associate",
                DecisionKind = AssociationDecisionKind.Associate,
                DecisionActorId = "user-001",
                DecisionActorType = "Human",
                DecidedAtUtc = new DateTimeOffset(2026, 6, 1, 0, 0, 30, TimeSpan.Zero),
                DecisionNoteRedactionState = ProjectConversationItemDecisionNoteRedactionState.Redacted,
                SurfaceOrigin = "ui",
                PolicySnapshotVersion = "association-thresholds.m0.default.v1",
                EvidenceReferenceSummary = ["mailbox:intake:subject"],
                CorrectionKind = AssociationCorrectionKind.ProjectReassignment,
                PriorProjectId = "project-000",
                CorrectedProjectId = projectId,
                SupersedesAssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FB1",
                CorrectionRationaleRedactionState = ProjectConversationItemCorrectionRationaleRedactionState.Redacted,
                CorrectionActorId = "user-001",
                CorrectionActorType = "Human",
                CorrectedAtUtc = new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
                DownstreamImpactStatus = "delayed",
                CorrectionId = "correction-001",
                WorkflowInstanceId = "workflow-001",
                RequiredStoreKeys = ["project-conversation", "participants"],
                CompletedStoreKeys = ["project-conversation"],
                FailedStoreKeys = ["participants"],
                PropagationProgressNumerator = 1,
                PropagationProgressDenominator = 2,
                PropagationStartedAtUtc = new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
                PropagationStatus = "delayed",
                IsCorrectedContextStale = true,
                ResponsibleOwnerRole = "operations",
                StatusSummary = new ProjectConversationItemStatusSummary
                {
                    Facets =
                    [
                        new ProjectConversationItemStatusFacet
                        {
                            Domain = ProjectConversationItemStatusFacetDomain.Association,
                            Health = ChatBotHealthStatus.Healthy,
                            SourceState = "associated",
                            MessageCode = "association_decision_accepted",
                            SafeNextAction = "none",
                            SafeMetadataIds = new Dictionary<string, string> { ["associationId"] = "01ARZ3NDEKTSV4RRFFQ69G5FAW" },
                        },
                        new ProjectConversationItemStatusFacet
                        {
                            Domain = ProjectConversationItemStatusFacetDomain.Command,
                            Health = ChatBotHealthStatus.Degraded,
                            SourceState = "accepted-projection-pending",
                            MessageCode = "operation_projection_pending",
                            SafeNextAction = "wait-for-projection",
                            OperationId = "operation-001",
                            CompletionStatus = "accepted-projection-pending",
                            ProjectionStatus = "accepted-projection-pending",
                            AuditStatus = "reconciling",
                            CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                        },
                        new ProjectConversationItemStatusFacet
                        {
                            Domain = ProjectConversationItemStatusFacetDomain.Task,
                            Health = ChatBotHealthStatus.Unknown,
                            SourceState = "unknown",
                            MessageCode = "status_task_unknown",
                            SafeNextAction = "none",
                        },
                    ],
                },
                Classification = new ProjectConversationItemClassification
                {
                    Kind = ProjectConversationClassificationKind.Actionable,
                    KernelVersion = "chatbot.project-conversation-classification.kernel.v1",
                    ConfidenceScore = 0.91,
                    MessageCode = "conversation_item_actionable",
                    SourceEvidenceIds = ["mailbox:intake:subject"],
                    RedactionState = ProjectConversationItemClassificationRedactionState.Metadata_only,
                },
                DetectedIntent = new ProjectConversationDetectedIntent
                {
                    Summary = "intent:review-decision",
                    ActionKind = ProjectConversationDetectedActionKind.RequestDecision,
                    SourceEvidenceIds = ["mailbox:intake:subject"],
                    SafeNextAction = "review-association",
                    MessageCode = "detected_intent_request_decision",
                    RedactionState = ProjectConversationDetectedIntentRedactionState.Metadata_only,
                },
                ReviewHistory =
                [
                    new ProjectConversationReviewHistoryEntry
                    {
                        ReviewedResourceKind = "email",
                        ReviewedResourceId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                        ActionCode = "classification-projected",
                        DecisionCode = "actionable",
                        ActorKind = "system",
                        ActorLabel = "System decision",
                        ReviewedAtUtc = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                        SurfaceOrigin = "api",
                        CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                        RedactionState = ProjectConversationReviewHistoryEntryRedactionState.Metadata_only,
                        ReasonCode = "conversation_item_actionable",
                    },
                ],
            };

        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
        {
            LastSubmittedCommand = command;
            LastSubmitCorrelationId = correlationId;
            LastSubmitOrigin = origin;
            return Task.FromResult(new CommandSubmissionResponse
            {
                CommandId = "accepted-command-001",
                CorrelationId = correlationId ?? "correlation-generated",
                TaskId = taskId,
                LifecycleState = LifecycleState.Proposed,
                AcceptedAt = new DateTimeOffset(2026, 6, 1, 0, 8, 0, TimeSpan.Zero),
            });
        }

        public Task<OperationStatus> GetOperationStatusAsync(string operationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(string operationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(string associationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
        {
            LastAssociationId = associationId;
            return Task.FromResult(new AssociationRoutingStatus
            {
                AssociationId = associationId,
                IntakeId = "01ARZ3NDEKTSV4RRFFQ69G5FBA",
                SourceMailboxId = "controlled-mailbox-001",
                SourceConversationId = "conversation-001",
                LifecycleState = LifecycleState.Associated,
                Outcome = AssociationScoringOutcome.AutoAssociated,
                ThresholdBand = AssociationThresholdBand.Auto,
                ConfidenceScore = 0.91,
                ReasonCodes = [AssociationReasonCode.ExplicitProjectIdentifierMatched],
                Candidates = [],
                Exclusions = [],
                ThresholdPolicyVersion = "association-thresholds.m0.default.v1",
                EvidenceRefs =
                [
                    new AssociationEvidenceReference
                    {
                        EvidenceReference = "mailbox:project-id",
                        EvidenceFingerprint = "hash-project",
                        EvidenceKind = "project-identifier",
                        SignalClass = AssociationEvidenceReferenceSignalClass.ExplicitProjectIdentifier,
                        MatchedValueDisplayToken = "mailbox:metadata",
                        VisibilityState = AssociationEvidenceReferenceVisibilityState.Available,
                        RedactionState = AssociationEvidenceReferenceRedactionState.Metadata_only,
                        FreshnessState = AssociationEvidenceReferenceFreshnessState.Fresh,
                        ConfidenceContribution = 0.42,
                    },
                ],
                KernelVersion = "association-deterministic.kernel.m0.v1",
                DetectedAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                SourceProvenance = AssociationRoutingStatusSourceProvenance.M365MailboxIntake,
                RedactionState = AssociationRoutingStatusRedactionState.Metadata_only,
                RetentionClass = AssociationRoutingStatusRetentionClass.Collaboration_input,
                SchemaVersion = "chatbot.association-routing-status.v1",
                SourceVersion = 7,
                CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                DisabledActionReasonCodes = [],
                NextActionReasonCodes = [ChatBotMessageCode.Association_ambiguous_routed],
                DecidedAt = new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
                DecisionActorId = "actor-safe",
                DecisionActorType = "human",
                SupersededByAssociationId = "01ARZ3NDEKTSV4RRFFQ69G5FBC",
                SupersedingCorrectionId = "correction-002",
                SupersedingCorrectionLink = "association:01ARZ3NDEKTSV4RRFFQ69G5FBC",
                CorrectionPanelAvailable = true,
                SafeNextAction = "none",
            });
        }
    }
}
