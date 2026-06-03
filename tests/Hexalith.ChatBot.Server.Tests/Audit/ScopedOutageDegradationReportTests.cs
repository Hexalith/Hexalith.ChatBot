using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.13 (Task 4, AC1/AC4) coverage for the <see cref="ScopedOutageDegradationReport"/> fail-safe
/// <c>Unmeasurable</c> factory and the <c>IsBreach</c>/<c>IsScopeBreach</c> folds. The coordinator tests exercise these
/// through the driver paths; this fixture pins the factory's exact field values and the three-dimension breach fold
/// directly (mirroring the <see cref="ProjectionRebuildReportTests"/> / <c>ContinuityDrillReport</c> twins).
/// </summary>
public sealed class ScopedOutageDegradationReportTests
{
    private const string Tenant = "replay-test:scoped-outage";
    private const string Dependency = ScopedOutageDependencies.Graph;
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset Started = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UnmeasurableFactoryProducesTheFailSafeBreachReportNeverAFabricatedContained()
    {
        DateTimeOffset ended = Started + TimeSpan.FromMinutes(5);

        ScopedOutageDegradationReport report = ScopedOutageDegradationReport.Unmeasurable(Tenant, Dependency, Correlation, Started, ended);

        report.TenantRef.ShouldBe(Tenant);
        report.Dependency.ShouldBe(Dependency);
        report.CorrelationId.ShouldBe(Correlation);
        report.StartedAtUtc.ShouldBe(Started);
        report.EndedAtUtc.ShouldBe(ended);
        report.Verdict.ShouldBe(ScopedOutageDegradationVerdicts.Unmeasurable);
        report.ExpectedScope.ShouldBe(ScopedOutageScopes.Tenant);
        report.ObservedScope.ShouldBe(ScopedOutageScopes.Tenant);
        report.ScopeRecordingLatency.ShouldBe(TimeSpan.Zero);
        report.ScopeRecordedWithinTarget.ShouldBeFalse();
        report.Deviations.ShouldBe([ScopedOutageDegradationReport.IncompleteDeviation]);
        report.FirstBreachLocator.ShouldBeNull();
        report.ReasonCode.ShouldBe(ScopedOutageDegradationReport.ValidationUnmeasurableReasonCode);

        // Unmeasurable is the fail-safe breach, NOT a serious isolation/scope failure.
        report.IsBreach.ShouldBeTrue();
        report.IsScopeBreach.ShouldBeFalse();
    }

    [Fact]
    public void ContainedWithinTargetIsNotABreach()
    {
        ScopedOutageDegradationReport report = Contained(ScopeRecordedWithinTarget: true, ScopedOutageDegradationVerdicts.Contained, []);

        report.IsBreach.ShouldBeFalse();
        report.IsScopeBreach.ShouldBeFalse();
    }

    [Fact]
    public void ContainedButLateRecordingIsABreachButNotAScopeBreach()
    {
        // A contained-but-slow degradation stays contained with ScopeRecordedWithinTarget == false — a recording miss.
        ScopedOutageDegradationReport report = Contained(
            ScopeRecordedWithinTarget: false,
            ScopedOutageDegradationVerdicts.Contained,
            [ScopedOutageDegradationEvaluator.ScopeRecordingExceededDeviation]);

        report.IsBreach.ShouldBeTrue(); // a recording miss is still a breach to surface
        report.IsScopeBreach.ShouldBeFalse(); // ...but it is NOT a serious isolation/scope failure
    }

    [Fact]
    public void BreachedIsBothABreachAndAScopeBreach()
    {
        ScopedOutageDegradationReport report = Contained(
            ScopeRecordedWithinTarget: true,
            ScopedOutageDegradationVerdicts.Breached,
            [ScopedOutageDegradationEvaluator.CrossTenantLeakageDeviation]);

        report.IsBreach.ShouldBeTrue();
        report.IsScopeBreach.ShouldBeTrue();
    }

    private static ScopedOutageDegradationReport Contained(bool ScopeRecordedWithinTarget, string verdict, IReadOnlyList<string> deviations)
        => new(
            Tenant,
            Dependency,
            ScopedOutageScopes.Mailbox,
            ScopedOutageScopes.Mailbox,
            Started,
            Started + TimeSpan.FromMinutes(2),
            ScopeRecordingLatency: TimeSpan.FromMinutes(1),
            ScopeRecordedWithinTarget,
            verdict,
            deviations,
            FirstBreachLocator: null,
            Correlation,
            ScopedOutageDegradationReport.ValidationCompletedReasonCode);
}
