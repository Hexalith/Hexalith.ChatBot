using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Governance.Policy;

/// <summary>Denies live policy writes while accepted A6 protection and native isolation evidence is absent.</summary>
internal static class TenantPolicyPersistenceGate
{
    /// <summary>Identifies every policy write, including legacy transports and signed follow-ups.</summary>
    public static bool Denies(string? commandType)
        => commandType is nameof(SubmitTenantPolicyChange) or nameof(ApproveTenantPolicyChange) or nameof(SetAssociationConfidenceThresholds) or nameof(SubmitEscalationPolicyChange);
}
