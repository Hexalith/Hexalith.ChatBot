namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed record TenantAiPolicySnapshot(
    string PolicySnapshotId,
    bool LowRiskAllowed,
    string EffectSurface,
    IReadOnlyList<string> AssistanceKinds,
    bool IsFresh,
    bool IsValid,
    IReadOnlyDictionary<Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass, bool>? LowRiskAllowedByActionClass = null);
