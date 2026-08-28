using System.Text.Json;

using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

public sealed class FileRecoveryValidationEvidenceSinkTests
{
    [Fact]
    public async Task RecordAsyncDistinguishesConfiguredCorpusVolumeFromScenarioExerciseVolume()
    {
        string evidenceDirectory = CreateEvidenceDirectory();
        LiveRecoveryValidationOptions options = Options(evidenceDirectory);
        FileRecoveryValidationEvidenceSink sink = new(
            options,
            repositoryCommit: "0123456789abcdef0123456789abcdef01234567",
            daprRuntimeVersion: "1.18.4",
            aspireVersion: "13.4.6",
            appHostVersion: "1.0.0");
        DateTimeOffset started = DateTimeOffset.Parse(
            "2026-08-01T00:00:00Z",
            System.Globalization.CultureInfo.InvariantCulture);

        await sink.RecordAsync(
            MetReport(ChatBotCorrelationId.New().Value, ContinuityDrillScenarios.EventStoreOutage, started),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sink.RecordAsync(
            ControlledLossReport(ChatBotCorrelationId.New().Value, started.AddSeconds(30)),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sink.RecordAsync(
            EquivalentProjectionReport(ChatBotCorrelationId.New().Value, started.AddMinutes(1), resourcesCompared: 2),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sink.RecordAsync(
            ContainedScopedOutageReport(ChatBotCorrelationId.New().Value, started.AddMinutes(2)),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        RecoveryValidationEvidenceManifest[] manifests = Directory
            .GetFiles(evidenceDirectory, "*.manifest.json")
            .Select(DeserializeManifest)
            .ToArray();

        manifests.Length.ShouldBe(4);
        manifests.ShouldAllBe(static manifest => manifest.ConfiguredDatasetVolume == 6);
        manifests.Single(static manifest => manifest.JobId == LiveRecoveryValidationJobs.Continuity)
            .DatasetVolume.ShouldBe(0);
        manifests.Single(static manifest => manifest.JobId == LiveRecoveryValidationJobs.ControlledLossPath)
            .DatasetVolume.ShouldBe(0);
        manifests.Single(static manifest => manifest.JobId == LiveRecoveryValidationJobs.ProjectionRebuild)
            .DatasetVolume.ShouldBe(2);
        manifests.Single(static manifest => manifest.JobId == LiveRecoveryValidationJobs.ScopedOutage)
            .DatasetVolume.ShouldBe(0);
    }

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
    public async Task RecordAsyncRetainsControlledLossDurableBoundsAsMetadataOnlyEvidence()
    {
        string evidenceDirectory = CreateEvidenceDirectory();
        FileRecoveryValidationEvidenceSink sink = new(
            Options(evidenceDirectory),
            repositoryCommit: "0123456789abcdef0123456789abcdef01234567",
            daprRuntimeVersion: "1.18.4",
            aspireVersion: "13.4.6",
            appHostVersion: "1.0.0");
        DateTimeOffset started = DateTimeOffset.Parse("2026-08-01T00:00:00Z");

        await sink.RecordAsync(
            ControlledLossReport(ChatBotCorrelationId.New().Value, started),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        RecoveryValidationEvidenceManifest manifest = DeserializeManifest(
            Directory.GetFiles(evidenceDirectory, "*.manifest.json").Single());
        manifest.Validate().ShouldBeEmpty();
        manifest.JobId.ShouldBe(LiveRecoveryValidationJobs.ControlledLossPath);
        manifest.MeasurementsSeconds["rpo"].ShouldBe(20);
        manifest.AllowedTargetsSeconds["rpo"].ShouldBe(RecoveryTargets.MaxRpo.TotalSeconds);
        manifest.PreFaultEventRef.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAB");
        manifest.RejectedCandidateRef.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAC");
        manifest.PostRecoveryEventRef.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAE");
        manifest.Assertions["candidate-absent"].ShouldBeTrue();
        manifest.Assertions["rpo-positive"].ShouldBeTrue();
    }

    /// <summary>
    /// Ties the producer's hand-listed assertion dictionary and residual set to what the gate actually demands. Every
    /// other controlled-loss gate test builds its manifest from <c>RequiredAssertionsFor</c> or by hand, so a renamed
    /// or dropped key in the sink would have surfaced first on a hosted run.
    /// </summary>
    [Fact]
    public async Task RecordedControlledLossManifestSatisfiesTheGateItWillBeReplayedThrough()
    {
        string evidenceDirectory = CreateEvidenceDirectory();
        FileRecoveryValidationEvidenceSink sink = new(
            Options(evidenceDirectory),
            repositoryCommit: "0123456789abcdef0123456789abcdef01234567",
            daprRuntimeVersion: "1.18.4",
            aspireVersion: "13.4.6",
            appHostVersion: "1.0.0");

        await sink.RecordAsync(
            ControlledLossReport(ChatBotCorrelationId.New().Value, DateTimeOffset.Parse("2026-08-01T00:00:00Z")),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        RecoveryValidationEvidenceManifest manifest = DeserializeManifest(
            Directory.GetFiles(evidenceDirectory, "*.manifest.json").Single());

        foreach (string assertion in LiveRecoveryValidationEvidenceGate.RequiredAssertionsFor(
            LiveRecoveryValidationJobs.ControlledLossPath))
        {
            manifest.Assertions.ShouldContainKey(assertion);
            manifest.Assertions[assertion].ShouldBeTrue();
        }

        // The drill faults and restores the composed Graph/subscription boundary, so the manifest that carries the
        // RPO claim must declare the same external-M365 limitation the other subscription manifests declare.
        manifest.ResidualIds.ShouldContain("RV-EXT-M365");
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
            ResourcesCompared: 2,
            Deviations: [],
            FirstDivergingResourceLocator: null,
            ProjectConversationSourceEmailView.CurrentSchemaVersion,
            ChatBotCorrelationId.New().Value,
            ProjectionRebuildReport.ValidationCompletedReasonCode,
            PreRebuildDigests: SnapshotDigests(2),
            RebuiltDigests: SnapshotDigests(2),
            PreRebuildSchemaVersion: "source-v1|governed-v1",
            RebuiltSchemaVersion: "source-v1|governed-v1",
            SourceResourcesCompared: 1,
            GovernedResourcesCompared: 1,
            WormRecordsReplayed: 1,
            WormOperationsReplayed: 1,
            FingerprintAlgorithmVersion: ProjectionSnapshotFingerprint.AlgorithmVersion,
            PreRebuildFingerprint: ProjectionSnapshotFingerprint.Compute(SnapshotDigests(2)),
            RebuiltFingerprint: ProjectionSnapshotFingerprint.Compute(SnapshotDigests(2)));

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

    private static ControlledLossPathReport ControlledLossReport(string correlationId, DateTimeOffset started)
        => new(
            RecoveryValidationTopology.LogicalTenantRef,
            ControlledLossPathReport.SubscriptionNotificationRejectionScenario,
            started,
            started.AddSeconds(40),
            TimeSpan.FromSeconds(20),
            "01ARZ3NDEKTSV4RRFFQ69G5FAA",
            "01ARZ3NDEKTSV4RRFFQ69G5FAB",
            1,
            started.AddSeconds(10),
            "01ARZ3NDEKTSV4RRFFQ69G5FAC",
            started.AddSeconds(20),
            "01ARZ3NDEKTSV4RRFFQ69G5FAD",
            "01ARZ3NDEKTSV4RRFFQ69G5FAE",
            1,
            started.AddSeconds(30),
            PreFaultRetained: true,
            CandidateRejected: true,
            CandidateAbsent: true,
            PostRecoveryRetained: true,
            TenantIsolationPreserved: true,
            UnauthorizedMutationAbsent: true,
            CleanupComplete: true,
            ControlledLossPathVerdicts.Met,
            Deviations: [],
            correlationId,
            ControlledLossPathReport.CompletedReasonCode);

    private static ProjectionRebuildReport EquivalentProjectionReport(
        string correlationId,
        DateTimeOffset started,
        int resourcesCompared)
    {
        IReadOnlyList<ProjectionResourceDigest> digests = SnapshotDigests(resourcesCompared);
        return new(
            RecoveryValidationTopology.LogicalTenantRef,
            DatasetRef: "recovery-baseline",
            started,
            started.AddSeconds(1),
            MeasuredRebuildDuration: TimeSpan.FromSeconds(1),
            DurationWithinTarget: true,
            ProjectionRebuildVerdicts.Equivalent,
            resourcesCompared,
            Deviations: [],
            FirstDivergingResourceLocator: null,
            ProjectConversationSourceEmailView.CurrentSchemaVersion,
            correlationId,
            ProjectionRebuildReport.ValidationCompletedReasonCode,
            PreRebuildDigests: digests,
            RebuiltDigests: digests,
            PreRebuildSchemaVersion: "source-v1|governed-v1",
            RebuiltSchemaVersion: "source-v1|governed-v1",
            SourceResourcesCompared: 1,
            GovernedResourcesCompared: resourcesCompared - 1,
            WormRecordsReplayed: resourcesCompared - 1,
            WormOperationsReplayed: resourcesCompared - 1,
            FingerprintAlgorithmVersion: ProjectionSnapshotFingerprint.AlgorithmVersion,
            PreRebuildFingerprint: ProjectionSnapshotFingerprint.Compute(digests),
            RebuiltFingerprint: ProjectionSnapshotFingerprint.Compute(digests));
    }

    private static IReadOnlyList<ProjectionResourceDigest> SnapshotDigests(int count)
        =>
        [
            .. Enumerable.Range(0, count).Select(index => new ProjectionResourceDigest(
                $"resource-{index}",
                new string((char)('a' + index), 64))),
        ];

    private static ScopedOutageDegradationReport ContainedScopedOutageReport(
        string correlationId,
        DateTimeOffset started)
        => new(
            RecoveryValidationTopology.LogicalTenantRef,
            ScopedOutageDependencies.Graph,
            ScopedOutageScopes.Tenant,
            ScopedOutageScopes.Tenant,
            started,
            started.AddSeconds(1),
            ScopeRecordingLatency: TimeSpan.FromSeconds(1),
            ScopeRecordedWithinTarget: true,
            ScopedOutageDegradationVerdicts.Contained,
            Deviations: [],
            FirstBreachLocator: null,
            correlationId,
            ScopedOutageDegradationReport.ValidationCompletedReasonCode,
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
            EvidenceLocator = "artifact:live-recovery-validation-evidence",
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
