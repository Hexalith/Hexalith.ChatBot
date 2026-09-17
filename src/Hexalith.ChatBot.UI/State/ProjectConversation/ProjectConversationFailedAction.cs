namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationFailedAction(
    string ErrorCode,
    string? RequestId = null,
    string? RequestedProjectId = null,
    string? Cursor = null);
