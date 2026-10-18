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

        // Discover proposed snapshots independently of the audit map and the gate's command list.
        string[] snapshotChangeCommands =
        [
            .. typeof(IChatBotCommand).Assembly.GetTypes()
                .Where(static type => typeof(IChatBotCommand).IsAssignableFrom(type) &&
                    type.GetProperties().Any(static property =>
                        property.Name.StartsWith("Proposed", StringComparison.Ordinal) &&
                        property.Name.EndsWith("SnapshotId", StringComparison.Ordinal)))
                .Select(static type => type.Name),
        ];
        snapshotChangeCommands.ShouldNotBeEmpty();

        policyCommands.Union(snapshotChangeCommands, StringComparer.Ordinal)
            .Where(static command => !TenantPolicyPersistenceGate.Denies(command)).ShouldBeEmpty();
    }
}
