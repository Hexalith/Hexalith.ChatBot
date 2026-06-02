namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal interface ITenantAiPolicySnapshotProvider
{
    ValueTask<TenantAiPolicySnapshot?> TryGetAsync(
        string tenantId,
        string projectId,
        string? requestedPolicySnapshotId,
        CancellationToken cancellationToken);
}

internal sealed record TenantAiPolicySnapshot(
    string PolicySnapshotId,
    bool LowRiskAllowed,
    string EffectSurface,
    IReadOnlyList<string> AssistanceKinds,
    bool IsFresh,
    bool IsValid,
    IReadOnlyDictionary<Hexalith.ChatBot.Contracts.Enums.AiActionRiskActionClass, bool>? LowRiskAllowedByActionClass = null);
