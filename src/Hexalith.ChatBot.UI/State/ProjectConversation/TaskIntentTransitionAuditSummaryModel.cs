namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record TaskIntentTransitionAuditSummaryModel(
    string OperationId,
    string Status,
    string ActorId,
    DateTimeOffset DecidedAtUtc,
    string ReasonCode,
    string CorrelationId,
    string RedactionState);
