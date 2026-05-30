using System.Text.RegularExpressions;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Client;

public sealed partial class ChatBotClient : IChatBotClient
{
    private readonly IClient _transportClient;

    public ChatBotClient(IClient transportClient)
    {
        _transportClient = transportClient ?? throw new ArgumentNullException(nameof(transportClient));
    }

    public Task<CommandSubmissionResponse> SubmitAsync(
        IChatBotCommand command,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        string commandType = ResolveCommandType(command);
        string commandId = ChatBotCommandId.New().Value;
        string effectiveCorrelationId = NormalizeCorrelationId(correlationId);
        string? effectiveTaskId = NormalizeTaskId(taskId);

        var request = new CommandSubmissionRequest
        {
            CommandId = commandId,
            CommandType = commandType,
            Command = command,
            RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
        };

        return _transportClient.SubmitCommandAsync(effectiveCorrelationId, effectiveTaskId, request, cancellationToken);
    }

    public Task<OperationStatus> GetOperationStatusAsync(
        string operationId,
        string? correlationId = null,
        string? taskId = null,
        CancellationToken cancellationToken = default)
    {
        if (!ChatBotTaskId.TryParse(operationId, out ChatBotTaskId parsedOperationId))
        {
            throw new ArgumentException("Operation identifiers must be ULIDs.", nameof(operationId));
        }

        string effectiveCorrelationId = NormalizeCorrelationId(correlationId);
        string? effectiveTaskId = NormalizeTaskId(taskId);
        return _transportClient.GetOperationStatusAsync(parsedOperationId.Value, effectiveCorrelationId, effectiveTaskId, cancellationToken);
    }

    private static string ResolveCommandType(IChatBotCommand command)
    {
        string commandType = command.GetType().Name;

        if (!CommandTypeNamePattern().IsMatch(commandType) ||
            commandType.EndsWith("Command", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "ChatBot command type names must be imperative contract names and must not use the Command suffix.",
                nameof(command));
        }

        return commandType;
    }

    private static string NormalizeCorrelationId(string? correlationId)
    {
        if (correlationId is null)
        {
            return ChatBotCorrelationId.New().Value;
        }

        if (ChatBotCorrelationId.TryParse(correlationId, out ChatBotCorrelationId parsed))
        {
            return parsed.Value;
        }

        throw new ArgumentException("Correlation identifiers must be ULIDs.", nameof(correlationId));
    }

    private static string? NormalizeTaskId(string? taskId)
    {
        if (taskId is null)
        {
            return null;
        }

        if (ChatBotTaskId.TryParse(taskId, out ChatBotTaskId parsed))
        {
            return parsed.Value;
        }

        throw new ArgumentException("Task identifiers must be ULIDs.", nameof(taskId));
    }

    [GeneratedRegex("^[A-Z][A-Za-z0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex CommandTypeNamePattern();
}
