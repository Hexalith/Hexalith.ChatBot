using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Governance.Policy;

/// <summary>Denies live policy writes while accepted A6 protection and native isolation evidence is absent.</summary>
internal static class TenantPolicyPersistenceGate
{
    /// <summary>Identifies commands that propose a replacement tenant policy or configuration snapshot, approve a policy change, or activate thresholds.</summary>
    public static bool Denies(string? commandType)
        => commandType is nameof(SubmitTenantPolicyChange) or nameof(ApproveTenantPolicyChange) or nameof(SetAssociationConfidenceThresholds)
            or nameof(SubmitEscalationPolicyChange) or nameof(SubmitNotificationRoutingChange) or nameof(SubmitMailboxConfigurationChange)
            or nameof(SubmitRetentionConfigurationChange) or nameof(SubmitDataClassInventoryChange);
}
