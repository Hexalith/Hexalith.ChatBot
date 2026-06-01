namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationState(
    bool IsLoading,
    ProjectConversationModel? Conversation,
    string? ErrorCode);
