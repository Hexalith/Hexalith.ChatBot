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
}
