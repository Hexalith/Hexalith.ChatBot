namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationLoadedAction(
    ProjectConversationModel Conversation,
    string? RequestId = null,
    string? RequestedProjectId = null,
    string? Cursor = null);
