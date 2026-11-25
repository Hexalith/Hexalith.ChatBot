using Hexalith.ChatBot.IntegrationTests;

namespace Hexalith.ChatBot.Release.Tests;

/// <summary>Preserves real positive-state M2 and origin-parity product checks in the daily release project.</summary>
public sealed class GovernedStateReleaseGateTests(ITestOutputHelper output)
{
    private static readonly TopologyTestDeadline _deadline = new();

    /// <summary>Requires real two-tenant committed state and every original stop-ship assertion.</summary>
    [Fact]
    [Trait("ReleaseGate", "M2")]
    public async Task CommittedGovernedStateShouldPassM2StopShipSweepsInRealDaprTopology()
        => await _deadline.RunAsync(new TrivialGovernedCommandAspireE2eTests(output).RunCommittedGovernedStateM2GateAsync,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

    /// <summary>Requires identical real durable state across UI, CLI and MCP origins.</summary>
    [Fact]
    [Trait("ReleaseGate", "OriginParity")]
    public async Task GovernedNoteShouldProjectIdenticalDurableEndStateRegardlessOfDeclaredOrigin()
        => await _deadline.RunAsync(new TrivialGovernedCommandAspireE2eTests(output).RunGovernedNoteOriginParityAsync,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
}
