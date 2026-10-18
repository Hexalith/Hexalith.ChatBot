using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Governance.Policy;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Governance.Policy;

public sealed class TenantPolicyPersistenceGateTests
{
    [Fact]
    public void GateMustDenyEveryCommandClassifiedAsTenantPolicyMutation()
    {
        string[] policyCommands = [.. ChatBotAuditPathMap.CommandsForPath("tenant-policy-mutation")];
        policyCommands.ShouldNotBeEmpty();

        // Compliance snapshot changes are outside the audit map's tenant-policy-mutation category.
        string[] snapshotChangeCommands =
        [
            nameof(SubmitTenantPolicyChange), nameof(ApproveTenantPolicyChange), nameof(SetAssociationConfidenceThresholds),
            nameof(SubmitEscalationPolicyChange), nameof(SubmitNotificationRoutingChange), nameof(SubmitMailboxConfigurationChange),
            nameof(SubmitRetentionConfigurationChange), nameof(SubmitDataClassInventoryChange),
        ];
        policyCommands.Concat(snapshotChangeCommands).Where(static command => !TenantPolicyPersistenceGate.Denies(command)).ShouldBeEmpty();
    }
}
