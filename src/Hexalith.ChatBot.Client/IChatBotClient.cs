using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Client;

public interface IChatBotClient
{
    Task<CommandSubmissionResponse> SubmitAsync(
        IChatBotCommand command,
        string? correlationId = null,
        string? taskId = null,
        ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
        CancellationToken cancellationToken = default);

    Task<OperationStatus> GetOperationStatusAsync(
        string operationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default);

    Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
        string operationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default);

    Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
        string associationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default);

    Task<ProjectConversationResponse> GetProjectConversationAsync(
        string projectId,
        string? cursor = null,
        int pageSize = 25,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default);

    Task<TaskIntentReview> GetTaskIntentReviewAsync(
        string projectId,
        string taskIntentId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Task-intent review reads are not supported by this client implementation.");
}
