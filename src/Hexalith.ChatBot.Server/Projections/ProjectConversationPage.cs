namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ProjectConversationPage(
    IReadOnlyList<ProjectConversationItemView> Items,
    string? NextCursor,
    bool HasMore,
    int PageSize,
    ProjectConversationItemView? LatestItem = null);
