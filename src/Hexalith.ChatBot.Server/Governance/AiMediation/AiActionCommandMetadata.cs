using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed record AiActionCommandMetadata(
    string CommandName,
    IReadOnlyList<AiActionRiskActionClass> ActionClasses,
    string EffectSurface,
    string TenantPolicyClassification,
    string CommandAllowlistVersion,
    AiActionRiskClass CommandDefaultRisk,
    bool Supported,
    AiActionAuthorityClass RequiredAuthorityClass,
    AiActionIdempotencyContract IdempotencyContract);
