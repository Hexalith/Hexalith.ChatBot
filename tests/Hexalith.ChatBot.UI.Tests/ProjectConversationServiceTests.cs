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

        client.ReturnParticipant = true;
        ProjectConversationModel participantConversation = await service.GetProjectConversationAsync("project-001", cancellationToken: TestContext.Current.CancellationToken);
        ProjectConversationItemModel participant = participantConversation.Items.Single(static model => model.IsParticipant);
        participant.ParticipantStatus.ShouldBe("Unresolved");
        participant.ParticipantDisplayKind.ShouldBe("UnresolvedParticipant");
        participant.ParticipantAllowedReviewActions.ShouldBe(["Link", "CreatePending"], ignoreOrder: false);
        participant.ParticipantEvidenceReference.ShouldBe("mailbox:intake:sender");
    }

    private sealed class FakeChatBotClient : IChatBotClient
    {
        public string? LastProjectId { get; private set; }

        public bool ReturnParticipant { get; set; }

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
                Items = ReturnParticipant
                ?
                [
                    ProjectConversationItem(projectId),
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
                    },
                ]
                :
                [
                    ProjectConversationItem(projectId),
                ],
                Page = new ProjectConversationCursorPage { HasMore = false, PageSize = 25 },
                SourceProvenance = ProjectConversationResponseSourceProvenance.M365MailboxIntake,
                RedactionState = ProjectConversationResponseRedactionState.Metadata_only,
                RetentionClass = ProjectConversationResponseRetentionClass.Collaboration_input,
                SchemaVersion = ProjectConversationResponseSchemaVersion.Chatbot_projectConversationResponse_v1,
                CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                SafeNextAction = "none",
            });
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
