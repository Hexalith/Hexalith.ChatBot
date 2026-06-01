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
    }

    private sealed class FakeChatBotClient : IChatBotClient
    {
        public string? LastProjectId { get; private set; }

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
                Items =
                [
                    new ProjectConversationItem
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
                        SourceConversationId = "conversation-001",
                        SourceProvenance = ProjectConversationItemSourceProvenance.M365MailboxIntake,
                        RedactionState = ProjectConversationItemRedactionState.Metadata_only,
                        RetentionClass = ProjectConversationItemRetentionClass.Collaboration_input,
                        SchemaVersion = ProjectConversationItemSchemaVersion.Chatbot_projectConversationItem_v1,
                        SourceVersion = 4,
                        CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                        ProjectId = projectId,
                        ProjectDisplayName = "Authorized Project",
                        DecisionLabel = "Associate",
                    },
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
