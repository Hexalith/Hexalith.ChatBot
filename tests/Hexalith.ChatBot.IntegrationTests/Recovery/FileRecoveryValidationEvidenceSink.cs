using System.Text.Json;

using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Tier-3 metadata-only JSON artifact writer for canonical reports and their execution manifests.</summary>
internal sealed class FileRecoveryValidationEvidenceSink(
    LiveRecoveryValidationOptions options,
    string repositoryCommit,
    string daprRuntimeVersion,
    string aspireVersion) : IRecoveryValidationEvidenceSink
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public ValueTask RecordAsync(ContinuityDrillReport report, CancellationToken cancellationToken)
    {
        bool measurable = !string.Equals(report.Verdict, ContinuityDrillVerdicts.Unmeasurable, StringComparison.Ordinal);
        RecoveryValidationExecutionAssertions? execution = report.ExecutionAssertions;
        return WriteAsync(
                report,
                report.Scenario,
                LiveRecoveryValidationJobs.Continuity,
                report.Verdict,
                report.ReasonCode,
                report.StartedAtUtc,
                report.EndedAtUtc,
                new Dictionary<string, double>
                {
                    ["rpo"] = report.MeasuredRpo.TotalSeconds,
                    ["rto"] = report.MeasuredRto.TotalSeconds,
                },
                new Dictionary<string, double>
                {
                    ["rpo"] = RecoveryTargets.MaxRpo.TotalSeconds,
                    ["rto"] = RecoveryTargets.MaxRto.TotalSeconds,
                },
                new Dictionary<string, bool>
                {
                    ["cleanup-complete"] = execution?.CleanupComplete is true,
                    ["data-loss-absent"] = measurable && !report.DataLossDetected,
                    ["fault-observed"] = execution?.FaultObserved is true,
                    ["recovery-observed"] = execution?.RecoveryObserved is true,
                    ["state-reconstructable"] = execution?.StateReconstructable is true,
                    ["tenant-isolation-preserved"] = execution?.TenantIsolationPreserved is true,
                    ["unauthorized-mutation-absent"] = execution?.UnauthorizedMutationAbsent is true,
                    ["measurable"] = measurable,
                },
                report.Deviations,
                cancellationToken);
    }

    public ValueTask RecordAsync(ProjectionRebuildReport report, CancellationToken cancellationToken)
    {
        bool measurable = !string.Equals(report.Verdict, ProjectionRebuildVerdicts.Unmeasurable, StringComparison.Ordinal);
        RecoveryValidationExecutionAssertions? execution = report.ExecutionAssertions;
        return WriteAsync(
                report,
                report.DatasetRef,
                LiveRecoveryValidationJobs.ProjectionRebuild,
                report.Verdict,
                report.ReasonCode,
                report.StartedAtUtc,
                report.EndedAtUtc,
                new Dictionary<string, double> { ["rebuild-duration"] = report.MeasuredRebuildDuration.TotalSeconds },
                new Dictionary<string, double> { ["rebuild-duration"] = RecoveryTargets.MaxRto.TotalSeconds },
                new Dictionary<string, bool>
                {
                    ["cleanup-complete"] = execution?.CleanupComplete is true,
                    ["duration-within-target"] = report.DurationWithinTarget,
                    ["immutable-source-only"] = execution?.ImmutableSourceOnly is true,
                    ["mailbox-reingestion-absent"] = execution?.MailboxReingestionAbsent is true,
                    ["structurally-equivalent"] = measurable && !report.IsDivergent,
                    ["tenant-isolation-preserved"] = execution?.TenantIsolationPreserved is true,
                },
                report.Deviations,
                cancellationToken,
                report.ResourcesCompared);
    }

    public ValueTask RecordAsync(ScopedOutageDegradationReport report, CancellationToken cancellationToken)
    {
        bool measurable = !string.Equals(
            report.Verdict,
            ScopedOutageDegradationVerdicts.Unmeasurable,
            StringComparison.Ordinal);
        RecoveryValidationExecutionAssertions? execution = report.ExecutionAssertions;
        return WriteAsync(
                report,
                report.Dependency,
                LiveRecoveryValidationJobs.ScopedOutage,
                report.Verdict,
                report.ReasonCode,
                report.StartedAtUtc,
                report.EndedAtUtc,
                new Dictionary<string, double> { ["scope-recording-latency"] = report.ScopeRecordingLatency.TotalSeconds },
                new Dictionary<string, double> { ["scope-recording-latency"] = RecoveryTargets.MaxScopeRecordingLatency.TotalSeconds },
                new Dictionary<string, bool>
                {
                    ["fault-observed"] = execution?.FaultObserved is true,
                    ["independent-control-succeeded"] = execution?.IndependentControlSucceeded is true,
                    ["recovery-observed"] = execution?.RecoveryObserved is true,
                    ["cleanup-complete"] = execution?.CleanupComplete is true,
                    ["control-tenant-isolated"] = execution?.TenantIsolationPreserved is true,
                    ["scope-recorded-within-target"] = report.ScopeRecordedWithinTarget,
                    ["scope-contained"] = measurable && !report.IsScopeBreach,
                    ["cross-tenant-leakage-absent"] = AssertionPassed(
                        report,
                        measurable,
                        ScopedOutageDegradationEvaluator.CrossTenantLeakageDeviation),
                    ["unauthorized-mutation-absent"] = AssertionPassed(
                        report,
                        measurable,
                        ScopedOutageDegradationEvaluator.UnauthorizedMutationDeviation),
                    ["silent-data-loss-absent"] = AssertionPassed(
                        report,
                        measurable,
                        ScopedOutageDegradationEvaluator.SilentDataLossDeviation),
                    ["inflight-items-recoverable"] = AssertionPassed(
                        report,
                        measurable,
                        ScopedOutageDegradationEvaluator.InflightNotRecoverableDeviation),
                    ["duplicate-side-effect-absent"] = AssertionPassed(
                        report,
                        measurable,
                        ScopedOutageDegradationEvaluator.DuplicateSideEffectDeviation),
                },
                report.Deviations,
                cancellationToken);
    }

    private async ValueTask WriteAsync<TReport>(
        TReport report,
        string scenario,
        string reportKind,
        string verdict,
        string reasonCode,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        IReadOnlyDictionary<string, double> measurements,
        IReadOnlyDictionary<string, double> targets,
        IReadOnlyDictionary<string, bool> assertions,
        IReadOnlyList<string> deviations,
        CancellationToken cancellationToken,
        int coverage = 1)
    {
        Directory.CreateDirectory(options.EvidenceDirectory);
        string scenarioId = ChatBotCorrelationId.New().Value;
        RecoveryValidationEvidenceManifest manifest = new()
        {
            RunId = ReportCorrelationId(report),
            ScenarioId = scenarioId,
            StartedAtUtc = startedAtUtc.ToUniversalTime(),
            EndedAtUtc = endedAtUtc.ToUniversalTime(),
            RepositoryCommit = repositoryCommit,
            AppHostVersion = "chatbot-apphost-v1",
            // Resolved from the loaded Aspire assembly, not typed here: a literal silently kept claiming the old
            // version across an SDK bump, so published provenance could disagree with the topology that actually ran.
            AspireVersion = aspireVersion,
            DaprVersion = daprRuntimeVersion,
            TopologyVersion = "aspire-single-replica-recovery-v1",
            ConfigurationVersion = "live-recovery-high-capacity-v1",
            TenantRef = options.TestTenantRef,
            DatasetRef = options.DatasetRef,
            DatasetVersion = options.DatasetVersion,
            DatasetVolume = options.DatasetVolume,
            DriverMode = RecoveryValidationEvidenceManifest.LiveDriverMode,
            JobId = reportKind,
            Scenario = scenario,
            InjectedFaultAction = InjectedFaultAction(reportKind, scenario),
            RestoreAction = RestoreAction(reportKind, scenario),
            CleanupAction = $"cleanup:{scenario}",
            ExpectedScope = report is ScopedOutageDegradationReport scoped ? scoped.ExpectedScope : "tenant",
            ObservedScope = report is ScopedOutageDegradationReport observed ? observed.ObservedScope : "tenant",
            ReportKind = reportKind,
            Verdict = verdict,
            ReasonCode = reasonCode,
            MeasurementsSeconds = measurements,
            AllowedTargetsSeconds = targets,
            Assertions = assertions,
            Coverage = new Dictionary<string, int> { ["scenario"] = Math.Max(coverage, 0) },
            // The longest recovery this lane could observe. Published so a reader cannot mistake "met" against a
            // 4-hour target for proof the 4-hour target was exercised: anything slower than this ceiling becomes
            // unmeasurable, never a miss.
            MeasurableRecoveryCeilingSeconds = options.RestorationTimeout.TotalSeconds,
            Deviations = deviations,
            ResidualIds = ResidualIds(reportKind, scenario),
            // Advertise ONLY what the workflow actually uploads (the .trx plus this evidence directory's reports and
            // manifests). Emitting logs/traces/metrics/state-end-state locators produced links to artifacts the lane
            // never generates, which the syntax-only locator check happily accepted.
            ArtifactLocators = new Dictionary<string, string>
            {
                ["test-output"] = $"{options.EvidenceLocator}/results.trx",
                ["reports"] = $"{options.EvidenceLocator}/reports",
            },
        };
        IReadOnlyList<string> errors = manifest.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException("The live recovery evidence manifest failed metadata validation.");
        }

        string safeScenario = scenario.Replace(':', '-');
        string manifestPath = Path.Combine(options.EvidenceDirectory, $"{safeScenario}-{scenarioId}.manifest.json");
        string reportPath = Path.Combine(options.EvidenceDirectory, $"{safeScenario}-{scenarioId}.report.json");
        await File.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(manifest, SerializerOptions),
            cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(report, SerializerOptions),
            cancellationToken).ConfigureAwait(false);
    }

    private static string ReportCorrelationId<TReport>(TReport report)
        => report switch
        {
            ContinuityDrillReport continuity => continuity.CorrelationId,
            ProjectionRebuildReport rebuild => rebuild.CorrelationId,
            ScopedOutageDegradationReport outage => outage.CorrelationId,
            _ => throw new InvalidOperationException("Unknown canonical recovery report type."),
        };

    private static bool AssertionPassed(
        ScopedOutageDegradationReport report,
        bool measurable,
        string failedDeviation)
        => measurable && !report.Deviations.Contains(failedDeviation, StringComparer.Ordinal);

    private static string InjectedFaultAction(string reportKind, string scenario)
        => (reportKind, scenario) switch
        {
            ("continuity", ContinuityDrillScenarios.EventStoreOutage) => "stop:eventstore",
            ("continuity", ContinuityDrillScenarios.M365SubscriptionFailure) => "fault:graph-subscription",
            ("projection-rebuild", _) => "rebuild:fresh-partition",
            ("scoped-outage", ScopedOutageDependencies.Identity) => "stop:security",
            ("scoped-outage", ScopedOutageDependencies.Graph) => "fault:graph-subscription",
            _ => $"fault:{scenario}",
        };

    private static string RestoreAction(string reportKind, string scenario)
        => (reportKind, scenario) switch
        {
            ("continuity", ContinuityDrillScenarios.EventStoreOutage) => "start:eventstore",
            ("continuity", ContinuityDrillScenarios.M365SubscriptionFailure) => "restore:graph-subscription",
            ("projection-rebuild", _) => "verify:fresh-partition",
            ("scoped-outage", ScopedOutageDependencies.Identity) => "start:security",
            ("scoped-outage", ScopedOutageDependencies.Graph) => "restore:graph-subscription",
            _ => $"restore:{scenario}",
        };

    private static IReadOnlyList<string> ResidualIds(string reportKind, string scenario)
        => (reportKind, scenario) switch
        {
            ("continuity", ContinuityDrillScenarios.M365SubscriptionFailure) or
            ("scoped-outage", ScopedOutageDependencies.Graph) =>
                ["RV-EXT-M365", "RV-PROD-CONTROL", "RV-PROVIDER-SCALE"],
            ("projection-rebuild", _) or ("scoped-outage", ScopedOutageDependencies.AuditStore) =>
                ["RV-DURABLE-WORM", "RV-PROD-CONTROL", "RV-PROVIDER-SCALE"],
            _ => ["RV-PROD-CONTROL", "RV-PROVIDER-SCALE"],
        };
}
