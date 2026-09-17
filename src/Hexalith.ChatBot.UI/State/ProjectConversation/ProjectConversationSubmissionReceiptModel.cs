namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationSubmissionReceiptModel(
    ProjectConversationComposerMode Mode,
    string CommandId,
    string CorrelationId,
    string? TaskId,
    string LifecycleState,
    DateTimeOffset AcceptedAt,
    string SafeNextAction);
