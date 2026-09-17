namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record TaskIntentReviewModel(
    string ProjectId,
    string TaskIntentId,
    bool Available,
    string ReasonCode,
    string? SourceMessageContent,
    string? SourceMessageContentType,
    IReadOnlyList<TaskIntentAvailableTransitionModel> AvailableTransitions,
    IReadOnlyList<TaskIntentTransitionAuditSummaryModel> AuditHistory,
    string? CurrentState,
    long? SourceVersion,
    string CorrelationId,
    string RedactionState,
    string SchemaVersion);
