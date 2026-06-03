using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.12 (Task 1, AC2/AC4) coverage for the closed <see cref="ProjectionRebuildVerdicts"/> vocabulary. The three
/// verdicts (equivalent/divergent/unmeasurable) are a fixed, bounded set whose null-safe <c>Contains</c> biases an
/// unknown/null token to "not a member" — so a verdict can never be silently accepted as equivalent. These pin the set
/// contents, the exact literal values (which deliberately avoid the legacy-lifecycle tokens so no scaffold allowlist entry
/// is needed), and the null-safe membership contract directly (the coordinator/evaluator tests only exercise the literals
/// indirectly). Mirrors <see cref="ContinuityDrillTokensTests"/>.
/// </summary>
public sealed class ProjectionRebuildVerdictsTests
{
    [Fact]
    public void ClosedSetIsExactlyEquivalentDivergentUnmeasurable()
    {
        ProjectionRebuildVerdicts.All.ShouldBe(
            new[] { ProjectionRebuildVerdicts.Equivalent, ProjectionRebuildVerdicts.Divergent, ProjectionRebuildVerdicts.Unmeasurable },
            ignoreOrder: true);
        ProjectionRebuildVerdicts.Equivalent.ShouldBe("equivalent");
        ProjectionRebuildVerdicts.Divergent.ShouldBe("divergent");
        ProjectionRebuildVerdicts.Unmeasurable.ShouldBe("unmeasurable");
    }

    [Fact]
    public void LiteralsAvoidTheLegacyLifecycleTokens()
    {
        // The verdicts deliberately avoid pending/accepted/running/succeeded/cancelled so ScaffoldArchitectureTests does
        // not flag them and no allowlist entry is needed (Task 1) — guard that intent here directly.
        string[] legacyLifecycle = ["pending", "accepted", "running", "succeeded", "cancelled"];
        foreach (string verdict in ProjectionRebuildVerdicts.All)
        {
            legacyLifecycle.ShouldNotContain(verdict);
        }
    }

    [Theory]
    [InlineData(ProjectionRebuildVerdicts.Equivalent, true)]
    [InlineData(ProjectionRebuildVerdicts.Divergent, true)]
    [InlineData(ProjectionRebuildVerdicts.Unmeasurable, true)]
    [InlineData("passed", false)]
    [InlineData("", false)]
    public void ContainsRecognizesOnlyKnownTokens(string verdict, bool expected)
        => ProjectionRebuildVerdicts.Contains(verdict).ShouldBe(expected);

    [Fact]
    public void ContainsIsNullSafe()
        => ProjectionRebuildVerdicts.Contains(null).ShouldBeFalse();
}
