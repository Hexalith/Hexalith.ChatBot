namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record LoadProjectConversationAction(string ProjectId, string? Cursor = null);

public sealed record ProjectConversationLoadedAction(ProjectConversationModel Conversation);

public sealed record ProjectConversationFailedAction(string ErrorCode);
