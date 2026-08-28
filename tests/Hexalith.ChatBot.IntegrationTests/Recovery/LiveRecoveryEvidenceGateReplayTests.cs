using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Evaluates the release evidence gate <b>out of process</b>, against the uploaded artifact only.
/// <para>
/// This is the authoritative gate. The live lane's own in-process evaluation runs inside the test that produced the
/// evidence, from an attempt object that test builds — a run grading its own homework. This class runs in a separate
/// workflow job, reads the retained manifests and the run's observation summary from the downloaded artifact, applies
/// the thresholds the <b>release path</b> configures, and evaluates against a fresh wall clock so the staleness and
/// freshness branches are genuinely reachable.
/// </para>
/// </summary>
public sealed class LiveRecoveryEvidenceGateReplayTests
{
    private const string RetentionFailuresDirectoryName = "retention-failures";
    private const string EvidenceDirectoryVariable = "HEXALITH_CHATBOT_RECOVERY_EVIDENCE_DIR";
    private const string RequiredVariable = "HEXALITH_CHATBOT_RECOVERY_EVIDENCE_REQUIRED";
    private const string ExpectedDatasetsVariable = "HEXALITH_CHATBOT_RECOVERY_EXPECTED_DATASETS";
    private const string MaximumEvidenceAgeHoursVariable = "HEXALITH_CHATBOT_RECOVERY_MAX_EVIDENCE_AGE_HOURS";
    private const string ExpectedDatasetVersionVariable = "HEXALITH_CHATBOT_RECOVERY_EXPECTED_DATASET_VERSION";
    private const string MinimumDatasetVolumeVariable = "HEXALITH_CHATBOT_RECOVERY_MINIMUM_DATASET_VOLUME";
    private const string RequiredCommitVariable = "HEXALITH_CHATBOT_RECOVERY_REQUIRED_COMMIT";
    private const string MaximumMeasurableCeilingSecondsVariable = "HEXALITH_CHATBOT_RECOVERY_MAX_MEASURABLE_CEILING_SECONDS";

    [Fact]
    public async Task RetainedLiveRecoveryEvidenceShouldPassTheReleaseGateOutOfProcess()
    {
        string? evidenceDirectory = Environment.GetEnvironmentVariable(EvidenceDirectoryVariable);
        bool required = string.Equals(
            Environment.GetEnvironmentVariable(RequiredVariable),
            "1",
            StringComparison.Ordinal);
        if (required && string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            throw new InvalidOperationException(
                $"The required release evidence gate has no {EvidenceDirectoryVariable} to evaluate.");
        }

        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(evidenceDirectory),
            $"Set {EvidenceDirectoryVariable} to a retained live-recovery evidence directory to replay the release gate.");

        Directory.Exists(evidenceDirectory)
            .ShouldBeTrue($"The retained evidence directory '{evidenceDirectory}' does not exist.");

        JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);

        // Discover only the designated uploaded side-channel directory before making any manifest assumption. A total
        // evidence sink loss is precisely the case in which the normal evidence directory may contain no manifest.
        IReadOnlyList<RecoveryValidationEvidenceRetentionFailureMarker?> retentionFailureMarkers =
            await LoadRetentionFailureMarkersAsync(
                evidenceDirectory!,
                serializerOptions,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        // The attempt summary is written into the same independent root and staged exactly once. Do not accept a
        // summary found elsewhere: canonical-directory loss must not be hidden by a broader recursive search.
        // Never enumerate an absent directory: `actions/upload-artifact` drops an empty staged root, so a producer that
        // died before writing its summary yields no `retention-failures` subtree at all. Throwing here would report a
        // DirectoryNotFoundException stack trace instead of the gate's own reason, which is exactly what this
        // out-of-process gate exists to avoid.
        string retentionFailureDirectory = Path.Combine(evidenceDirectory!, RetentionFailuresDirectoryName);
        string[] summaryPaths = Directory.Exists(retentionFailureDirectory)
            ? Directory.GetFiles(
                retentionFailureDirectory,
                LiveRecoveryValidationAttemptSummary.FileName,
                SearchOption.TopDirectoryOnly)
            : [];
        summaryPaths.Length.ShouldBe(
            1,
            $"Expected exactly one {LiveRecoveryValidationAttemptSummary.FileName} under "
            + $"'{retentionFailureDirectory}'.");
        string summaryPath = summaryPaths[0];

        LiveRecoveryValidationAttemptSummary summary;
        await using (FileStream summaryStream = File.OpenRead(summaryPath))
        {
            summary = (await JsonSerializer
                .DeserializeAsync<LiveRecoveryValidationAttemptSummary>(
                    summaryStream,
                    serializerOptions,
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true)).ShouldNotBeNull();
        }

        List<RecoveryValidationEvidenceManifest> manifests = [];
        foreach (string manifestFile in Directory.GetFiles(evidenceDirectory!, "*.manifest.json", SearchOption.AllDirectories))
        {
            await using FileStream stream = File.OpenRead(manifestFile);
            RecoveryValidationEvidenceManifest? manifest = await JsonSerializer
                .DeserializeAsync<RecoveryValidationEvidenceManifest>(
                    stream,
                    serializerOptions,
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            manifests.Add(manifest.ShouldNotBeNull());
        }

        LiveRecoveryValidationEvidenceAttempt attempt = new(
            summary.Enabled,
            summary.RunId,
            summary.StartedAtUtc,
            summary.CompletedAtUtc,
            summary.LatestAttemptCompletedSuccessfully,
            manifests,
            retentionFailureMarkers,
            summary.AlertsDeliveredByJob);

        LiveRecoveryValidationGatePolicy policy = required
            ? LiveRecoveryValidationGatePolicy.ForRelease(
                ExpectedDatasets(required),
                targetDeviationsBlockRelease: true,
                RecoveryValidationEvidenceManifest.LiveDriverMode,
                MaximumEvidenceAge(),
                expectedDatasetVersion: RequiredValue(ExpectedDatasetVersionVariable),
                minimumDatasetVolume: (int)PositiveNumber(MinimumDatasetVolumeVariable, unset: null),
                maximumMeasurableRecoveryCeilingSeconds: PositiveNumber(MaximumMeasurableCeilingSecondsVariable, unset: null),
                requiredRepositoryCommit: RequiredValue(RequiredCommitVariable))
            : new LiveRecoveryValidationGatePolicy(
                ExpectedDatasets(required),
                TargetDeviationsBlockRelease: true,
                RecoveryValidationEvidenceManifest.LiveDriverMode,
                MaximumEvidenceAge(),
                ExpectedDatasetVersion: Optional(ExpectedDatasetVersionVariable),
                MinimumDatasetVolume: (int)PositiveNumber(MinimumDatasetVolumeVariable, unset: 0),
                RequiredRepositoryCommit: Optional(RequiredCommitVariable),
                MaximumMeasurableRecoveryCeilingSeconds: PositiveNumber(MaximumMeasurableCeilingSecondsVariable, unset: 0));

        LiveRecoveryValidationEvidenceGateDecision decision = LiveRecoveryValidationEvidenceGate.Evaluate(
            attempt,
            policy,
            DateTimeOffset.UtcNow);

        decision.IsStopShip.ShouldBeFalse(
            $"The release evidence gate rejected the retained evidence: {string.Join(", ", decision.StopShipReasons)}.");

        // Not failures. They are the limits on what this pass may be cited as evidence for, and they must be visible in
        // the job log rather than inferred by a reader of the manifest.
        foreach (string limitation in decision.ClaimLimitationReasons)
        {
            TestContext.Current.TestOutputHelper?.WriteLine($"claim limitation: {limitation}");
        }
    }

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
        DateTimeOffset startedAtUtc = DateTimeOffset.Parse("2026-08-27T12:00:00Z", CultureInfo.InvariantCulture);
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

        JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web);
        IReadOnlyList<RecoveryValidationEvidenceRetentionFailureMarker?> retainedMarkers =
            await LoadRetentionFailureMarkersAsync(
                artifactRoot,
                serializerOptions,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        string retainedSummaryPath = Directory.GetFiles(
            uploadedSideChannelRoot,
            LiveRecoveryValidationAttemptSummary.FileName,
            SearchOption.TopDirectoryOnly).ShouldHaveSingleItem();
        await using FileStream summaryStream = File.OpenRead(retainedSummaryPath);
        LiveRecoveryValidationAttemptSummary retainedSummary = (await JsonSerializer
            .DeserializeAsync<LiveRecoveryValidationAttemptSummary>(
                summaryStream,
                serializerOptions,
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();

        LiveRecoveryValidationEvidenceGateDecision decision = LiveRecoveryValidationEvidenceGate.Evaluate(
            new LiveRecoveryValidationEvidenceAttempt(
                retainedSummary.Enabled,
                retainedSummary.RunId,
                retainedSummary.StartedAtUtc,
                retainedSummary.CompletedAtUtc,
                retainedSummary.LatestAttemptCompletedSuccessfully,
                Evidence: [],
                RetentionFailureMarkers: retainedMarkers,
                AlertsDeliveredByJob: retainedSummary.AlertsDeliveredByJob),
            new LiveRecoveryValidationGatePolicy(
                ConfiguredProjectionDatasets: ["recovery-baseline"],
                TargetDeviationsBlockRelease: true,
                RequiredDriverMode: RecoveryValidationEvidenceManifest.LiveDriverMode,
                MaximumEvidenceAge: TimeSpan.FromDays(8)),
            completedAtUtc + TimeSpan.FromMinutes(1));

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

    private static IReadOnlyList<string> ExpectedDatasets(bool required)
    {
        string? raw = Environment.GetEnvironmentVariable(ExpectedDatasetsVariable);
        string configured = string.IsNullOrWhiteSpace(raw) ? "recovery-baseline" : raw.Trim();
        string[] datasets = [.. configured.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        if (datasets.Length == 0)
        {
            if (required)
            {
                throw new InvalidOperationException(
                    $"{ExpectedDatasetsVariable} resolved to an empty dataset list for a required gate run.");
            }

            return ["recovery-baseline"];
        }

        return datasets;
    }

    /// <summary>
    /// Reads the configured maximum evidence age. A malformed value <b>throws</b> rather than falling back: silently
    /// applying the 8-day default meant a typo in a tightened setting loosened the required gate's staleness bound with
    /// no signal anywhere.
    /// </summary>
    private static TimeSpan MaximumEvidenceAge()
    {
        string? configured = Environment.GetEnvironmentVariable(MaximumEvidenceAgeHoursVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return TimeSpan.FromDays(8);
        }

        return double.TryParse(configured, NumberStyles.Float, CultureInfo.InvariantCulture, out double hours) && hours > 0
            ? TimeSpan.FromHours(hours)
            : throw new InvalidOperationException(
                $"{MaximumEvidenceAgeHoursVariable} is set to '{configured}', which is not a positive number of hours.");
    }

    /// <summary>Returns a configured value, or <see langword="null"/> when the release path leaves it unpinned.</summary>
    private static string? Optional(string variable)
    {
        string? configured = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(configured) ? null : configured.Trim();
    }

    /// <summary>
    /// Reads a variable that a REQUIRED gate run must pin. Fails closed rather than calling
    /// <see cref="LiveRecoveryValidationGatePolicy.ForRelease"/> with a <see langword="null"/> anchor: an unset
    /// required env var is a broken required-gate invocation, not a legitimate "leave it unpinned" choice.
    /// </summary>
    private static string RequiredValue(string variable)
    {
        string? configured = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(configured)
            ? throw new InvalidOperationException($"{variable} must be set when the release evidence gate is required.")
            : configured.Trim();
    }

    /// <summary>
    /// Reads a positive numeric policy input. When <paramref name="unset"/> is <see langword="null"/>, an unset
    /// variable fails closed (required gate runs). When unset is a number, that value is used for optional local runs.
    /// </summary>
    private static double PositiveNumber(string variable, double? unset)
    {
        string? configured = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return unset ?? throw new InvalidOperationException(
                $"{variable} must be a positive number when the release evidence gate is required.");
        }

        return double.TryParse(configured, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && value > 0
            ? value
            : throw new InvalidOperationException($"{variable} is set to '{configured}', which is not a positive number.");
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
            ResidualIds = ["RV-PROD-CONTROL"],
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
