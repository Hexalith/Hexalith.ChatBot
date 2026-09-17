namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record StopProjectConversationAiResponseAction(ProjectConversationAiResponseProgressModel Progress)
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");
}
