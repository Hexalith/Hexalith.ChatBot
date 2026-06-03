using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.13 (AC1–AC4) coverage for the scoped-outage degradation validation coordinator, mirroring
/// <see cref="ProjectionRebuildValidationCoordinatorTests"/>: a scripted fake <see cref="IScopedOutageInjectionDriver"/>
/// drives the coordinator through contained / breached / contained-but-late-recording / unmeasurable, and the test
/// asserts the fail-closed audit-then-deliver discipline (envelope written BEFORE exactly one alert; audit-down ⇒ no
/// alert but the report still returns), the test-tenant-by-construction guard (a production tenant or unknown dependency
/// ⇒ <c>unmeasurable</c>, driver never invoked, never a fabricated <c>contained</c>), the sweep tally with distinct
/// dimensions, and the no-production-mutation construction (the driver is only ever invoked against the test tenant).
/// </summary>
public sealed class ScopedOutageDegradationValidationCoordinatorTests
{
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TestTenant = "replay-test:scoped-outage";
    private const string ProductionTenant = "tenant-production";
    private const string Dependency = ScopedOutageDependencies.Graph;
    private static readonly DateTimeOffset Now = WormAuditTestData.FixedNow;

    [Fact]
    public async Task ContainedWithinTargetProducesContainedReportWithNoAlertOrAuditEnvelope()
    {
        ScriptedDriver driver = new(Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox, TimeSpan.FromMinutes(1)));
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ScopedOutageDegradationValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ScopedOutageDegradationReport report = await coordinator.RunScenarioAndRecordAsync(Dependency, TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ScopedOutageDegradationVerdicts.Contained);
        report.ScopeRecordedWithinTarget.ShouldBeTrue();
        report.IsBreach.ShouldBeFalse();
        report.Deviations.ShouldBeEmpty();
        report.FirstBreachLocator.ShouldBeNull();
        report.ReasonCode.ShouldBe(ScopedOutageDegradationReport.ValidationCompletedReasonCode);
        auditWriter.Envelopes.ShouldBeEmpty();
        alertSink.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task BreachedValidationAuditsThenEmitsExactlyOneAlertWithFirstBreachLocator()
    {
        ScriptedDriver driver = new(Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox, TimeSpan.FromMinutes(1)) with { CrossTenantLeakageDetected = true });
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ScopedOutageDegradationValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ScopedOutageDegradationReport report = await coordinator.RunScenarioAndRecordAsync(Dependency, TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ScopedOutageDegradationVerdicts.Breached);
        report.IsScopeBreach.ShouldBeTrue();
        report.ScopeRecordedWithinTarget.ShouldBeTrue();
        report.Deviations.ShouldBe([ScopedOutageDegradationEvaluator.CrossTenantLeakageDeviation]);
        report.FirstBreachLocator.ShouldBe($"scope:{ScopedOutageScopes.Mailbox}|deviation:{ScopedOutageDegradationEvaluator.CrossTenantLeakageDeviation}");

        // Audited pre-commit BEFORE the alert (audit-then-deliver), metadata-only.
        AuditEnvelope envelope = auditWriter.Envelopes.ShouldHaveSingleItem();
        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.CommandName.ShouldBe("ScopedOutageDegradationBreach");
        envelope.TenantId.ShouldBe(TestTenant);

