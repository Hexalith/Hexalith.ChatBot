namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record TaskIntentTransitionAuditSummary(
    string OperationId,
    string Status,
    string ActorId,
    DateTimeOffset DecidedAtUtc,
    string ReasonCode,
    string CorrelationId,
    string RedactionState);
