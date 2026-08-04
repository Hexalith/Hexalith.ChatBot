using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
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
        List<string> callOrder = [];
        OrderTrackingAuditWriter auditWriter = new(callOrder);
        OrderTrackingAlertSink alertSink = new(callOrder);
        ContinuityDrillCoordinator coordinator = new(
            new ContinuityRunner(),
            auditWriter,
            alertSink,
            new WormAuditTestData.FixedClock(Now),
            new OrderTrackingSink(callOrder, new ThrowingSink()));

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            TestTenant,
            Correlation,
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        report.IsBreach.ShouldBeTrue();
        report.Deviations.ShouldContain(ContinuityDrillReport.EvidenceRetentionFailedDeviation);
        report.ExecutionAssertions.ShouldNotBeNull();
        // Retention runs before audit-then-alert: the envelope/alert must describe the substituted unmeasurable
        // retention-failure report, not the pre-retention met measurement.
        auditWriter.Envelopes.ShouldHaveSingleItem();
        alertSink.Alerts.ShouldHaveSingleItem().Kind.ShouldBe(OperatorAlertKind.ContinuityDrillTargetMissed);
        AssertRetentionRanBeforeAuditThenAlert(callOrder);
    }

    [Fact]
    public async Task EvidenceSinkFailureTurnsOtherwiseEquivalentRebuildIntoUnmeasurableBreach()
    {
        List<string> callOrder = [];
        OrderTrackingAuditWriter auditWriter = new(callOrder);
        OrderTrackingAlertSink alertSink = new(callOrder);
        ProjectionRebuildValidationCoordinator coordinator = new(
            new RebuildDriver(),
            auditWriter,
            alertSink,
            new WormAuditTestData.FixedClock(Now),
            new OrderTrackingSink(callOrder, new ThrowingSink()));

        ProjectionRebuildReport report = await coordinator.RunValidationAndRecordAsync(
            TestTenant,
            "recovery-baseline-v1",
            Correlation,
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ProjectionRebuildVerdicts.Unmeasurable);
        report.IsBreach.ShouldBeTrue();
        report.Deviations.ShouldContain(ProjectionRebuildReport.EvidenceRetentionFailedDeviation);
        report.ExecutionAssertions.ShouldNotBeNull();
        auditWriter.Envelopes.ShouldHaveSingleItem();
        alertSink.Alerts.ShouldHaveSingleItem().Kind.ShouldBe(OperatorAlertKind.ProjectionRebuildValidationFailed);
        AssertRetentionRanBeforeAuditThenAlert(callOrder);
    }

    [Fact]
    public async Task EvidenceSinkFailureTurnsOtherwiseContainedOutageIntoUnmeasurableBreach()
    {
        List<string> callOrder = [];
        OrderTrackingAuditWriter auditWriter = new(callOrder);
        OrderTrackingAlertSink alertSink = new(callOrder);
        ScopedOutageDegradationValidationCoordinator coordinator = new(
            new OutageDriver(),
            auditWriter,
            alertSink,
            new WormAuditTestData.FixedClock(Now),
            new OrderTrackingSink(callOrder, new ThrowingSink()));

        ScopedOutageDegradationReport report = await coordinator.RunScenarioAndRecordAsync(
            ScopedOutageDependencies.CommandExecution,
            TestTenant,
            Correlation,
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ScopedOutageDegradationVerdicts.Unmeasurable);
        report.IsBreach.ShouldBeTrue();
        report.Deviations.ShouldContain(ScopedOutageDegradationReport.EvidenceRetentionFailedDeviation);
        report.ExecutionAssertions.ShouldNotBeNull();
        auditWriter.Envelopes.ShouldHaveSingleItem();
        alertSink.Alerts.ShouldHaveSingleItem().Kind.ShouldBe(OperatorAlertKind.ScopedOutageDegradationBreach);
        AssertRetentionRanBeforeAuditThenAlert(callOrder);
    }

    [Fact]
    public async Task EvidenceRetentionFailureClampsEndedAtUtcToStartedAtUtcWhenTheClockAppearsToRetreat()
    {
        // The completed measurement's StartedAtUtc is ahead of the coordinator's own clock — a clock-skew shape the
        // fixed-clock fixtures above cannot exercise (there, clock.UtcNow always equals the measurement's
        // StartedAtUtc, so `endedAtUtc < report.StartedAtUtc` is never true, merely equal).
        DateTimeOffset futureStartedAtUtc = Now + TimeSpan.FromMinutes(5);
        ContinuityDrillCoordinator coordinator = new(
            new FutureStartedContinuityRunner(futureStartedAtUtc),
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
        report.StartedAtUtc.ShouldBe(futureStartedAtUtc);
        report.EndedAtUtc.ShouldBe(futureStartedAtUtc);
        (report.EndedAtUtc >= report.StartedAtUtc).ShouldBeTrue("EndedAtUtc must never precede StartedAtUtc, even under clock skew.");
    }

    /// <summary>
    /// Asserts the actual recorded call sequence — not just final counts/shape, which a swapped
    /// audit/alert-before-retain order would also satisfy — puts every "retain" call (the evidence sink, retried once
    /// on failure) strictly before "audit" (<see cref="IAuditWriter"/>), which itself precedes "alert"
    /// (<see cref="IOperatorAlertSink"/>).
    /// </summary>
    private static void AssertRetentionRanBeforeAuditThenAlert(List<string> callOrder)
    {
        callOrder.ShouldContain("retain");
        callOrder.ShouldContain("audit");
        callOrder.ShouldContain("alert");
        int lastRetain = callOrder.LastIndexOf("retain");
        int audit = callOrder.IndexOf("audit");
        int alert = callOrder.IndexOf("alert");
        (audit > lastRetain).ShouldBeTrue("audit must run after every retention attempt, not before.");
        (alert > audit).ShouldBeTrue("alert must run after audit, not before or interleaved ahead of it.");
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

    /// <summary>
    /// A representative "everything observed cleanly" fixture, so the sink-failure tests can assert this measurement's
    /// <see cref="RecoveryValidationExecutionAssertions"/> survives into the retention-failure fallback report rather
    /// than being silently dropped to <see langword="null"/>.
    /// </summary>
    private static readonly RecoveryValidationExecutionAssertions SampleExecutionAssertions = new(
        CleanupComplete: true,
        FaultObserved: true,
        RecoveryObserved: true,
        IndependentControlSucceeded: true,
        TenantIsolationPreserved: true,
        UnauthorizedMutationAbsent: true,
        StateReconstructable: true,
        ImmutableSourceOnly: true,
        MailboxReingestionAbsent: false);

    private sealed class ContinuityRunner : IContinuityDrillScenarioRunner
    {
        public ValueTask<ContinuityDrillMeasurement> RunAsync(string scenario, string testTenantRef, string correlationId, CancellationToken cancellationToken)
            => ValueTask.FromResult(new ContinuityDrillMeasurement(
                Now,
                Now + TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                false,
                SampleExecutionAssertions));
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
                GovernedOperationView.CurrentSchemaVersion,
                SampleExecutionAssertions));
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
                Now + TimeSpan.FromSeconds(2),
                SampleExecutionAssertions));
    }

    /// <summary>Reports a <see cref="ContinuityDrillMeasurement.StartedAtUtc"/> ahead of the coordinator's clock.</summary>
    private sealed class FutureStartedContinuityRunner(DateTimeOffset startedAtUtc) : IContinuityDrillScenarioRunner
    {
        public ValueTask<ContinuityDrillMeasurement> RunAsync(string scenario, string testTenantRef, string correlationId, CancellationToken cancellationToken)
            => ValueTask.FromResult(new ContinuityDrillMeasurement(
                startedAtUtc,
                startedAtUtc + TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                false,
                SampleExecutionAssertions));
    }

    private sealed class OrderTrackingSink(List<string> callOrder, IRecoveryValidationEvidenceSink inner) : IRecoveryValidationEvidenceSink
    {
        public ValueTask RecordAsync(ContinuityDrillReport report, CancellationToken cancellationToken)
        {
            callOrder.Add("retain");
            return inner.RecordAsync(report, cancellationToken);
        }

        public ValueTask RecordAsync(ProjectionRebuildReport report, CancellationToken cancellationToken)
        {
            callOrder.Add("retain");
            return inner.RecordAsync(report, cancellationToken);
        }

        public ValueTask RecordAsync(ScopedOutageDegradationReport report, CancellationToken cancellationToken)
        {
            callOrder.Add("retain");
            return inner.RecordAsync(report, cancellationToken);
        }
    }

    private sealed class OrderTrackingAuditWriter(List<string> callOrder) : IAuditWriter
    {
        private readonly InMemoryAuditWriter _inner = new();

        public IReadOnlyList<AuditEnvelope> Envelopes => _inner.Envelopes;

        public ValueTask RecordAuthorizationFailureAsync(ChatBotAuthorizationFailureAuditFact fact, CancellationToken cancellationToken)
        {
            callOrder.Add("audit");
            return _inner.RecordAuthorizationFailureAsync(fact, cancellationToken);
        }

        public ValueTask<AuditWriteResult> RecordPreCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            callOrder.Add("audit");
            return _inner.RecordPreCommitAsync(envelope, cancellationToken);
        }

        public ValueTask<AuditWriteResult> RecordPostCommitAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
        {
            callOrder.Add("audit");
            return _inner.RecordPostCommitAsync(envelope, cancellationToken);
        }
    }

    private sealed class OrderTrackingAlertSink(List<string> callOrder) : IOperatorAlertSink
    {
        private readonly InMemoryOperatorAlertSink _inner = new();

        public IReadOnlyList<OperatorAlert> Alerts => _inner.Alerts;

        public ValueTask EmitAsync(OperatorAlert alert, CancellationToken cancellationToken)
        {
            callOrder.Add("alert");
            return _inner.EmitAsync(alert, cancellationToken);
        }
    }
}
