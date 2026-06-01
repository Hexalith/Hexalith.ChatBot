namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Cursor page metadata for tenant-scoped project conversation reads.
/// </summary>
public sealed record ProjectConversationCursorPage(
    string? NextCursor,
    bool HasMore,
    int PageSize);
