using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.12 (AC1–AC4) coverage for the projection-rebuild validation coordinator, mirroring
/// <see cref="ContinuityDrillCoordinatorTests"/>: a scripted fake <see cref="IProjectionRebuildDriver"/> drives the
/// coordinator through equivalent / divergent / duration-exceeded / unmeasurable, and the test asserts the fail-closed
/// audit-then-deliver discipline (envelope written BEFORE exactly one alert; audit-down ⇒ no alert but the report still
/// returns), the test-tenant-by-construction guard (a production tenant ⇒ <c>unmeasurable</c>, driver never invoked,
/// never a fabricated <c>equivalent</c>), the sweep tally with distinct dimensions, and the no-production-mutation
/// construction (the driver is only ever invoked against the test tenant).
/// </summary>
public sealed class ProjectionRebuildValidationCoordinatorTests
{
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TestTenant = "replay-test:projection-rebuild";
    private const string ProductionTenant = "tenant-production";
    private const string Dataset = "baseline-dataset-1";
    private const string SchemaV1 = "chatbot.governed-operation-view.v1";
    private const string SchemaV2 = "chatbot.governed-operation-view.v2";
    private static readonly DateTimeOffset Now = WormAuditTestData.FixedNow;

    private static readonly IReadOnlyList<ProjectionResourceDigest> Snapshot =
    [
        ProjectionResourceDigest.Create("resource-a", "token-a"),
        ProjectionResourceDigest.Create("resource-b", "token-b"),
    ];

    [Fact]
    public async Task EquivalentWithinTargetProducesEquivalentReportWithNoAlertOrAuditEnvelope()
    {
        ScriptedDriver driver = new(Measurement(Snapshot, Snapshot, SchemaV1, SchemaV1, TimeSpan.FromHours(1)));
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ProjectionRebuildValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ProjectionRebuildReport report = await coordinator.RunValidationAndRecordAsync(TestTenant, Dataset, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ProjectionRebuildVerdicts.Equivalent);
        report.DurationWithinTarget.ShouldBeTrue();
        report.IsBreach.ShouldBeFalse();
        report.ResourcesCompared.ShouldBe(2);
        report.Deviations.ShouldBeEmpty();
        report.FirstDivergingResourceLocator.ShouldBeNull();
        report.ReasonCode.ShouldBe(ProjectionRebuildReport.ValidationCompletedReasonCode);
        auditWriter.Envelopes.ShouldBeEmpty();
        alertSink.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task DivergentValidationAuditsThenEmitsExactlyOneAlertWithFirstDivergingLocator()
    {
        IReadOnlyList<ProjectionResourceDigest> rebuilt =
        [
            ProjectionResourceDigest.Create("resource-a", "token-a"),
            ProjectionResourceDigest.Create("resource-b", "token-CHANGED"),
        ];
        ScriptedDriver driver = new(Measurement(Snapshot, rebuilt, SchemaV1, SchemaV1, TimeSpan.FromHours(1)));
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ProjectionRebuildValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ProjectionRebuildReport report = await coordinator.RunValidationAndRecordAsync(TestTenant, Dataset, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ProjectionRebuildVerdicts.Divergent);
        report.IsDivergent.ShouldBeTrue();
        report.DurationWithinTarget.ShouldBeTrue();
        report.Deviations.ShouldBe([ProjectionRebuildEquivalenceEvaluator.DivergedDeviation]);
        report.FirstDivergingResourceLocator.ShouldBe("resource:resource-b");

        // Audited pre-commit BEFORE the alert (audit-then-deliver), metadata-only.
        AuditEnvelope envelope = auditWriter.Envelopes.ShouldHaveSingleItem();
        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.CommandName.ShouldBe("ProjectionRebuildValidationFailed");
        envelope.TenantId.ShouldBe(TestTenant);

        OperatorAlert alert = alertSink.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.ProjectionRebuildValidationFailed);
        alert.TenantId.ShouldBe(TestTenant);
        alert.CorrelationId.ShouldBe(Correlation);
        alert.ReasonCode.ShouldBe(ProjectionRebuildReport.ValidationCompletedReasonCode);
        alert.FirstBreakLocator.ShouldBe("resource:resource-b");
    }

