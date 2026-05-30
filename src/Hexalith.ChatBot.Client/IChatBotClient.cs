using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Client;

public interface IChatBotClient
{
    Task<CommandSubmissionResponse> SubmitAsync(
        IChatBotCommand command,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default);
}
