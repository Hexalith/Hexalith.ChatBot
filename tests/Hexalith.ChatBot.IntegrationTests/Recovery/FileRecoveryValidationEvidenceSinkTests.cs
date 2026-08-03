using System.Text.Json;

using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

public sealed class FileRecoveryValidationEvidenceSinkTests
{
    [Fact]
    public async Task RecordAsyncRetainsMetAndMissedContinuityReportsWithProvenance()
    {
        string evidenceDirectory = CreateEvidenceDirectory();
        LiveRecoveryValidationOptions options = Options(evidenceDirectory);
        FileRecoveryValidationEvidenceSink sink = new(
            options,
            repositoryCommit: "0123456789abcdef0123456789abcdef01234567",
            daprRuntimeVersion: "1.18.4",
            aspireVersion: "13.4.6",
            appHostVersion: "1.0.0");

        string runId = ChatBotCorrelationId.New().Value;
        DateTimeOffset started = DateTimeOffset.Parse("2026-08-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        await sink.RecordAsync(
            MetReport(runId, ContinuityDrillScenarios.EventStoreOutage, started),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sink.RecordAsync(
            MissedReport(runId, ContinuityDrillScenarios.M365SubscriptionFailure, started.AddMinutes(1)),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        string[] manifests = Directory.GetFiles(evidenceDirectory, "*.manifest.json");
        string[] reports = Directory.GetFiles(evidenceDirectory, "*.report.json");
        manifests.Length.ShouldBe(2);
        reports.Length.ShouldBe(2);

        foreach (string manifestPath in manifests)
        {
            RecoveryValidationEvidenceManifest manifest = DeserializeManifest(manifestPath);
            manifest.Validate().ShouldBeEmpty();
            manifest.AppHostVersion.ShouldBe("1.0.0");
            manifest.AspireVersion.ShouldBe("13.4.6");
            manifest.ExpectedScope.ShouldBe("not-applicable");
            manifest.ObservedScope.ShouldBe("not-applicable");
            manifest.ResidualIds.ShouldContain("RV-EVIDENCE-KINDS");
            manifest.ArtifactLocators.Keys.OrderBy(static key => key).ShouldBe(["reports", "test-output"]);
            Path.GetFileName(manifestPath).ShouldNotContain(":");
            Path.GetFileName(manifestPath).ShouldNotContain("/");
        }
    }

    [Fact]
    public async Task RecordAsyncSupersedesPriorArtifactForTheSameScenario()
    {
        string evidenceDirectory = CreateEvidenceDirectory();
        LiveRecoveryValidationOptions options = Options(evidenceDirectory);
        FileRecoveryValidationEvidenceSink sink = new(
            options,
            repositoryCommit: "0123456789abcdef0123456789abcdef01234567",
            daprRuntimeVersion: "1.18.4",
            aspireVersion: "13.4.6",
            appHostVersion: "1.0.0");

        DateTimeOffset started = DateTimeOffset.Parse("2026-08-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        string firstCorrelationId = ChatBotCorrelationId.New().Value;
        await sink.RecordAsync(
            MetReport(firstCorrelationId, ContinuityDrillScenarios.EventStoreOutage, started),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        string manifestPath = Path.Combine(evidenceDirectory, $"{ContinuityDrillScenarios.EventStoreOutage}.manifest.json");
        RecoveryValidationEvidenceManifest firstManifest = DeserializeManifest(manifestPath);
        firstManifest.Verdict.ShouldBe(ContinuityDrillVerdicts.Met);

        // A retention-fallback unmeasurable substitute for the same scenario must overwrite the prior artifact
        // in place, not accumulate a second manifest/report pair beside it.
        string secondCorrelationId = ChatBotCorrelationId.New().Value;
        await sink.RecordAsync(
            ContinuityDrillReport.Unmeasurable(
                options.TestTenantRef,
                ContinuityDrillScenarios.EventStoreOutage,
                secondCorrelationId,
                started.AddMinutes(2),
                started.AddMinutes(3)),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Directory.GetFiles(evidenceDirectory, "*.manifest.json").Length.ShouldBe(1);
        Directory.GetFiles(evidenceDirectory, "*.report.json").Length.ShouldBe(1);

        RecoveryValidationEvidenceManifest supersededManifest = DeserializeManifest(manifestPath);
        supersededManifest.Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        supersededManifest.RunId.ShouldBe(secondCorrelationId);
        supersededManifest.ScenarioId.ShouldNotBe(firstManifest.ScenarioId);
    }

    [Fact]
    public async Task RecordAsyncSanitizesPathSeparatorsInScenarioTokens()
    {
        string evidenceDirectory = CreateEvidenceDirectory();
        FileRecoveryValidationEvidenceSink sink = new(
            Options(evidenceDirectory),
            repositoryCommit: "0123456789abcdef0123456789abcdef01234567",
            daprRuntimeVersion: "1.18.4",
            aspireVersion: "13.4.6",
            appHostVersion: "1.0.0");

        // Continuity scenarios are closed-set; abuse the dataset-ref slot of a projection rebuild report instead.
        // ':' is allowed by manifest token rules but must not appear in evidence filenames.
        ProjectionRebuildReport report = new(
            Options(evidenceDirectory).TestTenantRef,
            DatasetRef: "recovery:baseline-escape",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddSeconds(1),
            MeasuredRebuildDuration: TimeSpan.FromSeconds(1),
            DurationWithinTarget: true,
            ProjectionRebuildVerdicts.Equivalent,
            ResourcesCompared: 1,
            Deviations: [],
            FirstDivergingResourceLocator: null,
            ProjectConversationSourceEmailView.CurrentSchemaVersion,
            ChatBotCorrelationId.New().Value,
            ProjectionRebuildReport.ValidationCompletedReasonCode);

        await sink.RecordAsync(report, TestContext.Current.CancellationToken).ConfigureAwait(true);

        string[] manifests = Directory.GetFiles(evidenceDirectory, "*.manifest.json");
        manifests.Length.ShouldBe(1);
        Path.GetFileName(manifests[0]).ShouldNotContain(":");
        Path.GetFullPath(manifests[0]).StartsWith(
            Path.GetFullPath(evidenceDirectory) + Path.DirectorySeparatorChar,
            StringComparison.Ordinal).ShouldBeTrue();
    }

    private static ContinuityDrillReport MetReport(string correlationId, string scenario, DateTimeOffset started)
        => new(
            RecoveryValidationTopology.LogicalTenantRef,
            scenario,
            started,
            started.AddSeconds(30),
            MeasuredRpo: TimeSpan.Zero,
            MeasuredRto: TimeSpan.FromSeconds(30),
            DataLossDetected: false,
            ContinuityDrillVerdicts.Met,
            Deviations: [],
            RecalibrationFlag: false,
            FollowUpActionRef: null,
            correlationId,
            ContinuityDrillReport.DrillCompletedReasonCode,
            new RecoveryValidationExecutionAssertions(
                CleanupComplete: true,
                FaultObserved: true,
                RecoveryObserved: true,
                IndependentControlSucceeded: true,
                TenantIsolationPreserved: true,
                UnauthorizedMutationAbsent: true,
                StateReconstructable: true,
                ImmutableSourceOnly: false,
                MailboxReingestionAbsent: false));

    private static ContinuityDrillReport MissedReport(string correlationId, string scenario, DateTimeOffset started)
        => MetReport(correlationId, scenario, started) with
        {
            Verdict = ContinuityDrillVerdicts.Missed,
            MeasuredRto = TimeSpan.FromHours(5),
            RecalibrationFlag = true,
            Deviations = ["rto_target_missed"],
        };

    private static LiveRecoveryValidationOptions Options(string evidenceDirectory)
        => new()
        {
            Enabled = true,
            EnvironmentName = "Testing",
            TestTenantRef = RecoveryValidationTopology.LogicalTenantRef,
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            DatasetVolume = 6,
            ProjectionSchemaVersion = ProjectConversationSourceEmailView.CurrentSchemaVersion,
            ValidationPartitionRef = "recovery-partition-v1",
            ControllerCapability = LiveRecoveryValidationOptions.AspireControllerCapability,
            ControllerSecret = "tier3-value",
            RestorationTimeout = TimeSpan.FromMinutes(3),
            EvidenceDirectory = evidenceDirectory,
            EvidenceLocator = "artifact:live-recovery-validation",
        };

    private static string CreateEvidenceDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"live-recovery-evidence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static RecoveryValidationEvidenceManifest DeserializeManifest(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<RecoveryValidationEvidenceManifest>(
            stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
