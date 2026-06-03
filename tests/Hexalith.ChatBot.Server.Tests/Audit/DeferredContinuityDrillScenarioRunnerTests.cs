using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.11 (Task 4, AC1, inert-control-floor) coverage for the wired-but-inert default scenario runner. The live
/// fault-injection runtime (downing a real EventStore / lapsing a real M365 Graph subscription) is M2-deferred, so the
/// registered default <see cref="DeferredContinuityDrillScenarioRunner"/> throws <see cref="NotSupportedException"/> —
/// unmistakably not yet live. This is the throw the coordinator's fail-safe catch maps to an <c>unmeasurable</c> report
/// rather than a fabricated <c>met</c> (mirroring Story 9.4's deferred replay-driver discipline).
/// </summary>
public sealed class DeferredContinuityDrillScenarioRunnerTests
{
    [Fact]
    public async Task RunAsyncThrowsNotSupportedBecauseLiveFaultInjectionIsDeferred()
    {
        DeferredContinuityDrillScenarioRunner runner = new();

        NotSupportedException ex = await Should.ThrowAsync<NotSupportedException>(
            () => runner.RunAsync(
                ContinuityDrillScenarios.EventStoreOutage,
                "replay-test:continuity-drill",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TestContext.Current.CancellationToken).AsTask());

        ex.Message.ShouldContain("M2-deferred");
    }
}
