using Hexalith.ChatBot.IntegrationTests.Recovery;

using Xunit;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// CiCd-owned topology-acceptance filters this type name (E2E, not E2e) on the IntegrationTests runner.
/// </summary>
[Trait("Category", "E2E")]
[Collection(RealmTemplateMutationCollection.Name)]
public sealed class TrivialGovernedCommandAspireE2ETests(ITestOutputHelper output)
{
    /// <summary>Required CI topology smoke: real canonical refusal without governed mutation or reservations.</summary>
    [Fact]
    public Task GovernedCommandsShouldFailClosedWithoutAtomicParticipationInRealDaprTopology()
        => new TopologyTestDeadline().RunAsync(
            new TrivialGovernedCommandAspireE2eTests(output).RunGovernedCommandRefusalAsync,
            TestContext.Current.CancellationToken);

    /// <summary>Required release M2 gate: committed two-tenant governed state and original stop-ship sweeps.</summary>
    [Fact]
    [Trait("ReleaseGate", "M2")]
    public Task CommittedGovernedStateShouldPassM2StopShipSweepsInRealDaprTopology()
        => new TopologyTestDeadline().RunAsync(
            new TrivialGovernedCommandAspireE2eTests(output).RunCommittedGovernedStateM2GateAsync,
            TestContext.Current.CancellationToken);
}
