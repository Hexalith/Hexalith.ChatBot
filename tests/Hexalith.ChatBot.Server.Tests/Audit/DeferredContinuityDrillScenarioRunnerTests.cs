using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Coverage for the deliberately inert product default. Story 12.15 composes the separate live implementation only in
/// the opted-in Tier-3 harness; the default throws and the coordinator maps that to <c>unmeasurable</c>, never a
/// fabricated <c>met</c>.
/// </summary>
public sealed class DeferredContinuityDrillScenarioRunnerTests
{
    [Fact]
    public async Task RunAsyncRequiresTheOptedInTier3Harness()
    {
        DeferredContinuityDrillScenarioRunner runner = new();

        NotSupportedException ex = await Should.ThrowAsync<NotSupportedException>(
            () => runner.RunAsync(
                ContinuityDrillScenarios.EventStoreOutage,
                "replay-test:continuity-drill",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TestContext.Current.CancellationToken).AsTask());

        ex.Message.ShouldContain("Tier-3 recovery harness");
    }
}
