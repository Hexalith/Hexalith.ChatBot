using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record TaskIntentReview(
    string ProjectId,
    string TaskIntentId,
    bool Available,
    string ReasonCode,
    TaskIntentRecord? Record,
    TaskIntentReviewSourceMessage? SourceMessage,
    IReadOnlyList<TaskIntentAvailableTransition> AvailableTransitions,
    IReadOnlyList<TaskIntentTransitionAuditSummary> AuditHistory,
    TaskIntentState? CurrentState,
    long? SourceVersion,
    string CorrelationId,
    string RedactionState,
    string SchemaVersion);
