using System.Security.Cryptography;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Verifies fail-closed recovery manifest and retention-marker loading behavior.</summary>
public sealed class LiveRecoveryEvidenceGateReplayTests
{
    private const string RetentionFailuresDirectoryName = "retention-failures";

    private const string EvidenceDirectoryVariable = "HEXALITH_CHATBOT_RECOVERY_EVIDENCE_DIR";
    private const int MaximumRetainedFileBytes = 16 * 1024 * 1024;
    private const string EvidenceRequiredVariable = "HEXALITH_CHATBOT_RECOVERY_EVIDENCE_REQUIRED";

    /// <summary>Judges the retained bundle in the separate nightly gate process.</summary>
    [Fact]
    public async Task RetainedLiveRecoveryEvidenceShouldPassTheReleaseGateOutOfProcess()
    {
        string? artifactRoot = Environment.GetEnvironmentVariable(EvidenceDirectoryVariable);
        bool required = string.Equals(Environment.GetEnvironmentVariable(EvidenceRequiredVariable), "1", StringComparison.Ordinal);
        if (required && string.IsNullOrWhiteSpace(artifactRoot))
        {
            throw new InvalidOperationException($"{EvidenceDirectoryVariable} is required for recovery evidence replay.");
        }

        Assert.SkipWhen(string.IsNullOrWhiteSpace(artifactRoot), $"Set {EvidenceDirectoryVariable} to replay a retained bundle.");
        LiveRecoveryValidationGatePolicy policy = LiveRecoveryValidationGatePolicy.ForRelease(
            ReadGateSetting("HEXALITH_CHATBOT_RECOVERY_EXPECTED_DATASETS").Split(',', StringSplitOptions.TrimEntries),
            targetDeviationsBlockRelease: true,
            RecoveryValidationEvidenceManifest.LiveDriverMode,
            TimeSpan.FromHours(ReadGateNumber("HEXALITH_CHATBOT_RECOVERY_MAX_EVIDENCE_AGE_HOURS")),
            expectedDatasetVersion: ReadGateSetting("HEXALITH_CHATBOT_RECOVERY_EXPECTED_DATASET_VERSION"),
            minimumDatasetVolume: ReadGateNumber("HEXALITH_CHATBOT_RECOVERY_MINIMUM_DATASET_VOLUME"),
            maximumMeasurableRecoveryCeilingSeconds: ReadGateNumber("HEXALITH_CHATBOT_RECOVERY_MAX_MEASURABLE_CEILING_SECONDS"),
            requiredRepositoryCommit: ReadGateSetting("HEXALITH_CHATBOT_RECOVERY_REQUIRED_COMMIT"));

        LiveRecoveryValidationEvidenceGateDecision decision = await ReplayRetainedArtifactAsync(
            artifactRoot!, policy, TestContext.Current.CancellationToken).ConfigureAwait(true);
        foreach (string limitation in decision.ClaimLimitationReasons)
        {
            TestContext.Current.TestOutputHelper?.WriteLine($"claim limitation: {limitation}");
        }

        decision.IsStopShip.ShouldBeFalse(
            $"The recovery evidence gate rejected the retained evidence: {string.Join(", ", decision.StopShipReasons)}.");
    }

