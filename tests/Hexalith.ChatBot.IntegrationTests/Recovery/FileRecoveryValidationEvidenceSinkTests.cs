using System.Text.Json;

using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

public sealed class FileRecoveryValidationEvidenceSinkTests
{
    /// <summary>Every emitted scenario retains its complete independently specified scope limitations.</summary>
    [Fact]
    public async Task EveryEmittedScenarioCarriesItsCompleteResidualSet()
    {
        string directory = CreateEvidenceDirectory();
        FileRecoveryValidationEvidenceSink sink = new(Options(directory),
            "0123456789abcdef0123456789abcdef01234567", "1.18.4", "13.4.6", "1.0.0");
        DateTimeOffset started = DateTimeOffset.UtcNow;
        string runId = ChatBotCorrelationId.New().Value;
        try
        {
            await WriteAllPassingReportsAsync(sink, runId, started, TestContext.Current.CancellationToken).ConfigureAwait(true);

            RecoveryValidationEvidenceManifest[] manifests = Directory.GetFiles(directory, "*.manifest.json")
                .Select(DeserializeManifest).ToArray();
            manifests.Length.ShouldBe(10);
            foreach (RecoveryValidationEvidenceManifest manifest in manifests)
            {
                string[] expected = ["RV-PROD-CONTROL", "RV-PROVIDER-SCALE", "RV-EVIDENCE-KINDS"];
                if ((manifest.JobId == "continuity" && manifest.Scenario == ContinuityDrillScenarios.M365SubscriptionFailure)
                    || manifest.JobId == "controlled-loss-path"
                    || (manifest.JobId == "scoped-outage" && manifest.Scenario == ScopedOutageDependencies.Graph))
                {
                    expected = [.. expected, "RV-EXT-M365"];
                }
                else if (manifest.JobId == "projection-rebuild"
                    || (manifest.JobId == "scoped-outage" && manifest.Scenario == ScopedOutageDependencies.AuditStore))
                {
                    expected = [.. expected, "RV-DURABLE-WORM"];
                }

                manifest.ResidualIds.Order(StringComparer.Ordinal).ShouldBe(expected.Order(StringComparer.Ordinal));
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Replays the sink's exported bytes with release policy and checks missing, corrupt, and mismatched inputs.</summary>
    [Theory]
    [InlineData("valid")]
    [InlineData("missing-summary")]
    [InlineData("missing-manifest")]
    [InlineData("missing-report")]
    [InlineData("missing-pair")]
    [InlineData("malformed-summary")]
    [InlineData("malformed-manifest")]
    [InlineData("malformed-report")]
    [InlineData("different-commit")]
    [InlineData("different-run")]
    [InlineData("different-report")]
    [InlineData("report-contradiction")]
    public async Task RetainedArtifactReplaysTheExportedEvidence(string scenario)
    {
        string artifactRoot = Directory.CreateTempSubdirectory("chatbot-retained-recovery-").FullName;
        try
        {
            await VerifyRetainedArtifactAsync(artifactRoot, scenario).ConfigureAwait(true);
        }
        finally
        {
            Directory.Delete(artifactRoot, recursive: true);
        }
    }

    private static async Task VerifyRetainedArtifactAsync(string artifactRoot, string scenario)
    {
        const string Commit = "0123456789abcdef0123456789abcdef01234567";
        DateTimeOffset started = DateTimeOffset.UtcNow.AddMinutes(-2);
        string runId = ChatBotCorrelationId.New().Value;
        string manifestRoot = Path.Combine(artifactRoot, "live-recovery", runId);
        string summaryRoot = Path.Combine(artifactRoot, "retention-failures");
        string summaryPath = Path.Combine(summaryRoot, LiveRecoveryValidationAttemptSummary.FileName);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        FileRecoveryValidationEvidenceSink sink = new(Options(manifestRoot), Commit, "1.18.4", "13.4.6", "1.0.0");
        await WriteAllPassingReportsAsync(sink, runId, started, cancellationToken).ConfigureAwait(true);
        Directory.CreateDirectory(summaryRoot);
        LiveRecoveryValidationAttemptSummary summary = new()
        {
            Enabled = true,
            RunId = scenario == "different-run" ? ChatBotCorrelationId.New().Value : runId,
            StartedAtUtc = started,
            CompletedAtUtc = started.AddMinutes(1),
            LatestAttemptCompletedSuccessfully = true,
            AlertsDeliveredByJob = new Dictionary<string, int>(StringComparer.Ordinal),
        };
        await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(summary, JsonSerializerOptions.Web), cancellationToken)
            .ConfigureAwait(true);

        string manifestPath = Path.Combine(manifestRoot, $"{ContinuityDrillScenarios.EventStoreOutage}.manifest.json");
        string reportPath = Path.Combine(manifestRoot, $"{ContinuityDrillScenarios.EventStoreOutage}.report.json");
        string? expectedError = null;
        switch (scenario)
        {
            case "missing-summary":
                File.Delete(summaryPath);
                expectedError = "attempt summary is missing";
                break;
            case "missing-manifest":
                File.Delete(manifestPath);
                expectedError = "report is missing its manifest";
                break;
            case "missing-report":
                File.Delete(reportPath);
                expectedError = "report is missing";
                break;
            case "missing-pair":
                File.Delete(manifestPath);
                File.Delete(reportPath);
                break;
            case "malformed-summary":
                await File.WriteAllTextAsync(summaryPath, "{", cancellationToken).ConfigureAwait(true);
                expectedError = "attempt summary is malformed";
                break;
            case "malformed-manifest":
                await File.WriteAllTextAsync(manifestPath, "{", cancellationToken).ConfigureAwait(true);
                expectedError = "manifest is malformed";
                break;
            case "malformed-report":
                await File.WriteAllTextAsync(reportPath, "{", cancellationToken).ConfigureAwait(true);
                expectedError = "report is malformed";
                break;
            case "report-contradiction":
                System.Text.Json.Nodes.JsonNode contradictory = System.Text.Json.Nodes.JsonNode.Parse(
                    await File.ReadAllTextAsync(reportPath, cancellationToken).ConfigureAwait(true))!;
                contradictory["executionAssertions"]!["cleanupComplete"] = false;
                byte[] contradictoryBytes = JsonSerializer.SerializeToUtf8Bytes(contradictory);
                await File.WriteAllBytesAsync(reportPath, contradictoryBytes, cancellationToken).ConfigureAwait(true);
                System.Text.Json.Nodes.JsonNode pairedManifest = System.Text.Json.Nodes.JsonNode.Parse(
                    await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(true))!;
                pairedManifest["reportSha256"] = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(contradictoryBytes));
                await File.WriteAllTextAsync(manifestPath, pairedManifest.ToJsonString(), cancellationToken).ConfigureAwait(true);
                expectedError = "report does not match its manifest";
                break;
            case "different-report":
                string report = await File.ReadAllTextAsync(reportPath, cancellationToken).ConfigureAwait(true);
                await File.WriteAllTextAsync(
                    reportPath, report.Replace(runId, ChatBotCorrelationId.New().Value, StringComparison.Ordinal), cancellationToken)
                    .ConfigureAwait(true);
                expectedError = "report does not match its manifest";
                break;
        }

        LiveRecoveryValidationGatePolicy policy = LiveRecoveryValidationGatePolicy.ForRelease(
            ["recovery-baseline"], true, RecoveryValidationEvidenceManifest.LiveDriverMode, TimeSpan.FromDays(8),
            "v1", 6, 180, scenario == "different-commit" ? new string('b', 40) : Commit);
        Task<LiveRecoveryValidationEvidenceGateDecision> ReplayAsync() => LiveRecoveryEvidenceGateReplayTests
            .ReplayRetainedArtifactAsync(artifactRoot, policy, cancellationToken);
        if (expectedError is not null)
        {
            InvalidDataException failure = await Should.ThrowAsync<InvalidDataException>(ReplayAsync).ConfigureAwait(true);
            failure.Message.ShouldContain(expectedError);
            return;
        }

        LiveRecoveryValidationEvidenceGateDecision decision = await ReplayAsync().ConfigureAwait(true);
        string? expectedReason = scenario switch
        {
            "different-commit" => $"{LiveRecoveryValidationJobs.Continuity}:repository_commit_unexpected",
            "different-run" => $"{LiveRecoveryValidationJobs.Continuity}:run_mismatch",
            "missing-pair" => $"{LiveRecoveryValidationJobs.Continuity}:incomplete_scenario_set",
            _ => null,
        };
        if (expectedReason is null)
        {
            decision.IsStopShip.ShouldBeFalse(string.Join(", ", decision.StopShipReasons));
        }
        else
        {
            decision.IsStopShip.ShouldBeTrue();
            decision.StopShipReasons.ShouldContain(expectedReason);
        }
    }

    private static async Task WriteAllPassingReportsAsync(
        FileRecoveryValidationEvidenceSink sink, string runId, DateTimeOffset started, CancellationToken cancellationToken)
    {
        foreach (string scenario in ContinuityDrillScenarios.All)
        {
            await sink.RecordAsync(MetReport(runId, scenario, started), cancellationToken).ConfigureAwait(false);
        }

        await sink.RecordAsync(ControlledLossReport(runId, started), cancellationToken).ConfigureAwait(false);
        await sink.RecordAsync(EquivalentProjectionReport(runId, started, 2) with
        {
            ExecutionAssertions = new RecoveryValidationExecutionAssertions(
                CleanupComplete: true,
                FaultObserved: true,
                RecoveryObserved: true,
                IndependentControlSucceeded: true,
                TenantIsolationPreserved: true,
                UnauthorizedMutationAbsent: true,
                StateReconstructable: true,
                ImmutableSourceOnly: true,
                MailboxReingestionAbsent: true),
        }, cancellationToken).ConfigureAwait(false);
        foreach (string dependency in ScopedOutageDependencies.All)
        {
            await sink.RecordAsync(ContainedScopedOutageReport(runId, started) with { Dependency = dependency }, cancellationToken)
                .ConfigureAwait(false);
        }
    }

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
    public async Task UnmeasurableProjectionReportRetainsEmptySnapshotEvidence()
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
        string correlationId = ChatBotCorrelationId.New().Value;

        await sink.RecordAsync(
            ProjectionRebuildReport.Unmeasurable(
                options.TestTenantRef,
                options.DatasetRef,
                correlationId,
                started,
                started.AddSeconds(1),
                ProjectConversationSourceEmailView.CurrentSchemaVersion),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        RecoveryValidationEvidenceManifest manifest = DeserializeManifest(
            Path.Combine(evidenceDirectory, $"{options.DatasetRef}.manifest.json"));
        manifest.Verdict.ShouldBe(ProjectionRebuildVerdicts.Unmeasurable);
        manifest.ReasonCode.ShouldBe(ProjectionRebuildReport.ValidationUnmeasurableReasonCode);
        manifest.ValidateProjectionEvidence().ShouldBeEmpty();
        (manifest.ProjectionPreRebuildDigests ?? []).ShouldBeEmpty();
        (manifest.ProjectionRebuiltDigests ?? []).ShouldBeEmpty();
        manifest.ProjectionFingerprintAlgorithmVersion.ShouldBeNull();
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
            new("source-resource-0", new string('a', 64)),
            .. Enumerable.Range(1, count - 1).Select(index => new ProjectionResourceDigest(
                $"governed-resource-{index}",
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
