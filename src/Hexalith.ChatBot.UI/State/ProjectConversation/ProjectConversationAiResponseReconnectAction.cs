namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationAiResponseReconnectAction(string ProjectId)
{
    public long ScopeVersion { get; init; }
}