    /// <summary>Loads only the retained bundle and evaluates it against a policy from the gate caller.</summary>
    internal static async Task<LiveRecoveryValidationEvidenceGateDecision> ReplayRetainedArtifactAsync(
        string artifactRoot,
        LiveRecoveryValidationGatePolicy policy,
        CancellationToken cancellationToken)
    {
        JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web)
        {
            AllowDuplicateProperties = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        LiveRecoveryValidationAttemptSummary summary = await ReadRetainedJsonAsync<LiveRecoveryValidationAttemptSummary>(
            Path.Combine(artifactRoot, RetentionFailuresDirectoryName, LiveRecoveryValidationAttemptSummary.FileName),
            "attempt summary", serializerOptions, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<RecoveryValidationEvidenceRetentionFailureMarker?> markers = await LoadRetentionFailureMarkersAsync(
            artifactRoot, serializerOptions, cancellationToken).ConfigureAwait(false);

        List<RecoveryValidationEvidenceManifest> manifests = [];
        string manifestRoot = Path.Combine(artifactRoot, "live-recovery");
        if (Directory.Exists(manifestRoot))
        {
            const string ManifestSuffix = ".manifest.json";
            HashSet<string> reportPaths = Directory.EnumerateFiles(manifestRoot, "*.report.json", SearchOption.AllDirectories)
                .ToHashSet(StringComparer.Ordinal);
            foreach (string path in Directory.EnumerateFiles(manifestRoot, $"*{ManifestSuffix}", SearchOption.AllDirectories))
            {
                RecoveryValidationEvidenceManifest manifest = await ReadRetainedJsonAsync<RecoveryValidationEvidenceManifest>(
                    path, "manifest", serializerOptions, cancellationToken).ConfigureAwait(false);
                string reportPath = $"{path[..^ManifestSuffix.Length]}.report.json";
                using JsonDocument report = await ReadRetainedJsonAsync<JsonDocument>(
                    reportPath, "report", serializerOptions, cancellationToken,
                    manifest.ReportSha256 ?? string.Empty).ConfigureAwait(false);
                if (!MatchesReport(manifest, report.RootElement))
                {
                    throw new InvalidDataException("A retained recovery report does not match its manifest.");
                }

                _ = reportPaths.Remove(reportPath);
                manifests.Add(manifest);
            }

            if (reportPaths.Count != 0)
            {
                throw new InvalidDataException("A retained recovery report is missing its manifest.");
            }
        }

        LiveRecoveryValidationEvidenceAttempt attempt = new(
            summary.Enabled,
            summary.RunId,
            summary.StartedAtUtc,
            summary.CompletedAtUtc,
            summary.LatestAttemptCompletedSuccessfully,
            manifests,
            markers,
            summary.AlertsDeliveredByJob);
        return LiveRecoveryValidationEvidenceGate.Evaluate(attempt, policy, DateTimeOffset.UtcNow);
    }

    private static bool MatchesReport(RecoveryValidationEvidenceManifest manifest, JsonElement report)
    {
        string scenarioProperty = manifest.JobId switch
        {
            LiveRecoveryValidationJobs.ProjectionRebuild => "datasetRef",
            LiveRecoveryValidationJobs.ScopedOutage => "dependency",
            _ => "scenario",
        };
        if (!MatchesString(report, "correlationId", manifest.RunId) ||
            !MatchesString(report, "tenantRef", manifest.TenantRef) ||
            !MatchesString(report, scenarioProperty, manifest.Scenario) ||
            !MatchesString(report, "verdict", manifest.Verdict) ||
            !MatchesString(report, "reasonCode", manifest.ReasonCode) ||
            !MatchesInstant(report, "startedAtUtc", manifest.StartedAtUtc) ||
            !MatchesInstant(report, "endedAtUtc", manifest.EndedAtUtc))
        {
            return false;
        }

        JsonElement cleanup = manifest.JobId == LiveRecoveryValidationJobs.ControlledLossPath
            ? report : report.TryGetProperty("executionAssertions", out JsonElement execution) ? execution : default;
        bool cleanupComplete = cleanup.ValueKind == JsonValueKind.Object &&
            cleanup.TryGetProperty("cleanupComplete", out JsonElement complete) && complete.ValueKind == JsonValueKind.True;
        if (manifest.Assertions is null || !manifest.Assertions.TryGetValue("cleanup-complete", out bool retainedCleanup) ||
            retainedCleanup != cleanupComplete)
        {
            return false;
        }

        return manifest.JobId switch
        {
            LiveRecoveryValidationJobs.Continuity => MatchesSeconds(report, manifest, "measuredRpo", "rpo") &&
                MatchesSeconds(report, manifest, "measuredRto", "rto"),
            LiveRecoveryValidationJobs.ControlledLossPath => MatchesSeconds(report, manifest, "measuredRpo", "rpo"),
            LiveRecoveryValidationJobs.ProjectionRebuild => MatchesSeconds(report, manifest, "measuredRebuildDuration", "rebuild-duration"),
            LiveRecoveryValidationJobs.ScopedOutage => MatchesSeconds(report, manifest, "scopeRecordingLatency", "scope-recording-latency"),
            _ => false,
        };
    }

    private static bool MatchesInstant(JsonElement report, string name, DateTimeOffset expected)
        => report.TryGetProperty(name, out JsonElement property) && property.ValueKind == JsonValueKind.String &&
            property.TryGetDateTimeOffset(out DateTimeOffset actual) && actual == expected;

    private static bool MatchesSeconds(JsonElement report, RecoveryValidationEvidenceManifest manifest, string name, string measurement)
        => manifest.MeasurementsSeconds is not null && manifest.MeasurementsSeconds.TryGetValue(measurement, out double expected) &&
            report.TryGetProperty(name, out JsonElement property) && property.ValueKind == JsonValueKind.String &&
            TimeSpan.TryParseExact(property.GetString(), "c", CultureInfo.InvariantCulture, out TimeSpan actual) &&
            actual.TotalSeconds.Equals(expected);

    private static bool MatchesString(JsonElement report, string name, string expected)
        => report.ValueKind == JsonValueKind.Object &&
            report.TryGetProperty(name, out JsonElement property) &&
            property.ValueKind == JsonValueKind.String &&
            string.Equals(property.GetString(), expected, StringComparison.Ordinal);

    private static async Task<T> ReadRetainedJsonAsync<T>(
        string path, string evidenceKind, JsonSerializerOptions serializerOptions, CancellationToken cancellationToken,
        string? expectedSha256 = null)
        where T : class
    {
        try
        {
            using FileStream stream = File.OpenRead(path);
            if (stream.Length > MaximumRetainedFileBytes)
            {
                throw new InvalidDataException($"The retained recovery {evidenceKind} exceeds its size limit.");
            }

            byte[] bytes = new byte[(int)stream.Length];
            await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
            if (stream.ReadByte() != -1)
            {
                throw new InvalidDataException($"The retained recovery {evidenceKind} changed while being read.");
            }

            T document = JsonSerializer.Deserialize<T>(bytes, serializerOptions)
                ?? throw new InvalidDataException($"The retained recovery {evidenceKind} is empty.");
            if (expectedSha256 is not null && !string.Equals(
                Convert.ToHexStringLower(SHA256.HashData(bytes)), expectedSha256, StringComparison.Ordinal))
            {
                (document as IDisposable)?.Dispose();
                throw new InvalidDataException($"The retained recovery {evidenceKind} does not match its manifest.");
            }

            return document;
        }
        catch (JsonException)
        {
            throw new InvalidDataException($"The retained recovery {evidenceKind} is malformed.");
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new InvalidDataException($"The retained recovery {evidenceKind} is missing.");
        }
    }

    private static string ReadGateSetting(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{name} is required for recovery evidence replay.")
            : value;
    }

    private static int ReadGateNumber(string name)
        => int.TryParse(ReadGateSetting(name), NumberStyles.None, CultureInfo.InvariantCulture, out int value) && value > 0
            ? value
            : throw new InvalidOperationException($"{name} must be a positive integer for recovery evidence replay.");

    [Fact]
    public async Task RetentionFailureMarkersAreDiscoveredWithoutAnyManifest()
    {
        string artifactRoot = Path.Combine(Path.GetTempPath(), $"recovery-replay-{Guid.NewGuid():N}");
        string markerRoot = Path.Combine(artifactRoot, "retention-failures");
        Directory.CreateDirectory(markerRoot);
        RecoveryValidationEvidenceRetentionFailureMarker marker =
            RecoveryValidationEvidenceRetentionFailureMarker.Create(
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                LiveRecoveryValidationJobs.ProjectionRebuild,
                RecoveryValidationEvidenceRetentionFailureMarker.ProjectionRebuildScenario,
                DateTimeOffset.Parse(
                    "2026-08-27T12:00:00Z",
                    CultureInfo.InvariantCulture));
        await File.WriteAllTextAsync(
            Path.Combine(markerRoot, "projection-rebuild-projection-rebuild.retention-failure.json"),
            JsonSerializer.Serialize(marker, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        IReadOnlyList<RecoveryValidationEvidenceRetentionFailureMarker?> retained =
            await LoadRetentionFailureMarkersAsync(
                artifactRoot,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        retained.ShouldHaveSingleItem().ShouldBe(marker);
        Directory.GetFiles(artifactRoot, "*.manifest.json", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public async Task ReplayLoaderOnlyAcceptsBoundedClosedJsonFromDesignatedDirectory()
    {
        string artifactRoot = Path.Combine(Path.GetTempPath(), $"recovery-replay-bounds-{Guid.NewGuid():N}");
        string markerRoot = Path.Combine(artifactRoot, RetentionFailuresDirectoryName);
        Directory.CreateDirectory(markerRoot);
        RecoveryValidationEvidenceRetentionFailureMarker marker =
            RecoveryValidationEvidenceRetentionFailureMarker.Create(
                "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                LiveRecoveryValidationJobs.ProjectionRebuild,
                RecoveryValidationEvidenceRetentionFailureMarker.ProjectionRebuildScenario,
                DateTimeOffset.Parse("2026-08-27T12:00:00Z", CultureInfo.InvariantCulture));
        string validJson = JsonSerializer.Serialize(marker, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await File.WriteAllTextAsync(
            Path.Combine(artifactRoot, "misplaced.retention-failure.json"),
            validJson,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(
            Path.Combine(markerRoot, "oversized.retention-failure.json"),
            new string('x', RecoveryValidationEvidenceRetentionFailureMarker.MaximumSerializedBytes + 1),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(
            Path.Combine(markerRoot, "unmapped.retention-failure.json"),
            validJson[..^1] + ",\"unexpected\":true}",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        IReadOnlyList<RecoveryValidationEvidenceRetentionFailureMarker?> retained =
            await LoadRetentionFailureMarkersAsync(
                artifactRoot,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        retained.Count.ShouldBe(2);
        retained.ShouldAllBe(static candidate => candidate == null);
    }

    [Fact]
    public async Task ReplayLoaderPreservesCancellationAndMapsAFileRaceToInvalid()
    {
        string missingMarker = Path.Combine(
            Path.GetTempPath(),
            $"missing-retention-marker-{Guid.NewGuid():N}.retention-failure.json");
        RecoveryValidationEvidenceRetentionFailureMarker? raced = await LoadRetentionFailureMarkerAsync(
            missingMarker,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            CancellationToken.None).ConfigureAwait(true);
        raced.ShouldBeNull();

        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();
        _ = await Should.ThrowAsync<OperationCanceledException>(async () =>
            await LoadRetentionFailureMarkerAsync(
                missingMarker,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                cancelled.Token).ConfigureAwait(true));
    }

    [Fact]
    public async Task WorkflowShapedArtifactReplaysAllFourRetentionFailuresWithoutManifests()
    {
        string root = Path.Combine(Path.GetTempPath(), $"recovery-workflow-artifact-{Guid.NewGuid():N}");
        string canonicalEvidenceRoot = Path.Combine(root, "canonical-evidence");
        string producerSideChannelRoot = Path.Combine(root, "runner-temp", "live-recovery-retention-failures");
        string artifactRoot = Path.Combine(root, "uploaded-TestResults");
        string uploadedSideChannelRoot = Path.Combine(artifactRoot, RetentionFailuresDirectoryName);
        string runId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(3);
        DateTimeOffset failedAtUtc = startedAtUtc + TimeSpan.FromMinutes(1);
        DateTimeOffset completedAtUtc = startedAtUtc + TimeSpan.FromMinutes(2);
        FileRecoveryValidationEvidenceRetentionFailureSink sink = new(
            producerSideChannelRoot,
            canonicalEvidenceRoot);
        foreach (RecoveryValidationEvidenceRetentionFailureMarker marker in new[]
        {
            RecoveryValidationEvidenceRetentionFailureMarker.Create(
                runId,
                LiveRecoveryValidationJobs.Continuity,
                ContinuityDrillScenarios.EventStoreOutage,
                failedAtUtc),
            RecoveryValidationEvidenceRetentionFailureMarker.Create(
                runId,
                LiveRecoveryValidationJobs.ControlledLossPath,
                ControlledLossPathReport.SubscriptionNotificationRejectionScenario,
                failedAtUtc),
            RecoveryValidationEvidenceRetentionFailureMarker.Create(
                runId,
                LiveRecoveryValidationJobs.ProjectionRebuild,
                RecoveryValidationEvidenceRetentionFailureMarker.ProjectionRebuildScenario,
                failedAtUtc),
            RecoveryValidationEvidenceRetentionFailureMarker.Create(
                runId,
                LiveRecoveryValidationJobs.ScopedOutage,
                ScopedOutageDependencies.Graph,
                failedAtUtc),
        })
        {
            await sink.RecordAsync(marker, TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        LiveRecoveryValidationAttemptSummary summary = new()
        {
            Enabled = true,
            RunId = runId,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            LatestAttemptCompletedSuccessfully = true,
            AlertsDeliveredByJob = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [LiveRecoveryValidationJobs.Continuity] = 0,
                [LiveRecoveryValidationJobs.ControlledLossPath] = 0,
                [LiveRecoveryValidationJobs.ProjectionRebuild] = 0,
                [LiveRecoveryValidationJobs.ScopedOutage] = 0,
            },
        };
        await File.WriteAllTextAsync(
            Path.Combine(producerSideChannelRoot, LiveRecoveryValidationAttemptSummary.FileName),
            JsonSerializer.Serialize(summary, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Simulates the workflows' always-run `cp -R "$RETENTION_FAILURE_ROOT"/. TestResults/retention-failures/`.
        Directory.CreateDirectory(uploadedSideChannelRoot);
        foreach (string source in Directory.GetFiles(producerSideChannelRoot, "*", SearchOption.TopDirectoryOnly))
        {
            File.Copy(source, Path.Combine(uploadedSideChannelRoot, Path.GetFileName(source)));
        }

        LiveRecoveryValidationEvidenceGateDecision decision = await ReplayRetainedArtifactAsync(
            artifactRoot, ReplayPolicy(),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        foreach (string jobId in LiveRecoveryValidationJobs.All)
        {
            decision.StopShipReasons.ShouldContain($"{jobId}:evidence_retention_failed");
            decision.StopShipReasons.ShouldNotContain($"{jobId}:missing_evidence");
            if (string.Equals(jobId, LiveRecoveryValidationJobs.ControlledLossPath, StringComparison.Ordinal))
            {
                decision.StopShipReasons.ShouldNotContain($"{jobId}:unalerted_breach");
            }
            else
            {
                decision.StopShipReasons.ShouldContain($"{jobId}:unalerted_breach");
            }
        }

        Directory.GetFiles(artifactRoot, "*.manifest.json", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public void ReplayRejectsControlledLossEvidenceFromAnotherCommit()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-28T12:00:00Z", CultureInfo.InvariantCulture);
        LiveRecoveryValidationEvidenceGateDecision decision = ReplayDecision(
            [ReplayControlledLossManifest(now)],
            now,
            requiredCommit: "2493ff8f2f7e031bc386a2d379d95649744fe7ee");

        decision.StopShipReasons.ShouldContain(
            $"{LiveRecoveryValidationJobs.ControlledLossPath}:repository_commit_unexpected");
    }

    [Fact]
    public void ReplayRejectsStaleControlledLossEvidence()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-28T12:00:00Z", CultureInfo.InvariantCulture);
        RecoveryValidationEvidenceManifest stale = ReplayControlledLossManifest(now) with
        {
            StartedAtUtc = now - TimeSpan.FromDays(3),
            EndedAtUtc = now - TimeSpan.FromDays(2),
            PreFaultCommittedAtUtc = now - TimeSpan.FromDays(3) + TimeSpan.FromSeconds(10),
            RejectedAtUtc = now - TimeSpan.FromDays(3) + TimeSpan.FromSeconds(20),
            PostRecoveryCommittedAtUtc = now - TimeSpan.FromDays(3) + TimeSpan.FromSeconds(30),
        };

        LiveRecoveryValidationEvidenceGateDecision decision = ReplayDecision([stale], now);

        decision.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.ControlledLossPath}:manifest_stale");
    }

    [Fact]
    public void ReplayRejectsDuplicateControlledLossEvidence()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-28T12:00:00Z", CultureInfo.InvariantCulture);
        RecoveryValidationEvidenceManifest manifest = ReplayControlledLossManifest(now);

        LiveRecoveryValidationEvidenceGateDecision decision = ReplayDecision(
            [manifest, manifest with { ScenarioId = "01ARZ3NDEKTSV4RRFFQ69G5FAF" }],
            now);

        decision.StopShipReasons.ShouldContain(
            $"{LiveRecoveryValidationJobs.ControlledLossPath}:incomplete_scenario_set");
    }

    [Fact]
    public void ReplayRejectsCorruptRetentionEvidence()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-28T12:00:00Z", CultureInfo.InvariantCulture);
        LiveRecoveryValidationEvidenceAttempt attempt = ReplayAttempt([ReplayControlledLossManifest(now)], now) with
        {
            RetentionFailureMarkers = [null],
        };

        LiveRecoveryValidationEvidenceGateDecision decision = LiveRecoveryValidationEvidenceGate.Evaluate(
            attempt,
            ReplayPolicy(),
            now);

        decision.StopShipReasons.ShouldContain("retention_failure_marker_invalid");
    }

    internal static ValueTask<IReadOnlyList<RecoveryValidationEvidenceRetentionFailureMarker?>>
        LoadRetentionFailureMarkersAsync(
            string artifactRoot,
            JsonSerializerOptions serializerOptions,
            CancellationToken cancellationToken)
        => LoadRetentionFailureMarkersFromDirectoryAsync(
            Path.Combine(artifactRoot, RetentionFailuresDirectoryName),
            serializerOptions,
            cancellationToken);

    /// <summary>
    /// Loads markers from one designated marker root. The live producer and this out-of-process gate share this single
    /// bounded loader so identical bytes cannot be judged differently on either side of the artifact boundary.
    /// </summary>
    internal static async ValueTask<IReadOnlyList<RecoveryValidationEvidenceRetentionFailureMarker?>>
        LoadRetentionFailureMarkersFromDirectoryAsync(
            string markerRoot,
            JsonSerializerOptions serializerOptions,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<RecoveryValidationEvidenceRetentionFailureMarker?> retentionFailureMarkers = [];
        if (!Directory.Exists(markerRoot))
        {
            return retentionFailureMarkers;
        }

        string[] markerFiles;
        try
        {
            markerFiles = Directory.GetFiles(
                markerRoot,
                "*.retention-failure.json",
                SearchOption.TopDirectoryOnly);
        }
        catch (IOException)
        {
            return [null];
        }
        catch (UnauthorizedAccessException)
        {
            return [null];
        }

        foreach (string markerFile in markerFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            retentionFailureMarkers.Add(await LoadRetentionFailureMarkerAsync(
                markerFile,
                serializerOptions,
                cancellationToken).ConfigureAwait(false));
        }

        return retentionFailureMarkers;
    }

    private static async ValueTask<RecoveryValidationEvidenceRetentionFailureMarker?>
        LoadRetentionFailureMarkerAsync(
            string markerFile,
            JsonSerializerOptions serializerOptions,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (new FileInfo(markerFile).Length >
                RecoveryValidationEvidenceRetentionFailureMarker.MaximumSerializedBytes)
            {
                return null;
            }

            JsonSerializerOptions boundedOptions = new(serializerOptions)
            {
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            };
            using FileStream markerStream = File.OpenRead(markerFile);
            return await JsonSerializer
                .DeserializeAsync<RecoveryValidationEvidenceRetentionFailureMarker>(
                    markerStream,
                    boundedOptions,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static LiveRecoveryValidationEvidenceGateDecision ReplayDecision(
        IReadOnlyList<RecoveryValidationEvidenceManifest> manifests,
        DateTimeOffset now,
        string? requiredCommit = null)
        => LiveRecoveryValidationEvidenceGate.Evaluate(
            ReplayAttempt(manifests, now),
            ReplayPolicy(requiredCommit),
            now);

    private static LiveRecoveryValidationEvidenceAttempt ReplayAttempt(
        IReadOnlyList<RecoveryValidationEvidenceManifest> manifests,
        DateTimeOffset now)
        => new(
            Enabled: true,
            RunId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            StartedAtUtc: now - TimeSpan.FromMinutes(3),
            CompletedAtUtc: now - TimeSpan.FromMinutes(1),
            LatestAttemptCompletedSuccessfully: true,
            Evidence: manifests,
            RetentionFailureMarkers: [],
            AlertsDeliveredByJob: new Dictionary<string, int>(StringComparer.Ordinal));

    private static LiveRecoveryValidationGatePolicy ReplayPolicy(string? requiredCommit = null)
        => new(
            ConfiguredProjectionDatasets: ["recovery-baseline"],
            TargetDeviationsBlockRelease: true,
            RequiredDriverMode: RecoveryValidationEvidenceManifest.LiveDriverMode,
            MaximumEvidenceAge: TimeSpan.FromDays(1),
            ExpectedDatasetVersion: "v1",
            MinimumDatasetVolume: 6,
            RequiredRepositoryCommit: requiredCommit,
            MaximumMeasurableRecoveryCeilingSeconds: 180);

    private static RecoveryValidationEvidenceManifest ReplayControlledLossManifest(DateTimeOffset now)
        => new()
        {
            RunId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ScenarioId = "01ARZ3NDEKTSV4RRFFQ69G5FAE",
            StartedAtUtc = now - TimeSpan.FromMinutes(2),
            EndedAtUtc = now - TimeSpan.FromMinutes(1),
            RepositoryCommit = "1493ff8f2f7e031bc386a2d379d95649744fe7ee",
            AppHostVersion = "chatbot-apphost-v1",
            AspireVersion = "13.4.6",
            DaprVersion = "1.18.4",
            TopologyVersion = "aspire-single-replica-recovery-v1",
            ConfigurationVersion = "live-recovery-v1",
            TenantRef = "replay-test:recovery-validation",
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            ConfiguredDatasetVolume = 6,
            DatasetVolume = 0,
            DriverMode = RecoveryValidationEvidenceManifest.LiveDriverMode,
            JobId = LiveRecoveryValidationJobs.ControlledLossPath,
            Scenario = ControlledLossPathReport.SubscriptionNotificationRejectionScenario,
            // Exactly what FileRecoveryValidationEvidenceSink writes for this job. A replay fixture that invents its
            // own action vocabulary replays a shape the producer never emits.
            InjectedFaultAction = "reject:subscription-notification",
            RestoreAction = "restore:graph-subscription",
            CleanupAction = "cleanup:subscription-notification-rejection",
            ExpectedScope = "tenant",
            ObservedScope = "tenant",
            ReportKind = LiveRecoveryValidationJobs.ControlledLossPath,
            Verdict = ControlledLossPathVerdicts.Met,
            ReasonCode = ControlledLossPathReport.CompletedReasonCode,
            MeasurementsSeconds = new Dictionary<string, double> { ["rpo"] = 20 },
            AllowedTargetsSeconds = new Dictionary<string, double> { ["rpo"] = 900 },
            Assertions = LiveRecoveryValidationEvidenceGate.RequiredAssertionsFor(
                    LiveRecoveryValidationJobs.ControlledLossPath)
                .ToDictionary(static assertion => assertion, static _ => true, StringComparer.Ordinal),
            Coverage = new Dictionary<string, int> { ["scenario"] = 1 },
            Deviations = [],
            ResidualIds = ["RV-EXT-M365", "RV-DURABLE-WORM", "RV-PROD-CONTROL", "RV-PROVIDER-SCALE", "RV-EVIDENCE-KINDS"],
            ArtifactLocators = new Dictionary<string, string>
            {
                ["test-output"] = "artifact:live-recovery-validation-evidence/results.trx",
                ["reports"] = "artifact:live-recovery-validation-evidence/reports",
            },
            MeasurableRecoveryCeilingSeconds = 180,
            PreFaultRetainedRef = "01ARZ3NDEKTSV4RRFFQ69G5FAA",
            PreFaultEventRef = "01ARZ3NDEKTSV4RRFFQ69G5FAB",
            PreFaultSequence = 1,
            PreFaultCommittedAtUtc = now - TimeSpan.FromSeconds(100),
            RejectedCandidateRef = "01ARZ3NDEKTSV4RRFFQ69G5FAC",
            RejectedAtUtc = now - TimeSpan.FromSeconds(90),
            PostRecoveryRetainedRef = "01ARZ3NDEKTSV4RRFFQ69G5FAD",
            PostRecoveryEventRef = "01ARZ3NDEKTSV4RRFFQ69G5FAF",
            PostRecoverySequence = 1,
            PostRecoveryCommittedAtUtc = now - TimeSpan.FromSeconds(80),
        };
}
