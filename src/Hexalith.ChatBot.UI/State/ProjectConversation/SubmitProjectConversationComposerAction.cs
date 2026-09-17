namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record SubmitProjectConversationComposerAction(
    string ProjectId,
    ProjectConversationComposerMode Mode,
    string Text,
    string Locale,
    long ExpectedSourceVersion)
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");
}
