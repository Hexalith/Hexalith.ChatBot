namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationAiResponseCancellationFailedAction(string ErrorCode)
{
    public string? ProjectId { get; init; }

    public string? RequestId { get; init; }

    public long ScopeVersion { get; init; }
}
