using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association;

public sealed record AssociationConfidenceThresholdsChanged(
    string PolicyId,
    string TenantId,
    double PreviousTHigh,
    double PreviousTLow,
    string PreviousPolicyVersion,
    double THigh,
    double TLow,
    string PolicyVersion,
    string? EvaluationRunReference,
    string ActorId,
    string CorrelationId,
    DateTimeOffset ChangedAt,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string SchemaVersion) : IEventPayload;
