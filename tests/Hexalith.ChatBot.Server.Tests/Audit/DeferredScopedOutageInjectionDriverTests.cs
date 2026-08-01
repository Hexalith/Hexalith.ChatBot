using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Coverage for the deliberately inert product default. Story 12.15 composes the separate live implementation only in
/// the opted-in Tier-3 harness; the default throws and the coordinator maps that to <c>unmeasurable</c>, never a
/// fabricated <c>contained</c>.
/// </summary>
public sealed class DeferredScopedOutageInjectionDriverTests
{
    [Fact]
    public async Task InjectAndMeasureAsyncRequiresTheOptedInTier3Harness()
    {
        DeferredScopedOutageInjectionDriver driver = new();

        NotSupportedException ex = await Should.ThrowAsync<NotSupportedException>(
            () => driver.InjectAndMeasureAsync(
                ScopedOutageDependencies.Graph,
                "replay-test:scoped-outage",
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TestContext.Current.CancellationToken).AsTask());

        ex.Message.ShouldContain("Tier-3 recovery harness");
    }
}
