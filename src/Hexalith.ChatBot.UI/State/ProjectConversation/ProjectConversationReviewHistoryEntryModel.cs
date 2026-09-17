namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record ProjectConversationReviewHistoryEntryModel(
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
