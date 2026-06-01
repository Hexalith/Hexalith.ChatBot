using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record AiActionRiskInputTuple(
    string IntendedCommandName,
    IReadOnlyList<AiActionRiskActionClass> ActionClasses,
    string? EffectSurface,
    string? TenantPolicyClassification,
    string? RequesterAuthorityClass,
    string? PolicySnapshotId,
    string? CommandAllowlistVersion,
    AiActionRiskClass? CommandDefaultRisk,
    string? AllowlistMetadataState,
    string? ProjectAuthorizationState,
    string CorrelationId);
