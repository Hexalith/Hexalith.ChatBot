using System.Collections.Frozen;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

/// <summary>Resolved AI policy evidence with an exact immutable tenant, identity, and version reference.</summary>
/// <param name="TenantId">The provider-reported owning tenant, which must match the trusted request tenant.</param>
/// <param name="PolicyVersion">The resolved positive policy version.</param>
/// <param name="PolicySnapshotId">The exact immutable snapshot identity.</param>
/// <param name="LowRiskAllowed">Whether low-risk assistance is allowed.</param>
/// <param name="EffectSurface">The permitted effect surface.</param>
/// <param name="AssistanceKinds">The permitted assistance kinds.</param>
/// <param name="IsFresh">Whether the provider evidence is fresh.</param>
/// <param name="IsValid">Whether the provider evidence is valid.</param>
/// <param name="LowRiskAllowedByActionClass">Optional per-action restrictions.</param>
internal sealed record TenantAiPolicySnapshot(
    string TenantId,
    long PolicyVersion,
    string PolicySnapshotId,
    bool LowRiskAllowed,
    string EffectSurface,
    IReadOnlyList<string> AssistanceKinds,
    bool IsFresh,
    bool IsValid,
    IReadOnlyDictionary<AiActionRiskActionClass, bool>? LowRiskAllowedByActionClass = null)
{
    /// <summary>Gets the permitted assistance kinds captured with the snapshot.</summary>
    public IReadOnlyList<string> AssistanceKinds
    {
        get;
        init => field = CopyAssistanceKinds(value);
    } = CopyAssistanceKinds(AssistanceKinds);

    /// <summary>Gets the optional action-class restrictions captured with the snapshot.</summary>
    public IReadOnlyDictionary<AiActionRiskActionClass, bool>? LowRiskAllowedByActionClass
    {
        get;
        init => field = value?.ToFrozenDictionary();
    } = LowRiskAllowedByActionClass?.ToFrozenDictionary();

    private static IReadOnlyList<string> CopyAssistanceKinds(IReadOnlyList<string> kinds)
        => kinds is null ? null! : Array.AsReadOnly(kinds.ToArray());
}
