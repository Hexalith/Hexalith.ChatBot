using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Top-level deterministic scorer result carried to durable state and projection contracts.
/// </summary>
public sealed record AssociationScoringResult(
    double ConfidenceScore,
    AssociationThresholdBand ThresholdBand,
    AssociationScoringOutcome Outcome,
    IReadOnlyList<AssociationReasonCode> ReasonCodes,
    string KernelVersion,
    DateTimeOffset DetectedAt,
    string SourceMailboxId,
    string IntakeId,
    string SourceConversationId,
    string? SourceThreadId,
    string CorrelationId,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion);
