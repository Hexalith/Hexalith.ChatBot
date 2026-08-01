using System.Text.Json;

using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>Ensures retained live-recovery provenance and safety assertions remain metadata-only.</summary>
public sealed class LiveRecoveryEvidenceLeakageScanTests
{
    [Fact]
    public void CompleteScopedOutageManifestCarriesNoTenantPayloadOrCredentialSentinel()
    {
        DateTimeOffset started = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
        RecoveryValidationEvidenceManifest manifest = new()
        {
            RunId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ScenarioId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            StartedAtUtc = started,
            EndedAtUtc = started + TimeSpan.FromSeconds(2),
            RepositoryCommit = "1493ff8f2f7e031bc386a2d379d95649744fe7ee",
            AppHostVersion = "chatbot-apphost-v1",
            AspireVersion = "13.4.6",
            DaprVersion = "1.18.1",
            TopologyVersion = "aspire-single-replica-recovery-v1",
            ConfigurationVersion = "live-recovery-v1",
            TenantRef = "replay-test:recovery-validation",
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            DatasetVolume = 6,
            DriverMode = "aspire-tier3-live",
            JobId = LiveRecoveryValidationJobs.ScopedOutage,
            Scenario = ScopedOutageDependencies.AttachmentProcessing,
            InjectedFaultAction = "fault:attachment-processing",
            RestoreAction = "restore:attachment-processing",
            CleanupAction = "cleanup:attachment-processing",
            ExpectedScope = ScopedOutageScopes.WorkflowItem,
            ObservedScope = ScopedOutageScopes.WorkflowItem,
            ReportKind = LiveRecoveryValidationJobs.ScopedOutage,
            Verdict = ScopedOutageDegradationVerdicts.Contained,
            ReasonCode = ScopedOutageDegradationReport.ValidationCompletedReasonCode,
            MeasurementsSeconds = new Dictionary<string, double> { ["scope-recording-latency"] = 0.025 },
            AllowedTargetsSeconds = new Dictionary<string, double> { ["scope-recording-latency"] = 300 },
            Assertions = new Dictionary<string, bool>
            {
                ["cleanup-complete"] = true,
                ["cross-tenant-leakage-absent"] = true,
                ["duplicate-side-effect-absent"] = true,
                ["inflight-items-recoverable"] = true,
                ["silent-data-loss-absent"] = true,
                ["unauthorized-mutation-absent"] = true,
            },
            Coverage = new Dictionary<string, int> { ["scenario"] = 1 },
            Deviations = [],
            ResidualIds = ["RV-PROVIDER-SCALE"],
            MeasurableRecoveryCeilingSeconds = 180,
            ArtifactLocators = new Dictionary<string, string>
            {
                ["test-output"] = "artifact:live-recovery/results.trx",
                ["reports"] = "artifact:live-recovery/reports",
                ["logs"] = "artifact:live-recovery/logs",
                ["traces"] = "artifact:live-recovery/traces",
                ["metrics"] = "artifact:live-recovery/metrics",
                ["state-end-state"] = "artifact:live-recovery/state-end-state",
            },
        };

        manifest.Validate().ShouldBeEmpty();
        string rendered = JsonSerializer.Serialize(manifest, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Should.NotThrow(() => CrossTenantLeakageScanner.ScanAll(
            "live-recovery-evidence",
            "replay-test:recovery-validation",
            rendered));
    }
}
