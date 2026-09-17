using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed record ProjectConversationQuery(
    string ProjectId,
    string? Cursor,
    int PageSize,
    bool ProjectReadAuthorized,
    bool HasProjectScopeClaims,
    string? TaskId);
