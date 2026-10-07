using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed record ProjectConversationQuery(
    string ProjectId,
    string? Cursor,
    int PageSize,
    string? TaskId);
