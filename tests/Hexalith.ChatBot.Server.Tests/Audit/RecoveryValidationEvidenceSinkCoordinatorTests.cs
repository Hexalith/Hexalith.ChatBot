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
        CapturingMarkerSink markers = new(callOrder);
        ContinuityDrillCoordinator coordinator = new(
            new ContinuityRunner(),
            auditWriter,
            alertSink,
            new WormAuditTestData.FixedClock(Now),
            new OrderTrackingSink(callOrder, new ThrowingSink()),
            markers);

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
        RecoveryValidationEvidenceRetentionFailureMarker marker = markers.Markers.ShouldHaveSingleItem();
        marker.RunId.ShouldBe(Correlation);
        marker.JobId.ShouldBe(LiveRecoveryValidationJobs.Continuity);
        marker.Scenario.ShouldBe(ContinuityDrillScenarios.EventStoreOutage);
        markers.ReceivedCancelableToken.ShouldBeFalse();
        AssertRetentionRanBeforeAuditThenAlert(callOrder);
    }

    [Fact]
    public async Task EvidenceSinkFailureTurnsOtherwiseEquivalentRebuildIntoUnmeasurableBreach()
    {
        List<string> callOrder = [];
        OrderTrackingAuditWriter auditWriter = new(callOrder);
        OrderTrackingAlertSink alertSink = new(callOrder);
        CapturingMarkerSink markers = new(callOrder);
        ProjectionRebuildValidationCoordinator coordinator = new(
            new RebuildDriver(),
            auditWriter,
            alertSink,
            new WormAuditTestData.FixedClock(Now),
            new OrderTrackingSink(callOrder, new ThrowingSink()),
            markers);

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
        RecoveryValidationEvidenceRetentionFailureMarker marker = markers.Markers.ShouldHaveSingleItem();
        marker.JobId.ShouldBe(LiveRecoveryValidationJobs.ProjectionRebuild);
        marker.Scenario.ShouldBe(RecoveryValidationEvidenceRetentionFailureMarker.ProjectionRebuildScenario);
        AssertRetentionRanBeforeAuditThenAlert(callOrder);
    }

    [Fact]
    public async Task EvidenceSinkFailureTurnsOtherwiseContainedOutageIntoUnmeasurableBreach()
    {
        List<string> callOrder = [];
        OrderTrackingAuditWriter auditWriter = new(callOrder);
        OrderTrackingAlertSink alertSink = new(callOrder);
        CapturingMarkerSink markers = new(callOrder);
        ScopedOutageDegradationValidationCoordinator coordinator = new(
            new OutageDriver(),
            auditWriter,
            alertSink,
            new WormAuditTestData.FixedClock(Now),
            new OrderTrackingSink(callOrder, new ThrowingSink()),
            markers);

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
        RecoveryValidationEvidenceRetentionFailureMarker marker = markers.Markers.ShouldHaveSingleItem();
        marker.JobId.ShouldBe(LiveRecoveryValidationJobs.ScopedOutage);
        marker.Scenario.ShouldBe(ScopedOutageDependencies.CommandExecution);
        AssertRetentionRanBeforeAuditThenAlert(callOrder);
    }

    [Fact]
    public async Task SuccessfulFallbackEvidenceWriteDoesNotEmitMarker()
    {
        FallbackSucceedsSink evidence = new();
        CapturingMarkerSink markers = new();
        ContinuityDrillCoordinator coordinator = new(
            new ContinuityRunner(),
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            new WormAuditTestData.FixedClock(Now),
            evidence,
            markers);

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            TestTenant,
            Correlation,
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        evidence.Attempts.ShouldBe(2);
        evidence.Retained.ShouldHaveSingleItem().Deviations
            .ShouldContain(ContinuityDrillReport.EvidenceRetentionFailedDeviation);
        markers.Markers.ShouldBeEmpty();
    }

    [Fact]
    public async Task MarkerSinkFailureDoesNotMaskUnmeasurableReportOrAuditAlert()
    {
        InMemoryAuditWriter audit = new();
        InMemoryOperatorAlertSink alerts = new();
        ContinuityDrillCoordinator coordinator = new(
            new ContinuityRunner(),
            audit,
            alerts,
            new WormAuditTestData.FixedClock(Now),
            new ThrowingSink(),
            new ThrowingMarkerSink());

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            TestTenant,
            Correlation,
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        report.Deviations.ShouldContain(ContinuityDrillReport.EvidenceRetentionFailedDeviation);
        audit.Envelopes.ShouldHaveSingleItem();
        alerts.Alerts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task EvidenceRetentionFailureClampsEndedAtUtcToStartedAtUtcWhenTheClockAppearsToRetreat()
    {
        // The completed measurement's StartedAtUtc is ahead of the coordinator's own clock — a clock-skew shape the
        // fixed-clock fixtures above cannot exercise (there, clock.UtcNow always equals the measurement's
        // StartedAtUtc, so `endedAtUtc < report.StartedAtUtc` is never true, merely equal).
        DateTimeOffset futureStartedAtUtc = Now + TimeSpan.FromMinutes(5);
        CapturingMarkerSink markers = new();
        ContinuityDrillCoordinator coordinator = new(
            new FutureStartedContinuityRunner(futureStartedAtUtc),
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            new WormAuditTestData.FixedClock(Now),
            new ThrowingSink(),
            markers);

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            TestTenant,
            Correlation,
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        report.StartedAtUtc.ShouldBe(futureStartedAtUtc);
        report.EndedAtUtc.ShouldBe(futureStartedAtUtc);
        (report.EndedAtUtc >= report.StartedAtUtc).ShouldBeTrue("EndedAtUtc must never precede StartedAtUtc, even under clock skew.");
        markers.Markers.ShouldHaveSingleItem().FailedAtUtc.ShouldBe(futureStartedAtUtc);
    }

    [Fact]
    public async Task MarkerClockFailureDoesNotMaskUnmeasurableReportOrAuditAlert()
    {
        InMemoryAuditWriter audit = new();
        InMemoryOperatorAlertSink alerts = new();
        CapturingMarkerSink markers = new();
        ContinuityDrillCoordinator coordinator = new(
            new ContinuityRunner(),
            audit,
            alerts,
            new ClockThrowingOnlyDuringMarkerCreation(),
            new ThrowingSink(),
            markers);

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            TestTenant,
            Correlation,
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        markers.Markers.ShouldBeEmpty();
        audit.Envelopes.ShouldHaveSingleItem();
        alerts.Alerts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task NeverCompletingMarkerSinkIsBoundedBeforeAuditAndAlert()
    {
        List<string> callOrder = [];
        OrderTrackingAuditWriter audit = new(callOrder);
        OrderTrackingAlertSink alerts = new(callOrder);
        NeverCompletingMarkerSink markers = new(callOrder);
        ContinuityDrillCoordinator coordinator = new(
            new ContinuityRunner(),
            audit,
            alerts,
            new WormAuditTestData.FixedClock(Now),
            new OrderTrackingSink(callOrder, new ThrowingSink()),
            markers);

        Task<ContinuityDrillReport> run = coordinator.RunScenarioAndRecordAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            TestTenant,
            Correlation,
            TestContext.Current.CancellationToken).AsTask();
        Task winner = await Task.WhenAny(
            run,
            Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        winner.ShouldBe(run, "the best-effort marker sink must not delay audit and alert indefinitely.");
        (await run.ConfigureAwait(true)).Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        markers.Attempts.ShouldBe(1);
        audit.Envelopes.ShouldHaveSingleItem();
        alerts.Alerts.ShouldHaveSingleItem();
        AssertRetentionRanBeforeAuditThenAlert(callOrder);
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
        int marker = callOrder.IndexOf("retention-marker");
        int audit = callOrder.IndexOf("audit");
        int alert = callOrder.IndexOf("alert");
        (marker > lastRetain).ShouldBeTrue("the marker must run only after both evidence writes fail.");
        (audit > marker).ShouldBeTrue("audit must run after the best-effort retention marker attempt.");
        (audit > lastRetain).ShouldBeTrue("audit must run after every retention attempt, not before.");
        (alert > audit).ShouldBeTrue("alert must run after audit, not before or interleaved ahead of it.");
    }

    private sealed class CapturingSink : IRecoveryValidationEvidenceSink
    {
        public List<string> Events { get; } = [];
        public List<ControlledLossPathReport> ControlledLossReports { get; } = [];
        public List<ContinuityDrillReport> ContinuityReports { get; } = [];
        public List<ProjectionRebuildReport> RebuildReports { get; } = [];
        public List<ScopedOutageDegradationReport> ScopedOutageReports { get; } = [];

        public ValueTask RecordAsync(ControlledLossPathReport report, CancellationToken cancellationToken)
        {
            ControlledLossReports.Add(report);
            Events.Add($"controlled-loss:{report.Scenario}");
            return ValueTask.CompletedTask;
        }

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
        public ValueTask RecordAsync(ControlledLossPathReport report, CancellationToken cancellationToken)
            => throw new IOException("evidence unavailable");

        public ValueTask RecordAsync(ContinuityDrillReport report, CancellationToken cancellationToken)
            => throw new IOException("evidence unavailable");

        public ValueTask RecordAsync(ProjectionRebuildReport report, CancellationToken cancellationToken)
            => throw new IOException("evidence unavailable");

        public ValueTask RecordAsync(ScopedOutageDegradationReport report, CancellationToken cancellationToken)
            => throw new IOException("evidence unavailable");
    }

    private sealed class FallbackSucceedsSink : IRecoveryValidationEvidenceSink
    {
        public int Attempts { get; private set; }

        public List<ContinuityDrillReport> Retained { get; } = [];

        public ValueTask RecordAsync(ControlledLossPathReport report, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask RecordAsync(ContinuityDrillReport report, CancellationToken cancellationToken)
        {
            Attempts++;
            if (Attempts == 1)
            {
                throw new IOException("canonical evidence unavailable");
            }

            Retained.Add(report);
            return ValueTask.CompletedTask;
        }

        public ValueTask RecordAsync(ProjectionRebuildReport report, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public ValueTask RecordAsync(ScopedOutageDegradationReport report, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    private sealed class CapturingMarkerSink(List<string>? callOrder = null) :
        IRecoveryValidationEvidenceRetentionFailureSink
    {
        public List<RecoveryValidationEvidenceRetentionFailureMarker> Markers { get; } = [];

        public bool ReceivedCancelableToken { get; private set; }

        public ValueTask RecordAsync(
            RecoveryValidationEvidenceRetentionFailureMarker marker,
            CancellationToken cancellationToken)
        {
            callOrder?.Add("retention-marker");
            ReceivedCancelableToken = cancellationToken.CanBeCanceled;
            Markers.Add(marker);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingMarkerSink : IRecoveryValidationEvidenceRetentionFailureSink
    {
        public ValueTask RecordAsync(
            RecoveryValidationEvidenceRetentionFailureMarker marker,
            CancellationToken cancellationToken)
            => throw new IOException("retention marker unavailable");
    }

    private sealed class NeverCompletingMarkerSink(List<string> callOrder) :
        IRecoveryValidationEvidenceRetentionFailureSink
    {
        private readonly TaskCompletionSource _never = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Attempts { get; private set; }

        public ValueTask RecordAsync(
            RecoveryValidationEvidenceRetentionFailureMarker marker,
            CancellationToken cancellationToken)
        {
            callOrder.Add("retention-marker");
            Attempts++;
            cancellationToken.CanBeCanceled.ShouldBeFalse();
            return new ValueTask(_never.Task);
        }
    }

    private sealed class ClockThrowingOnlyDuringMarkerCreation : ISystemClock
    {
        private int _reads;

        public DateTimeOffset UtcNow
            => Interlocked.Increment(ref _reads) == 3
                ? throw new InvalidOperationException("marker timestamp unavailable")
                : Now;
    }

    /// <summary>
    /// A representative "everything observed cleanly" fixture, so the sink-failure tests can assert this measurement's
    /// <see cref="RecoveryValidationExecutionAssertions"/> survives into the retention-failure fallback report rather
    /// than being silently dropped to <see langword="null"/>. This generic retention fixture does not observe mailbox
    /// re-ingestion, so that one assertion remains false rather than being fabricated as clean; the independent live
    /// projection driver supplies its own positive immutable-input boundary assertion.
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
        public ValueTask RecordAsync(ControlledLossPathReport report, CancellationToken cancellationToken)
        {
            callOrder.Add("retain");
            return inner.RecordAsync(report, cancellationToken);
        }

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
