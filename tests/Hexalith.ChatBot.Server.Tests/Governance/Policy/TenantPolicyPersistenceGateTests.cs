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
        policyCommands.Where(static command => !TenantPolicyPersistenceGate.Denies(command)).ShouldBeEmpty();
    }
}
