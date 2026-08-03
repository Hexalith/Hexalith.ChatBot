using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>Story 12.15 Task 2 guards that canonical reports are retained before aggregate-only outcomes return.</summary>
public sealed class RecoveryValidationEvidenceSinkCoordinatorTests
{
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TestTenant = "replay-test:recovery-validation";
    private static readonly DateTimeOffset Now = WormAuditTestData.FixedNow;

    [Fact]
    public async Task EveryCoordinatorSweepRetainsEveryCanonicalReport()
    {
        CapturingSink sink = new();
        WormAuditTestData.FixedClock clock = new(Now);

        ContinuityDrillCoordinator continuity = new(
            new ContinuityRunner(),
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            clock,
            sink);
        ProjectionRebuildValidationCoordinator rebuild = new(
            new RebuildDriver(),
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            clock,
            sink);
        ScopedOutageDegradationValidationCoordinator outage = new(
            new OutageDriver(),
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            clock,
            sink);

        _ = await continuity.RunAllScenariosAsync(TestTenant, Correlation, TestContext.Current.CancellationToken);
        _ = await rebuild.RunAllAsync(TestTenant, ["recovery-baseline-v1"], Correlation, TestContext.Current.CancellationToken);
        _ = await outage.RunAllScenariosAsync(TestTenant, Correlation, TestContext.Current.CancellationToken);

        sink.ContinuityReports.Count.ShouldBe(ContinuityDrillScenarios.All.Count);
        sink.RebuildReports.Count.ShouldBe(1);
        sink.ScopedOutageReports.Count.ShouldBe(ScopedOutageDependencies.All.Count);
        sink.Events.Count.ShouldBe(ContinuityDrillScenarios.All.Count + 1 + ScopedOutageDependencies.All.Count);
    }

    [Fact]
    public async Task EvidenceSinkFailureTurnsOtherwiseMetReportIntoUnmeasurableBreach()
    {
        ContinuityDrillCoordinator coordinator = new(
            new ContinuityRunner(),
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            new WormAuditTestData.FixedClock(Now),
            new ThrowingSink());

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            TestTenant,
            Correlation,
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        report.IsBreach.ShouldBeTrue();
        report.Deviations.ShouldContain(ContinuityDrillReport.EvidenceRetentionFailedDeviation);
    }

    private sealed class CapturingSink : IRecoveryValidationEvidenceSink
    {
        public List<string> Events { get; } = [];
        public List<ContinuityDrillReport> ContinuityReports { get; } = [];
        public List<ProjectionRebuildReport> RebuildReports { get; } = [];
        public List<ScopedOutageDegradationReport> ScopedOutageReports { get; } = [];

        public ValueTask RecordAsync(ContinuityDrillReport report, CancellationToken cancellationToken)
        {
            ContinuityReports.Add(report);
            Events.Add($"continuity:{report.Scenario}");
            return ValueTask.CompletedTask;
        }

        public ValueTask RecordAsync(ProjectionRebuildReport report, CancellationToken cancellationToken)
        {
            RebuildReports.Add(report);
            Events.Add($"rebuild:{report.DatasetRef}");
            return ValueTask.CompletedTask;
        }

        public ValueTask RecordAsync(ScopedOutageDegradationReport report, CancellationToken cancellationToken)
        {
            ScopedOutageReports.Add(report);
            Events.Add($"outage:{report.Dependency}");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingSink : IRecoveryValidationEvidenceSink
    {
        public ValueTask RecordAsync(ContinuityDrillReport report, CancellationToken cancellationToken)
            => throw new IOException("evidence unavailable");

        public ValueTask RecordAsync(ProjectionRebuildReport report, CancellationToken cancellationToken)
            => throw new IOException("evidence unavailable");

        public ValueTask RecordAsync(ScopedOutageDegradationReport report, CancellationToken cancellationToken)
            => throw new IOException("evidence unavailable");
    }

    private sealed class ContinuityRunner : IContinuityDrillScenarioRunner
    {
        public ValueTask<ContinuityDrillMeasurement> RunAsync(string scenario, string testTenantRef, string correlationId, CancellationToken cancellationToken)
            => ValueTask.FromResult(new ContinuityDrillMeasurement(Now, Now + TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), false));
    }

    private sealed class RebuildDriver : IProjectionRebuildDriver
    {
        public ValueTask<ProjectionRebuildMeasurement> RebuildAsync(string testTenantRef, string datasetRef, string correlationId, CancellationToken cancellationToken)
        {
            ProjectionResourceDigest digest = ProjectionResourceDigest.Create("resource-1", "state-v1");
            return ValueTask.FromResult(new ProjectionRebuildMeasurement(
                Now,
                Now + TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                [digest],
                [digest],
                GovernedOperationView.CurrentSchemaVersion,
                GovernedOperationView.CurrentSchemaVersion));
        }
    }

    private sealed class OutageDriver : IScopedOutageInjectionDriver
    {
        public ValueTask<ScopedOutageDegradationMeasurement> InjectAndMeasureAsync(string dependency, string testTenantRef, string correlationId, CancellationToken cancellationToken)
            => ValueTask.FromResult(new ScopedOutageDegradationMeasurement(
                ScopedOutageScopes.Tenant,
                ScopedOutageScopes.Tenant,
                CrossTenantLeakageDetected: false,
                UnauthorizedMutationDetected: false,
                SilentDataLossDetected: false,
                InflightItemsRecoverable: true,
                DuplicateSideEffectDetected: false,
                ScopeRecordingLatency: TimeSpan.FromSeconds(1),
                Now,
                Now + TimeSpan.FromSeconds(2)));
    }
}
