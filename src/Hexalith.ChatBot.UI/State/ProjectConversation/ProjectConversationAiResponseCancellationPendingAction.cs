namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationAiResponseCancellationPendingAction(string ResponseId, string GenerationId)
{
    public string? ProjectId { get; init; }

    public string? RequestId { get; init; }

    public long ScopeVersion { get; init; }
}
