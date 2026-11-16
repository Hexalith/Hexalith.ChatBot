using Hexalith.ChatBot.IntegrationTests.Recovery;

namespace Hexalith.ChatBot.Recovery.Tests;

/// <summary>Runs the complete destructive recovery sweep in the weekly nightly hook.</summary>
[Trait("Category", "E2E")]
public sealed class LiveContinuityAspireE2eTests
{
    /// <summary>Preserves every recovery scenario, behavioral assertion and restoration boundary.</summary>
    [Fact]
    public async Task LiveRecoveryValidationRunsAllThreeCoordinatorsAndPassesEvidenceGate()
        => await new LiveRecoverySweep().RunAsync().ConfigureAwait(true);
}
