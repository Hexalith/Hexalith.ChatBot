using Hexalith.ChatBot.IntegrationTests;

namespace Hexalith.ChatBot.Live.IntegrationTests;

/// <summary>Runs every required real refusal, browser and runtime-health assertion with zero skipped tests.</summary>
public sealed class RequiredTopologyTests(ITestOutputHelper output)
{
    private static readonly TopologyTestDeadline _deadline = new();

    /// <summary>Requires real canonical refusal without governed mutation or reservations.</summary>
    [Fact]
    public async Task GovernedCommandsShouldFailClosedWithoutAtomicParticipationInRealDaprTopology()
        => await _deadline.RunAsync(new TrivialGovernedCommandAspireE2eTests(output).RunGovernedCommandRefusalAsync,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

    /// <summary>Requires the authenticated production-client Chrome matrix for admitted Message/Ask-AI/Stop.</summary>
    [Fact]
    public async Task AuthenticatedProductionClientShouldFailClosedWithoutPersistingAcrossRequiredChromeMatrix()
        => await _deadline.RunAsync(new Story132ProductionBrowserAspireE2ETests(output).RunAuthenticatedProductionClientMatrixAsync,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

    /// <summary>Requires the real correction-propagation workflow runtime to become healthy.</summary>
    [Fact]
    public async Task CorrectionPropagationWorkflowRuntimeShouldBeHealthyInRealDaprTopology()
        => await _deadline.RunAsync(new TrivialGovernedCommandAspireE2eTests(output).RunCorrectionPropagationRuntimeHealthAsync,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
}
