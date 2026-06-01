using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.ProjectConversation;

using Shouldly;

using ChatBotSurfaceOrigin = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin;
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
        item.SourceConversationId.ShouldBe("conversation-001");
        item.SourceProviderMessageId.ShouldBe("graph-message-001");
        item.InternetMessageId.ShouldBe("<internet-message-001@example.test>");
        item.SourceReceivedAtUtc.ShouldBe(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
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
    }

    private sealed class FakeChatBotClient : IChatBotClient
    {
        public string? LastProjectId { get; private set; }

        public bool ReturnParticipant { get; set; }

        public bool ReturnAttachment { get; set; }

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
        {
            LastProjectId = projectId;
            return Task.FromResult(new ProjectConversationResponse
            {
                ProjectId = projectId,
                ProjectDisplayName = "Authorized Project",
                Status = ProjectConversationReadStatus.Current,
                ConversationState = LifecycleState.Associated,
                Items = ProjectConversationItems(projectId),
                Page = new ProjectConversationCursorPage { HasMore = false, PageSize = 25 },
                SourceProvenance = ProjectConversationResponseSourceProvenance.M365MailboxIntake,
                RedactionState = ProjectConversationResponseRedactionState.Metadata_only,
                RetentionClass = ProjectConversationResponseRetentionClass.Collaboration_input,
                SchemaVersion = ProjectConversationResponseSchemaVersion.Chatbot_projectConversationResponse_v1,
                CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                SafeNextAction = "none",
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
            };

        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationStatus> GetOperationStatusAsync(string operationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(string operationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(string associationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
