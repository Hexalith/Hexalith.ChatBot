using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.11 (AC1–AC4) coverage for the continuity-drill coordinator, mirroring
/// <see cref="DerivedStoreIsolationProbeCoordinatorTests"/>: a scripted fake <see cref="IContinuityDrillScenarioRunner"/>
/// drives the coordinator through met / missed / unmeasurable, and the test asserts the fail-closed audit-then-deliver
/// discipline (envelope written BEFORE exactly one alert; audit-down ⇒ no alert but the report still returns), the
/// test-tenant-by-construction guard (a production tenant or unknown scenario ⇒ <c>unmeasurable</c>, never a fabricated
/// <c>met</c>), the sweep tally, and the no-production-mutation construction (the runner is only ever invoked against
/// the test tenant).
/// </summary>
public sealed class ContinuityDrillCoordinatorTests
{
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TestTenant = "replay-test:continuity-drill";
    private const string ProductionTenant = "tenant-production";
    private const string Scenario = ContinuityDrillScenarios.EventStoreOutage;
    private static readonly DateTimeOffset Now = WormAuditTestData.FixedNow;

    [Fact]
    public async Task MetDrillProducesMetReportWithNoAlertOrAuditEnvelope()
    {
        ScriptedRunner runner = new(Measurement(TimeSpan.FromMinutes(5), TimeSpan.FromHours(1), dataLossDetected: false));
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ContinuityDrillCoordinator coordinator = new(runner, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(Scenario, TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Met);
        report.IsBreach.ShouldBeFalse();
        report.RecalibrationFlag.ShouldBeFalse();
        report.FollowUpActionRef.ShouldBeNull();
        report.Deviations.ShouldBeEmpty();
        report.ReasonCode.ShouldBe(ContinuityDrillReport.DrillCompletedReasonCode);
        auditWriter.Envelopes.ShouldBeEmpty();
        alertSink.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task MissedDrillAuditsThenEmitsExactlyOneAlertAndFlagsRecalibration()
    {
        TimeSpan rpoOver = RecoveryTargets.MaxRpo + TimeSpan.FromMinutes(5);
        ScriptedRunner runner = new(Measurement(rpoOver, TimeSpan.FromHours(1), dataLossDetected: false));
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ContinuityDrillCoordinator coordinator = new(runner, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(Scenario, TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Missed);
        report.IsMiss.ShouldBeTrue();
        report.RecalibrationFlag.ShouldBeTrue();
        report.FollowUpActionRef.ShouldBe($"continuity-recalibration:{Scenario}");
        report.Deviations.ShouldBe([ContinuityDrillEvaluator.RpoExceededDeviation]);

        // Audited pre-commit BEFORE the alert (audit-then-deliver), metadata-only.
        AuditEnvelope envelope = auditWriter.Envelopes.ShouldHaveSingleItem();
        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.CommandName.ShouldBe("ContinuityDrillTargetMissed");
        envelope.TenantId.ShouldBe(TestTenant);

        OperatorAlert alert = alertSink.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.ContinuityDrillTargetMissed);
        alert.TenantId.ShouldBe(TestTenant);
        alert.CorrelationId.ShouldBe(Correlation);
        alert.ReasonCode.ShouldBe(ContinuityDrillReport.DrillCompletedReasonCode);
        alert.FirstBreakLocator.ShouldBe(ContinuityDrillEvaluator.RpoExceededDeviation);
    }

    [Fact]
    public async Task DataLossDrillIsAMissThatFlagsRecalibration()
    {
        ScriptedRunner runner = new(Measurement(TimeSpan.FromMinutes(1), TimeSpan.FromHours(1), dataLossDetected: true));
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ContinuityDrillCoordinator coordinator = new(runner, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(Scenario, TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Missed);
        report.DataLossDetected.ShouldBeTrue();
        report.Deviations.ShouldBe([ContinuityDrillEvaluator.DataLossDeviation]);
        alertSink.Alerts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task AuditDownAtPreCommitSuppressesTheAlertButStillReturnsTheReport()
    {
        ScriptedRunner runner = new(Measurement(RecoveryTargets.MaxRpo + TimeSpan.FromMinutes(1), TimeSpan.FromHours(1), false));
        InMemoryOperatorAlertSink alertSink = new();
        ContinuityDrillCoordinator coordinator = new(runner, new WormAuditTestData.UnavailableAuditWriter(), alertSink, new WormAuditTestData.FixedClock(Now));

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(Scenario, TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.IsBreach.ShouldBeTrue();
        alertSink.Alerts.ShouldBeEmpty(); // no observable side effect when the audit fails closed
    }

    [Fact]
    public async Task ThrowingRunnerFailsClosedToUnmeasurableAndAuditsThenAlerts()
    {
        ThrowingRunner runner = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ContinuityDrillCoordinator coordinator = new(runner, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(Scenario, TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        report.IsBreach.ShouldBeTrue();
        report.IsMiss.ShouldBeFalse(); // unmeasurable is the fail-safe breach, NOT an honest miss
        report.ReasonCode.ShouldBe(ContinuityDrillReport.DrillUnmeasurableReasonCode);
        report.RecalibrationFlag.ShouldBeTrue();
        report.Deviations.ShouldBe([ContinuityDrillReport.IncompleteDeviation]);
        auditWriter.Envelopes.ShouldHaveSingleItem();
        alertSink.Alerts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ProductionTenantTargetIsUnmeasurableAndNeverInvokesTheRunner()
    {
        ScriptedRunner runner = new(Measurement(TimeSpan.FromMinutes(1), TimeSpan.FromHours(1), false));
        InMemoryAuditWriter auditWriter = new();
        ContinuityDrillCoordinator coordinator = new(runner, auditWriter, new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now));

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(Scenario, ProductionTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        runner.Invocations.ShouldBeEmpty(); // the drill never ran recovery against a production tenant
        auditWriter.Envelopes.ShouldHaveSingleItem(); // the fail-safe breach is still audited
    }

    [Fact]
    public async Task UnknownScenarioIsUnmeasurableNeverAFabricatedMet()
    {
        ScriptedRunner runner = new(Measurement(TimeSpan.FromMinutes(1), TimeSpan.FromHours(1), false));
        ContinuityDrillCoordinator coordinator = new(runner, new InMemoryAuditWriter(), new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now));

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync("totally-unknown-scenario", TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        runner.Invocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task SweepRunsBothScenariosAndTalliesTheOutcome()
    {
        // A runner that misses on the M365 scenario and meets on the EventStore scenario, to exercise distinct tallies.
        SwitchingRunner runner = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ContinuityDrillCoordinator coordinator = new(runner, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ContinuityDrillOutcome outcome = await coordinator.RunAllScenariosAsync(TestTenant, Correlation, TestContext.Current.CancellationToken);

        outcome.ScenariosRun.ShouldBe(2);
        outcome.Met.ShouldBe(1);
        outcome.Missed.ShouldBe(1);
        outcome.Unmeasurable.ShouldBe(0); // the drills produced evidence (the release-gate dimension)
        outcome.Alerted.ShouldBe(1); // only the missed scenario fails-closed-audits-then-alerts

        // Destructive sweep order is a contract: EventStore stop before M365 subscription fault.
        runner.ScenariosSeen.ShouldBe(ContinuityDrillScenarios.SweepOrder);

        // No-production-mutation by construction: the runner was only ever invoked against the test tenant.
        runner.TenantsSeen.ShouldAllBe(tenant => ReplayTenantPolicy.IsTestTenant(tenant));
    }

    private static ContinuityDrillMeasurement Measurement(TimeSpan rpo, TimeSpan rto, bool dataLossDetected)
        => new(Now, Now + rto, rpo, rto, dataLossDetected);

    /// <summary>A scripted runner returning a fixed measurement, recording the (scenario, tenant) it was invoked with.</summary>
    private sealed class ScriptedRunner(ContinuityDrillMeasurement measurement) : IContinuityDrillScenarioRunner
    {
        public List<(string Scenario, string Tenant)> Invocations { get; } = [];

        public ValueTask<ContinuityDrillMeasurement> RunAsync(string scenario, string testTenantRef, string correlationId, CancellationToken cancellationToken)
        {
            Invocations.Add((scenario, testTenantRef));
            return ValueTask.FromResult(measurement);
        }
    }

    /// <summary>A runner that throws, exercising the fail-closed (unmeasurable) drill path.</summary>
    private sealed class ThrowingRunner : IContinuityDrillScenarioRunner
    {
        public ValueTask<ContinuityDrillMeasurement> RunAsync(string scenario, string testTenantRef, string correlationId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("scenario runner down");
    }

    /// <summary>Meets RPO/RTO on the EventStore scenario, misses on the M365 scenario, recording every tenant seen.</summary>
    private sealed class SwitchingRunner : IContinuityDrillScenarioRunner
    {
        public List<string> TenantsSeen { get; } = [];
        public List<string> ScenariosSeen { get; } = [];

        public ValueTask<ContinuityDrillMeasurement> RunAsync(string scenario, string testTenantRef, string correlationId, CancellationToken cancellationToken)
        {
            TenantsSeen.Add(testTenantRef);
            ScenariosSeen.Add(scenario);
            bool miss = string.Equals(scenario, ContinuityDrillScenarios.M365SubscriptionFailure, StringComparison.Ordinal);
            TimeSpan rto = miss ? RecoveryTargets.MaxRto + TimeSpan.FromHours(1) : TimeSpan.FromHours(1);
            return ValueTask.FromResult(new ContinuityDrillMeasurement(Now, Now + rto, TimeSpan.FromMinutes(1), rto, false));
        }
    }
}
