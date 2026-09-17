namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationComposerValidationFailedAction(string ErrorCode)
{
    public string? RequestId { get; init; }

    public long ScopeVersion { get; init; }
}
