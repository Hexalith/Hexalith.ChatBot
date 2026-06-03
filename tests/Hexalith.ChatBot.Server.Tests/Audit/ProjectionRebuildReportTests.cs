using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.12 (Task 4, AC1/AC4) coverage for the <see cref="ProjectionRebuildReport"/> fail-safe <c>Unmeasurable</c>
/// factory and the <c>IsBreach</c>/<c>IsDivergent</c> folds. The coordinator tests exercise these through the driver
/// paths; this fixture pins the factory's exact field values and the three-dimension breach fold directly (mirroring the
/// <c>AuditCompletenessMeasurement</c>/<c>ContinuityDrillReport</c> twins).
/// </summary>
public sealed class ProjectionRebuildReportTests
{
    private const string Tenant = "replay-test:projection-rebuild";
    private const string Dataset = "baseline-dataset-1";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string SchemaVersion = "chatbot.governed-operation-view.v1";
    private static readonly DateTimeOffset Started = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UnmeasurableFactoryProducesTheFailSafeBreachReportNeverAFabricatedEquivalent()
    {
        DateTimeOffset ended = Started + TimeSpan.FromMinutes(5);

        ProjectionRebuildReport report = ProjectionRebuildReport.Unmeasurable(Tenant, Dataset, Correlation, Started, ended, SchemaVersion);

        report.TenantRef.ShouldBe(Tenant);
        report.DatasetRef.ShouldBe(Dataset);
        report.CorrelationId.ShouldBe(Correlation);
        report.StartedAtUtc.ShouldBe(Started);
        report.EndedAtUtc.ShouldBe(ended);
        report.Verdict.ShouldBe(ProjectionRebuildVerdicts.Unmeasurable);
        report.MeasuredRebuildDuration.ShouldBe(TimeSpan.Zero);
        report.DurationWithinTarget.ShouldBeFalse();
        report.ResourcesCompared.ShouldBe(0);
        report.Deviations.ShouldBe([ProjectionRebuildReport.IncompleteDeviation]);
        report.FirstDivergingResourceLocator.ShouldBeNull();
        report.ProjectionSchemaVersion.ShouldBe(SchemaVersion);
        report.ReasonCode.ShouldBe(ProjectionRebuildReport.ValidationUnmeasurableReasonCode);

        // Unmeasurable is the fail-safe breach, NOT a determinism failure.
        report.IsBreach.ShouldBeTrue();
        report.IsDivergent.ShouldBeFalse();
    }

    [Fact]
    public void EquivalentWithinTargetIsNotABreach()
    {
        ProjectionRebuildReport report = new(
            Tenant,
            Dataset,
            Started,
            Started + TimeSpan.FromHours(1),
            MeasuredRebuildDuration: TimeSpan.FromHours(1),
            DurationWithinTarget: true,
            ProjectionRebuildVerdicts.Equivalent,
            ResourcesCompared: 2,
            Deviations: [],
            FirstDivergingResourceLocator: null,
            SchemaVersion,
            Correlation,
            ProjectionRebuildReport.ValidationCompletedReasonCode);

        report.IsBreach.ShouldBeFalse();
        report.IsDivergent.ShouldBeFalse();
    }

    [Fact]
    public void DeterministicButSlowIsABreachButNotDivergent()
    {
        // A deterministic-but-slow rebuild stays equivalent with DurationWithinTarget == false — a recovery-time miss.
        ProjectionRebuildReport report = new(
            Tenant,
            Dataset,
            Started,
            Started + RecoveryTargets.MaxRto + TimeSpan.FromMinutes(30),
            MeasuredRebuildDuration: RecoveryTargets.MaxRto + TimeSpan.FromMinutes(30),
            DurationWithinTarget: false,
            ProjectionRebuildVerdicts.Equivalent,
            ResourcesCompared: 2,
            Deviations: [ProjectionRebuildEquivalenceEvaluator.DurationExceededDeviation],
            FirstDivergingResourceLocator: null,
            SchemaVersion,
            Correlation,
            ProjectionRebuildReport.ValidationCompletedReasonCode);

        report.IsBreach.ShouldBeTrue(); // a recovery-time miss is still a breach to surface
        report.IsDivergent.ShouldBeFalse(); // ...but it is NOT a determinism failure
    }
}
