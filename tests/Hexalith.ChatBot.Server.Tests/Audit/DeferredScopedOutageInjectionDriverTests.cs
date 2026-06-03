using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.13 (Task 5, AC1, inert-control-floor) coverage for the wired-but-inert default injection driver. The live
/// fault-injection runtime (downing a real Graph/identity/AI/command/audit/attachment dependency against a deployed
/// AKS/Aspire environment and measuring the real degradation) is M2-deferred, so the registered default
/// <see cref="DeferredScopedOutageInjectionDriver"/> throws <see cref="NotSupportedException"/> — unmistakably not yet
/// live. This is the throw the coordinator's fail-safe catch maps to an <c>unmeasurable</c> report rather than a
/// fabricated <c>contained</c> (mirroring Story 9.4's deferred replay-driver and Story 9.11/9.12's deferred runner/driver
/// discipline).
/// </summary>
public sealed class DeferredScopedOutageInjectionDriverTests
{
    [Fact]
    public async Task InjectAndMeasureAsyncThrowsNotSupportedBecauseLiveFaultInjectionIsDeferred()
    {
        DeferredScopedOutageInjectionDriver driver = new();

        NotSupportedException ex = await Should.ThrowAsync<NotSupportedException>(
            () => driver.InjectAndMeasureAsync(
                ScopedOutageDependencies.Graph,
                "replay-test:scoped-outage",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TestContext.Current.CancellationToken).AsTask());

        ex.Message.ShouldContain("M2-deferred");
    }
}
