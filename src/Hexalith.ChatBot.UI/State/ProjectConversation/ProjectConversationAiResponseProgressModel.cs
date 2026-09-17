namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationAiResponseProgressModel(
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    string CorrelationId,
    long SourceVersion,
    long Sequence,
    string State,
    string TerminalReason,
    string SafeNextAction,
    string RedactionState,
    string VisibilityState,
    bool IsTerminal)
{
    public string? StateOwnerAggregateId { get; init; }

    public long? StartedSourceVersion { get; init; }

    public DateTimeOffset? RecoveryDeadlineUtc { get; init; }
}
