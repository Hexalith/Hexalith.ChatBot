namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ProjectConversationPage(
    IReadOnlyList<ProjectConversationItemView> Items,
    ProjectConversationCursorPosition? NextCursorPosition,
    bool HasMore,
    int PageSize,
    ProjectConversationItemView? LatestItem = null,
    IReadOnlyList<ProjectConversationItemView>? AuthoritativeItems = null);
