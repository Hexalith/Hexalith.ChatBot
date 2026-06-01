using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record AiActionRiskClassificationRecord(
    AiActionRiskClass RiskClass,
    IReadOnlyList<AiActionRiskActionClass> RiskActionClasses,
    string ClassifierVersion,
    AiActionRiskInputTuple InputTuple,
    string? PolicySnapshotId,
    string CommandAllowlistVersion,
    AiActionRiskClass? CommandDefaultRisk,
    string RequesterAuthorityClass,
    string ReasonCode,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    string CorrelationId,
    DateTimeOffset ProducedAtUtc,
    string? IndeterminateReason = null,
    bool Rejected = false);