        OperatorAlert alert = alertSink.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.ScopedOutageDegradationBreach);
        alert.TenantId.ShouldBe(TestTenant);
        alert.CorrelationId.ShouldBe(Correlation);
        alert.ReasonCode.ShouldBe(ScopedOutageDegradationReport.ValidationCompletedReasonCode);
        alert.FirstBreakLocator.ShouldBe($"scope:{ScopedOutageScopes.Mailbox}|deviation:{ScopedOutageDegradationEvaluator.CrossTenantLeakageDeviation}");
    }

    [Fact]
    public async Task ContainedButLateRecordingStaysContainedButStillAuditsThenAlerts()
    {
        // A contained-but-slow degradation: the verdict stays contained, but the scope recording exceeded the 5-min budget.
        ScriptedDriver driver = new(Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox, RecoveryTargets.MaxScopeRecordingLatency + TimeSpan.FromMinutes(2)));
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ScopedOutageDegradationValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ScopedOutageDegradationReport report = await coordinator.RunScenarioAndRecordAsync(Dependency, TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ScopedOutageDegradationVerdicts.Contained); // the verdict stays contained — NOT an isolation failure
        report.IsScopeBreach.ShouldBeFalse();
        report.ScopeRecordedWithinTarget.ShouldBeFalse();
        report.IsBreach.ShouldBeTrue(); // a recording miss is still a breach to surface
        report.Deviations.ShouldBe([ScopedOutageDegradationEvaluator.ScopeRecordingExceededDeviation]);
        report.FirstBreachLocator.ShouldBeNull(); // no serious assertion failed

        auditWriter.Envelopes.ShouldHaveSingleItem();
        OperatorAlert alert = alertSink.Alerts.ShouldHaveSingleItem();
        alert.FirstBreakLocator.ShouldBe(ScopedOutageDegradationEvaluator.ScopeRecordingExceededDeviation); // falls back to the first deviation
    }

    [Fact]
    public async Task ExactlyAtTheFiveMinuteBoundaryIsWithinTarget()
    {
        ScriptedDriver driver = new(Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox, RecoveryTargets.MaxScopeRecordingLatency));
        ScopedOutageDegradationValidationCoordinator coordinator = new(driver, new InMemoryAuditWriter(), new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now));

        ScopedOutageDegradationReport report = await coordinator.RunScenarioAndRecordAsync(Dependency, TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.ScopeRecordedWithinTarget.ShouldBeTrue(); // == target is within target
        report.IsBreach.ShouldBeFalse();
    }

    [Fact]
    public async Task AuditDownAtPreCommitSuppressesTheAlertButStillReturnsTheReport()
    {
        ScriptedDriver driver = new(Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox, TimeSpan.FromMinutes(1)) with { SilentDataLossDetected = true });
        InMemoryOperatorAlertSink alertSink = new();
        ScopedOutageDegradationValidationCoordinator coordinator = new(driver, new WormAuditTestData.UnavailableAuditWriter(), alertSink, new WormAuditTestData.FixedClock(Now));

        ScopedOutageDegradationReport report = await coordinator.RunScenarioAndRecordAsync(Dependency, TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.IsBreach.ShouldBeTrue();
        alertSink.Alerts.ShouldBeEmpty(); // no observable side effect when the audit fails closed
    }

    [Fact]
    public async Task ThrowingDriverFailsClosedToUnmeasurableAndAuditsThenAlerts()
    {
        ThrowingDriver driver = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ScopedOutageDegradationValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ScopedOutageDegradationReport report = await coordinator.RunScenarioAndRecordAsync(Dependency, TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ScopedOutageDegradationVerdicts.Unmeasurable);
        report.IsBreach.ShouldBeTrue();
        report.IsScopeBreach.ShouldBeFalse(); // unmeasurable is the fail-safe breach, NOT an isolation failure
        report.ReasonCode.ShouldBe(ScopedOutageDegradationReport.ValidationUnmeasurableReasonCode);
        report.Deviations.ShouldBe([ScopedOutageDegradationReport.IncompleteDeviation]);
        auditWriter.Envelopes.ShouldHaveSingleItem();
        OperatorAlert alert = alertSink.Alerts.ShouldHaveSingleItem();
        // The fail-safe breach rides the unmeasurable reason code (never a fabricated completed/contained code).
        alert.ReasonCode.ShouldBe(ScopedOutageDegradationReport.ValidationUnmeasurableReasonCode);
        alert.FirstBreakLocator.ShouldBe(ScopedOutageDegradationReport.IncompleteDeviation);
    }

    [Fact]
    public async Task CancellationPropagatesRatherThanFabricatingAnUnmeasurablePassOrAlert()
    {
        // The fail-safe catch is guarded by `when (!IsCancellationRequested)`: a cancelled run must surface the
        // cancellation, NOT swallow it into an unmeasurable report and fire a spurious audit-then-alert.
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        CancellingDriver driver = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ScopedOutageDegradationValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        await Should.ThrowAsync<OperationCanceledException>(
            () => coordinator.RunScenarioAndRecordAsync(Dependency, TestTenant, Correlation, cts.Token).AsTask());

        auditWriter.Envelopes.ShouldBeEmpty(); // no fabricated breach evidence on cancellation
        alertSink.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProductionTenantTargetIsUnmeasurableAndNeverInvokesTheDriver()
    {
        ScriptedDriver driver = new(Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox, TimeSpan.FromMinutes(1)));
        InMemoryAuditWriter auditWriter = new();
        ScopedOutageDegradationValidationCoordinator coordinator = new(driver, auditWriter, new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now));

        ScopedOutageDegradationReport report = await coordinator.RunScenarioAndRecordAsync(Dependency, ProductionTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ScopedOutageDegradationVerdicts.Unmeasurable); // never a fabricated contained
        driver.Invocations.ShouldBeEmpty(); // the outage never ran against a production tenant
        auditWriter.Envelopes.ShouldHaveSingleItem(); // the fail-safe breach is still audited
    }

    [Fact]
    public async Task UnknownDependencyIsUnmeasurableAndNeverInvokesTheDriver()
    {
        ScriptedDriver driver = new(Clean(ScopedOutageScopes.Mailbox, ScopedOutageScopes.Mailbox, TimeSpan.FromMinutes(1)));
        InMemoryAuditWriter auditWriter = new();
        ScopedOutageDegradationValidationCoordinator coordinator = new(driver, auditWriter, new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now));

        ScopedOutageDegradationReport report = await coordinator.RunScenarioAndRecordAsync("subscription-expiry", TestTenant, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ScopedOutageDegradationVerdicts.Unmeasurable); // an unknown dependency biases to unmeasurable
        driver.Invocations.ShouldBeEmpty();
        auditWriter.Envelopes.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task RunAllScenariosTalliesContainedBreachedScopeRecordingExceededAndUnmeasurableDistinctly()
    {
        SwitchingDriver driver = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ScopedOutageDegradationValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ScopedOutageDegradationOutcome outcome = await coordinator.RunAllScenariosAsync(TestTenant, Correlation, TestContext.Current.CancellationToken);

        // Six NFR59 dependencies: graph=breached, identity=slow-contained, ai-provider=throws, the other three=contained.
        outcome.ScenariosValidated.ShouldBe(6);
        outcome.Contained.ShouldBe(4); // the three clean + the contained-but-slow one all stay contained
        outcome.Breached.ShouldBe(1);
        outcome.ScopeRecordingExceeded.ShouldBe(1); // the slow dependency is counted in this distinct dimension
        outcome.Unmeasurable.ShouldBe(1);
        outcome.Alerted.ShouldBe(3); // breached + slow + unmeasurable each fail-closed-audit-then-alert; the clean ones do not

        // No-production-mutation by construction: the driver was only ever invoked against the test tenant.
        driver.TenantsSeen.ShouldAllBe(tenant => ReplayTenantPolicy.IsTestTenant(tenant));
    }

    private static ScopedOutageDegradationMeasurement Clean(string expectedScope, string observedScope, TimeSpan recordingLatency)
        => new(
            expectedScope,
            observedScope,
            CrossTenantLeakageDetected: false,
            UnauthorizedMutationDetected: false,
            SilentDataLossDetected: false,
            InflightItemsRecoverable: true,
            DuplicateSideEffectDetected: false,
            recordingLatency,
            Now,
            Now + TimeSpan.FromMinutes(2));

    /// <summary>A scripted driver returning a fixed measurement, recording the (dependency, tenant) it was invoked with.</summary>
    private sealed class ScriptedDriver(ScopedOutageDegradationMeasurement measurement) : IScopedOutageInjectionDriver
    {
        public List<(string Dependency, string Tenant)> Invocations { get; } = [];

        public ValueTask<ScopedOutageDegradationMeasurement> InjectAndMeasureAsync(string dependency, string testTenantRef, string correlationId, CancellationToken cancellationToken)
        {
            Invocations.Add((dependency, testTenantRef));
            return ValueTask.FromResult(measurement);
        }
    }

    /// <summary>A driver that throws, exercising the fail-closed (unmeasurable) validation path.</summary>
    private sealed class ThrowingDriver : IScopedOutageInjectionDriver
    {
        public ValueTask<ScopedOutageDegradationMeasurement> InjectAndMeasureAsync(string dependency, string testTenantRef, string correlationId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("injection driver down");
    }

    /// <summary>A driver that throws on a cancelled token, exercising the `when (!IsCancellationRequested)` rethrow path.</summary>
    private sealed class CancellingDriver : IScopedOutageInjectionDriver
    {
        public ValueTask<ScopedOutageDegradationMeasurement> InjectAndMeasureAsync(string dependency, string testTenantRef, string correlationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    /// <summary>Returns a distinct outcome per dependency (breached / slow-but-contained / throws / contained), recording every tenant seen.</summary>
    private sealed class SwitchingDriver : IScopedOutageInjectionDriver
    {
        public List<string> TenantsSeen { get; } = [];

        public ValueTask<ScopedOutageDegradationMeasurement> InjectAndMeasureAsync(string dependency, string testTenantRef, string correlationId, CancellationToken cancellationToken)
        {
            TenantsSeen.Add(testTenantRef);

            if (string.Equals(dependency, ScopedOutageDependencies.AiProvider, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("injection driver down");
            }

            bool leakage = string.Equals(dependency, ScopedOutageDependencies.Graph, StringComparison.Ordinal);
            TimeSpan recording = string.Equals(dependency, ScopedOutageDependencies.Identity, StringComparison.Ordinal)
                ? RecoveryTargets.MaxScopeRecordingLatency + TimeSpan.FromMinutes(2)
                : TimeSpan.FromMinutes(1);

            return ValueTask.FromResult(new ScopedOutageDegradationMeasurement(
                ScopedOutageScopes.Mailbox,
                ScopedOutageScopes.Mailbox,
                CrossTenantLeakageDetected: leakage,
                UnauthorizedMutationDetected: false,
                SilentDataLossDetected: false,
                InflightItemsRecoverable: true,
                DuplicateSideEffectDetected: false,
                recording,
                Now,
                Now + TimeSpan.FromMinutes(2)));
        }
    }
}
