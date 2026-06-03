using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.12 (Task 5, AC1, inert-control-floor) coverage for the wired-but-inert default rebuild driver. The live
/// rebuild runtime (replaying a tenant's immutable source records + WORM history into a fresh projection store against a
/// deployed AKS/Aspire environment) is M2-deferred, so the registered default <see cref="DeferredProjectionRebuildDriver"/>
/// throws <see cref="NotSupportedException"/> — unmistakably not yet live. This is the throw the coordinator's fail-safe
/// catch maps to an <c>unmeasurable</c> report rather than a fabricated <c>equivalent</c> (mirroring Story 9.4's deferred
/// replay-driver and Story 9.11's <see cref="DeferredContinuityDrillScenarioRunner"/> discipline).
/// </summary>
public sealed class DeferredProjectionRebuildDriverTests
{
    [Fact]
    public async Task RebuildAsyncThrowsNotSupportedBecauseLiveRebuildIsDeferred()
    {
        DeferredProjectionRebuildDriver driver = new();

        NotSupportedException ex = await Should.ThrowAsync<NotSupportedException>(
            () => driver.RebuildAsync(
                "replay-test:projection-rebuild",
                "baseline-dataset-1",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TestContext.Current.CancellationToken).AsTask());

        ex.Message.ShouldContain("M2-deferred");
    }
}
