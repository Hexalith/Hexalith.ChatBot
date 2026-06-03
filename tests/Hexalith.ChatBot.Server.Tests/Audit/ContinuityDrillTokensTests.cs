using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.11 (Task 1, AC1) coverage for the closed continuity-drill token vocabularies. Both
/// <see cref="ContinuityDrillScenarios"/> (the two NFR56-required scenarios the sweep runs) and
/// <see cref="ContinuityDrillVerdicts"/> (met/missed/unmeasurable) are fixed, bounded sets whose <c>Contains</c> membership
/// check biases an unknown/null token to "not a member" so the coordinator fails safe to <c>unmeasurable</c> rather than
/// fabricating a <c>met</c>. These pin the set contents and the null-safe membership contract directly (the coordinator
/// tests only exercise membership indirectly via the unknown-scenario path).
/// </summary>
public sealed class ContinuityDrillTokensTests
{
    [Fact]
    public void ScenariosClosedSetIsExactlyTheTwoNfr56Scenarios()
    {
        ContinuityDrillScenarios.All.ShouldBe(
            new[] { ContinuityDrillScenarios.EventStoreOutage, ContinuityDrillScenarios.M365SubscriptionFailure },
            ignoreOrder: true);
        ContinuityDrillScenarios.EventStoreOutage.ShouldBe("eventstore-outage");
        ContinuityDrillScenarios.M365SubscriptionFailure.ShouldBe("m365-subscription-failure");
    }

    [Theory]
    [InlineData(ContinuityDrillScenarios.EventStoreOutage, true)]
    [InlineData(ContinuityDrillScenarios.M365SubscriptionFailure, true)]
    [InlineData("unknown-scenario", false)]
    [InlineData("", false)]
    public void ScenariosContainsRecognizesOnlyKnownTokens(string scenario, bool expected)
        => ContinuityDrillScenarios.Contains(scenario).ShouldBe(expected);

    [Fact]
    public void ScenariosContainsIsNullSafe()
        => ContinuityDrillScenarios.Contains(null).ShouldBeFalse();

    [Fact]
    public void VerdictsClosedSetIsExactlyMetMissedUnmeasurable()
    {
        ContinuityDrillVerdicts.All.ShouldBe(
            new[] { ContinuityDrillVerdicts.Met, ContinuityDrillVerdicts.Missed, ContinuityDrillVerdicts.Unmeasurable },
            ignoreOrder: true);
        ContinuityDrillVerdicts.Met.ShouldBe("met");
        ContinuityDrillVerdicts.Missed.ShouldBe("missed");
        ContinuityDrillVerdicts.Unmeasurable.ShouldBe("unmeasurable");
    }

    [Theory]
    [InlineData(ContinuityDrillVerdicts.Met, true)]
    [InlineData(ContinuityDrillVerdicts.Missed, true)]
    [InlineData(ContinuityDrillVerdicts.Unmeasurable, true)]
    [InlineData("passed", false)]
    public void VerdictsContainsRecognizesOnlyKnownTokens(string verdict, bool expected)
        => ContinuityDrillVerdicts.Contains(verdict).ShouldBe(expected);

    [Fact]
    public void VerdictsContainsIsNullSafe()
        => ContinuityDrillVerdicts.Contains(null).ShouldBeFalse();
}