    [Fact]
    public async Task SchemaVersionMismatchIsADivergentValidation()
    {
        // Identical snapshots, but the rebuild stamped a different schema version (event-upcasting divergence).
        ScriptedDriver driver = new(Measurement(Snapshot, Snapshot, SchemaV1, SchemaV2, TimeSpan.FromHours(1)));
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ProjectionRebuildValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ProjectionRebuildReport report = await coordinator.RunValidationAndRecordAsync(TestTenant, Dataset, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ProjectionRebuildVerdicts.Divergent);
        report.ProjectionSchemaVersion.ShouldBe(SchemaV2); // the rebuilt snapshot's stamped version
        alertSink.Alerts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task EquivalentButOverTargetIsAnEquivalentReportThatStillAuditsThenAlerts()
    {
        // A deterministic-but-slow rebuild: equivalent verdict, but the measured duration exceeds the 4-hr target.
        ScriptedDriver driver = new(Measurement(Snapshot, Snapshot, SchemaV1, SchemaV1, RecoveryTargets.MaxRto + TimeSpan.FromMinutes(30)));
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ProjectionRebuildValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ProjectionRebuildReport report = await coordinator.RunValidationAndRecordAsync(TestTenant, Dataset, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ProjectionRebuildVerdicts.Equivalent); // the verdict stays equivalent — NOT a determinism failure
        report.IsDivergent.ShouldBeFalse();
        report.DurationWithinTarget.ShouldBeFalse();
        report.IsBreach.ShouldBeTrue(); // a recovery-time miss is still a breach to surface
        report.Deviations.ShouldBe([ProjectionRebuildEquivalenceEvaluator.DurationExceededDeviation]);
        report.FirstDivergingResourceLocator.ShouldBeNull();

        auditWriter.Envelopes.ShouldHaveSingleItem();
        OperatorAlert alert = alertSink.Alerts.ShouldHaveSingleItem();
        alert.FirstBreakLocator.ShouldBe(ProjectionRebuildEquivalenceEvaluator.DurationExceededDeviation); // falls back to the first deviation
    }

    [Fact]
    public async Task ExactlyAtTheFourHourBoundaryIsWithinTarget()
    {
        ScriptedDriver driver = new(Measurement(Snapshot, Snapshot, SchemaV1, SchemaV1, RecoveryTargets.MaxRto));
        ProjectionRebuildValidationCoordinator coordinator = new(driver, new InMemoryAuditWriter(), new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now));

        ProjectionRebuildReport report = await coordinator.RunValidationAndRecordAsync(TestTenant, Dataset, Correlation, TestContext.Current.CancellationToken);

        report.DurationWithinTarget.ShouldBeTrue(); // == target is within target
        report.IsBreach.ShouldBeFalse();
    }

    [Fact]
    public async Task AuditDownAtPreCommitSuppressesTheAlertButStillReturnsTheReport()
    {
        IReadOnlyList<ProjectionResourceDigest> rebuilt = [ProjectionResourceDigest.Create("resource-a", "token-CHANGED"), ProjectionResourceDigest.Create("resource-b", "token-b")];
        ScriptedDriver driver = new(Measurement(Snapshot, rebuilt, SchemaV1, SchemaV1, TimeSpan.FromHours(1)));
        InMemoryOperatorAlertSink alertSink = new();
        ProjectionRebuildValidationCoordinator coordinator = new(driver, new WormAuditTestData.UnavailableAuditWriter(), alertSink, new WormAuditTestData.FixedClock(Now));

        ProjectionRebuildReport report = await coordinator.RunValidationAndRecordAsync(TestTenant, Dataset, Correlation, TestContext.Current.CancellationToken);

        report.IsBreach.ShouldBeTrue();
        alertSink.Alerts.ShouldBeEmpty(); // no observable side effect when the audit fails closed
    }

    [Fact]
    public async Task ThrowingDriverFailsClosedToUnmeasurableAndAuditsThenAlerts()
    {
        ThrowingDriver driver = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ProjectionRebuildValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ProjectionRebuildReport report = await coordinator.RunValidationAndRecordAsync(TestTenant, Dataset, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ProjectionRebuildVerdicts.Unmeasurable);
        report.IsBreach.ShouldBeTrue();
        report.IsDivergent.ShouldBeFalse(); // unmeasurable is the fail-safe breach, NOT a determinism failure
        report.ReasonCode.ShouldBe(ProjectionRebuildReport.ValidationUnmeasurableReasonCode);
        report.Deviations.ShouldBe([ProjectionRebuildReport.IncompleteDeviation]);
        report.ResourcesCompared.ShouldBe(0);
        auditWriter.Envelopes.ShouldHaveSingleItem();
        alertSink.Alerts.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ProductionTenantTargetIsUnmeasurableAndNeverInvokesTheDriver()
    {
        ScriptedDriver driver = new(Measurement(Snapshot, Snapshot, SchemaV1, SchemaV1, TimeSpan.FromHours(1)));
        InMemoryAuditWriter auditWriter = new();
        ProjectionRebuildValidationCoordinator coordinator = new(driver, auditWriter, new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now));

        ProjectionRebuildReport report = await coordinator.RunValidationAndRecordAsync(ProductionTenant, Dataset, Correlation, TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ProjectionRebuildVerdicts.Unmeasurable); // never a fabricated equivalent
        driver.Invocations.ShouldBeEmpty(); // the rebuild never ran against a production tenant
        auditWriter.Envelopes.ShouldHaveSingleItem(); // the fail-safe breach is still audited
    }

