using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.12 (AC2/AC4) coverage for the pure <see cref="ProjectionRebuildEquivalenceEvaluator"/>: equivalent only when
/// schema version + key set + every structural token match; divergent when any single dimension differs (token, missing
/// key, extra key, or schema version); a deterministic first-diverging locator in the pre-rebuild snapshot's stable
/// order; and the exact deviation tokens for each verdict/duration combination. The evaluator consumes no clock or IO.
/// </summary>
public sealed class ProjectionRebuildEquivalenceEvaluatorTests
{
    private const string SchemaV1 = "chatbot.governed-operation-view.v1";
    private const string SchemaV2 = "chatbot.governed-operation-view.v2";

    private static readonly IReadOnlyList<ProjectionResourceDigest> Baseline =
    [
        ProjectionResourceDigest.Create("resource-a", "token-a"),
        ProjectionResourceDigest.Create("resource-b", "token-b"),
        ProjectionResourceDigest.Create("resource-c", "token-c"),
    ];

    [Fact]
    public void EquivalentWhenSchemaKeySetAndAllTokensMatch()
    {
        // A reordered rebuilt snapshot is still equivalent (comparison is order-independent on the key set).
        IReadOnlyList<ProjectionResourceDigest> rebuilt =
        [
            ProjectionResourceDigest.Create("resource-c", "token-c"),
            ProjectionResourceDigest.Create("resource-a", "token-a"),
            ProjectionResourceDigest.Create("resource-b", "token-b"),
        ];

        ProjectionRebuildEquivalenceEvaluator.Evaluate(Baseline, rebuilt, SchemaV1, SchemaV1)
            .ShouldBe(ProjectionRebuildVerdicts.Equivalent);
        ProjectionRebuildEquivalenceEvaluator.FirstDivergingResourceLocator(Baseline, rebuilt).ShouldBeNull();
    }

    [Fact]
    public void DivergentWhenAStructuralTokenDiffers()
    {
        IReadOnlyList<ProjectionResourceDigest> rebuilt =
        [
            ProjectionResourceDigest.Create("resource-a", "token-a"),
            ProjectionResourceDigest.Create("resource-b", "token-CHANGED"),
            ProjectionResourceDigest.Create("resource-c", "token-c"),
        ];

        ProjectionRebuildEquivalenceEvaluator.Evaluate(Baseline, rebuilt, SchemaV1, SchemaV1)
            .ShouldBe(ProjectionRebuildVerdicts.Divergent);
        ProjectionRebuildEquivalenceEvaluator.FirstDivergingResourceLocator(Baseline, rebuilt)
            .ShouldBe("resource:resource-b");
    }

    [Fact]
    public void DivergentWhenAKeyIsMissingFromRebuilt()
    {
        IReadOnlyList<ProjectionResourceDigest> rebuilt =
        [
            ProjectionResourceDigest.Create("resource-a", "token-a"),
            ProjectionResourceDigest.Create("resource-c", "token-c"),
        ];

        ProjectionRebuildEquivalenceEvaluator.Evaluate(Baseline, rebuilt, SchemaV1, SchemaV1)
            .ShouldBe(ProjectionRebuildVerdicts.Divergent);
        // resource-b is the first pre-rebuild-order key missing from the rebuilt snapshot.
        ProjectionRebuildEquivalenceEvaluator.FirstDivergingResourceLocator(Baseline, rebuilt)
            .ShouldBe("resource:resource-b");
    }

    [Fact]
    public void DivergentWhenAKeyIsExtraInRebuilt()
    {
        IReadOnlyList<ProjectionResourceDigest> rebuilt =
        [
            ProjectionResourceDigest.Create("resource-a", "token-a"),
            ProjectionResourceDigest.Create("resource-b", "token-b"),
            ProjectionResourceDigest.Create("resource-c", "token-c"),
            ProjectionResourceDigest.Create("resource-d", "token-d"),
        ];

        ProjectionRebuildEquivalenceEvaluator.Evaluate(Baseline, rebuilt, SchemaV1, SchemaV1)
            .ShouldBe(ProjectionRebuildVerdicts.Divergent);
        // Every pre-rebuild resource matches; the first EXTRA resource is the locator.
        ProjectionRebuildEquivalenceEvaluator.FirstDivergingResourceLocator(Baseline, rebuilt)
            .ShouldBe("resource:resource-d");
    }

    [Fact]
    public void DivergentWhenSchemaVersionDiffersEvenIfSnapshotsMatch()
    {
        // Identical snapshots, but the rebuilt schema version differs — the event-upcasting / schema-churn divergence.
        ProjectionRebuildEquivalenceEvaluator.Evaluate(Baseline, Baseline, SchemaV1, SchemaV2)
            .ShouldBe(ProjectionRebuildVerdicts.Divergent);
    }

    [Fact]
    public void FirstDivergingResourceLocatorIsDeterministicAcrossRuns()
    {
        IReadOnlyList<ProjectionResourceDigest> rebuilt =
        [
            ProjectionResourceDigest.Create("resource-a", "token-CHANGED"),
            ProjectionResourceDigest.Create("resource-b", "token-CHANGED"),
            ProjectionResourceDigest.Create("resource-c", "token-c"),
        ];

        string? first = ProjectionRebuildEquivalenceEvaluator.FirstDivergingResourceLocator(Baseline, rebuilt);
        string? second = ProjectionRebuildEquivalenceEvaluator.FirstDivergingResourceLocator(Baseline, rebuilt);

        first.ShouldBe("resource:resource-a"); // the FIRST pre-rebuild-order resource that diverges, not resource-b
        second.ShouldBe(first); // re-running over the same inputs yields the same locator (pure/deterministic)
    }

    [Fact]
    public void TwoEmptySnapshotsWithMatchingSchemaAreEquivalent()
    {
        // An empty derived projection rebuilt to an empty projection is trivially equivalent (same key set: none).
        IReadOnlyList<ProjectionResourceDigest> empty = [];

        ProjectionRebuildEquivalenceEvaluator.Evaluate(empty, empty, SchemaV1, SchemaV1)
            .ShouldBe(ProjectionRebuildVerdicts.Equivalent);
        ProjectionRebuildEquivalenceEvaluator.FirstDivergingResourceLocator(empty, empty).ShouldBeNull();
    }

    [Fact]
    public void DeviationsEnumerateDivergedAndDurationExceededForRelevantCombos()
    {
        ProjectionRebuildEquivalenceEvaluator.Deviations(ProjectionRebuildVerdicts.Equivalent, durationWithinTarget: true)
            .ShouldBeEmpty();

        ProjectionRebuildEquivalenceEvaluator.Deviations(ProjectionRebuildVerdicts.Divergent, durationWithinTarget: true)
            .ShouldBe([ProjectionRebuildEquivalenceEvaluator.DivergedDeviation]);

        // Deterministic-but-slow: equivalent verdict, duration overrun only.
        ProjectionRebuildEquivalenceEvaluator.Deviations(ProjectionRebuildVerdicts.Equivalent, durationWithinTarget: false)
            .ShouldBe([ProjectionRebuildEquivalenceEvaluator.DurationExceededDeviation]);

        // Both dimensions, in stable order.
        ProjectionRebuildEquivalenceEvaluator.Deviations(ProjectionRebuildVerdicts.Divergent, durationWithinTarget: false)
            .ShouldBe(
            [
                ProjectionRebuildEquivalenceEvaluator.DivergedDeviation,
                ProjectionRebuildEquivalenceEvaluator.DurationExceededDeviation,
            ]);
    }
}
