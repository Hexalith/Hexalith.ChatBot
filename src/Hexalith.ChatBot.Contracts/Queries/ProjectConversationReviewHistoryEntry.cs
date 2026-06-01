namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record ProjectConversationReviewHistoryEntry(
    string ReviewedResourceKind,
    string ReviewedResourceId,
    string ActionCode,
    string? DecisionCode,
    string? ActorKind,
    string? ActorLabel,
    DateTimeOffset ReviewedAtUtc,
    string? SurfaceOrigin,
    string? CorrelationId,
    string? OperationId,
    string RedactionState,
    string ReasonCode);