    [Fact]
    public async Task RunAllTalliesEquivalentDivergentDurationExceededAndUnmeasurableDistinctly()
    {
        SwitchingDriver driver = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ProjectionRebuildValidationCoordinator coordinator = new(driver, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ProjectionRebuildOutcome outcome = await coordinator.RunAllAsync(
            TestTenant,
            ["dataset-equivalent", "dataset-divergent", "dataset-slow", "dataset-throws"],
            Correlation,
            TestContext.Current.CancellationToken);

        outcome.TenantsValidated.ShouldBe(4);
        outcome.Equivalent.ShouldBe(2); // the equivalent + the deterministic-but-slow dataset both stay equivalent
        outcome.Divergent.ShouldBe(1);
        outcome.DurationExceeded.ShouldBe(1); // the slow dataset is counted in this distinct dimension
        outcome.Unmeasurable.ShouldBe(1);
        outcome.Alerted.ShouldBe(3); // divergent + slow + unmeasurable each fail-closed-audit-then-alert; the clean one does not

        // No-production-mutation by construction: the driver was only ever invoked against the test tenant.
        driver.TenantsSeen.ShouldAllBe(tenant => ReplayTenantPolicy.IsTestTenant(tenant));
    }

    private static ProjectionRebuildMeasurement Measurement(
        IReadOnlyList<ProjectionResourceDigest> preRebuild,
        IReadOnlyList<ProjectionResourceDigest> rebuilt,
        string preSchema,
        string rebuiltSchema,
        TimeSpan duration)
        => new(Now, Now + duration, duration, preRebuild, rebuilt, preSchema, rebuiltSchema);

    /// <summary>A scripted driver returning a fixed measurement, recording the (tenant, dataset) it was invoked with.</summary>
    private sealed class ScriptedDriver(ProjectionRebuildMeasurement measurement) : IProjectionRebuildDriver
    {
        public List<(string Tenant, string Dataset)> Invocations { get; } = [];

        public ValueTask<ProjectionRebuildMeasurement> RebuildAsync(string testTenantRef, string datasetRef, string correlationId, CancellationToken cancellationToken)
        {
            Invocations.Add((testTenantRef, datasetRef));
            return ValueTask.FromResult(measurement);
        }
    }

    /// <summary>A driver that throws, exercising the fail-closed (unmeasurable) validation path.</summary>
    private sealed class ThrowingDriver : IProjectionRebuildDriver
    {
        public ValueTask<ProjectionRebuildMeasurement> RebuildAsync(string testTenantRef, string datasetRef, string correlationId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("rebuild driver down");
    }

    /// <summary>Returns a distinct outcome per dataset id (equivalent / divergent / slow-but-equivalent / throws), recording every tenant seen.</summary>
    private sealed class SwitchingDriver : IProjectionRebuildDriver
    {
        public List<string> TenantsSeen { get; } = [];

        public ValueTask<ProjectionRebuildMeasurement> RebuildAsync(string testTenantRef, string datasetRef, string correlationId, CancellationToken cancellationToken)
        {
            TenantsSeen.Add(testTenantRef);

            if (string.Equals(datasetRef, "dataset-throws", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("rebuild driver down");
            }

            IReadOnlyList<ProjectionResourceDigest> rebuilt = string.Equals(datasetRef, "dataset-divergent", StringComparison.Ordinal)
                ? [ProjectionResourceDigest.Create("resource-a", "token-a"), ProjectionResourceDigest.Create("resource-b", "token-CHANGED")]
                : Snapshot;
            TimeSpan duration = string.Equals(datasetRef, "dataset-slow", StringComparison.Ordinal)
                ? RecoveryTargets.MaxRto + TimeSpan.FromMinutes(30)
                : TimeSpan.FromHours(1);

            return ValueTask.FromResult(new ProjectionRebuildMeasurement(Now, Now + duration, duration, Snapshot, rebuilt, SchemaV1, SchemaV1));
        }
    }
}
