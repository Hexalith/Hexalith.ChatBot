namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record LoadProjectConversationAction(string ProjectId, string? Cursor = null)
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");

    public bool IsHistory => !string.IsNullOrWhiteSpace(Cursor);
}
