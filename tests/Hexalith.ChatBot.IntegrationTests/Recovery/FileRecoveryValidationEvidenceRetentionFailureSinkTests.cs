using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

public sealed class FileRecoveryValidationEvidenceRetentionFailureSinkTests
{
    private const string RunId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public async Task RecordAsyncWritesOneBoundedContainedMetadataOnlyMarker()
    {
        string root = CreateRoot();
        string markerDirectory = Path.Combine(root, "runner-temp", "retention-failures");
        string evidenceDirectory = Path.Combine(root, "workspace", "TestResults", "live-recovery");
        FileRecoveryValidationEvidenceRetentionFailureSink sink = new(markerDirectory, evidenceDirectory);
        RecoveryValidationEvidenceRetentionFailureMarker marker = Marker(
            LiveRecoveryValidationJobs.Continuity,
            ContinuityDrillScenarios.EventStoreOutage,
            DateTimeOffset.Parse("2026-08-27T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));

        await sink.RecordAsync(marker, TestContext.Current.CancellationToken).ConfigureAwait(true);

        string markerPath = Directory.GetFiles(markerDirectory, "*.retention-failure.json").ShouldHaveSingleItem();
        Path.GetFileName(markerPath).ShouldBe("continuity-eventstore-outage.retention-failure.json");
        Path.GetFullPath(markerPath).StartsWith(
            Path.GetFullPath(markerDirectory) + Path.DirectorySeparatorChar,
            StringComparison.Ordinal).ShouldBeTrue();
        Encoding.UTF8.GetByteCount(await File.ReadAllTextAsync(markerPath, TestContext.Current.CancellationToken))
            .ShouldBeLessThanOrEqualTo(RecoveryValidationEvidenceRetentionFailureMarker.MaximumSerializedBytes);

        await using FileStream stream = File.OpenRead(markerPath);
        RecoveryValidationEvidenceRetentionFailureMarker retained = (await JsonSerializer
            .DeserializeAsync<RecoveryValidationEvidenceRetentionFailureMarker>(
                stream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();
        retained.ShouldBe(marker);
        retained.IsValid().ShouldBeTrue();
        Directory.GetFiles(markerDirectory, "*.tmp", SearchOption.TopDirectoryOnly).ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordAsyncOverwritesRetryForTheSameClosedScenario()
    {
        string root = CreateRoot();
        string markerDirectory = Path.Combine(root, "runner-temp", "retention-failures");
        FileRecoveryValidationEvidenceRetentionFailureSink sink = new(
            markerDirectory,
            Path.Combine(root, "workspace", "evidence"));
        DateTimeOffset firstFailure = DateTimeOffset.Parse(
            "2026-08-27T12:00:00Z",
            System.Globalization.CultureInfo.InvariantCulture);

        await sink.RecordAsync(
            Marker(LiveRecoveryValidationJobs.ScopedOutage, ScopedOutageDependencies.Graph, firstFailure),
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sink.RecordAsync(
            Marker(
                LiveRecoveryValidationJobs.ScopedOutage,
                ScopedOutageDependencies.Graph,
                firstFailure.AddSeconds(1)),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        string markerPath = Directory.GetFiles(markerDirectory, "*.retention-failure.json").ShouldHaveSingleItem();
        await using FileStream stream = File.OpenRead(markerPath);
        RecoveryValidationEvidenceRetentionFailureMarker retained = (await JsonSerializer
            .DeserializeAsync<RecoveryValidationEvidenceRetentionFailureMarker>(
                stream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();
        retained.FailedAtUtc.ShouldBe(firstFailure.AddSeconds(1));
        Directory.GetFiles(markerDirectory, "*.tmp", SearchOption.TopDirectoryOnly).ShouldBeEmpty();
    }

    [Fact]
    public async Task CancelledReplacementPreservesThePreviouslyCompleteMarker()
    {
        string root = CreateRoot();
        string markerDirectory = Path.Combine(root, "runner-temp", "retention-failures");
        FileRecoveryValidationEvidenceRetentionFailureSink sink = new(
            markerDirectory,
            Path.Combine(root, "workspace", "evidence"));
        DateTimeOffset firstFailure = DateTimeOffset.Parse(
            "2026-08-27T12:00:00Z",
            System.Globalization.CultureInfo.InvariantCulture);
        RecoveryValidationEvidenceRetentionFailureMarker first = Marker(
            LiveRecoveryValidationJobs.ScopedOutage,
            ScopedOutageDependencies.Graph,
            firstFailure);
        await sink.RecordAsync(first, TestContext.Current.CancellationToken).ConfigureAwait(true);

        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();
        _ = await Should.ThrowAsync<OperationCanceledException>(async () =>
            await sink.RecordAsync(
                first with { FailedAtUtc = firstFailure.AddSeconds(1) },
                cancelled.Token).ConfigureAwait(true));

        string markerPath = Directory.GetFiles(markerDirectory, "*.retention-failure.json").ShouldHaveSingleItem();
        await using FileStream stream = File.OpenRead(markerPath);
        RecoveryValidationEvidenceRetentionFailureMarker retained = (await JsonSerializer
            .DeserializeAsync<RecoveryValidationEvidenceRetentionFailureMarker>(
                stream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true)).ShouldNotBeNull();
        retained.ShouldBe(first);
        Directory.GetFiles(markerDirectory, "*.tmp", SearchOption.TopDirectoryOnly).ShouldBeEmpty();
    }

    [Fact]
    public void ConstructorRejectsOverlappingOrRelativeRoots()
    {
        string root = CreateRoot();
        string evidenceDirectory = Path.Combine(root, "evidence");

        _ = Should.Throw<ArgumentException>(() =>
            new FileRecoveryValidationEvidenceRetentionFailureSink(
                Path.Combine(evidenceDirectory, "markers"),
                evidenceDirectory));
        _ = Should.Throw<ArgumentException>(() =>
            new FileRecoveryValidationEvidenceRetentionFailureSink("relative-markers", evidenceDirectory));
    }

    [Fact]
    public async Task RecordAsyncRejectsUnknownMetadataWithoutWritingAFile()
    {
        string root = CreateRoot();
        string markerDirectory = Path.Combine(root, "markers");
        FileRecoveryValidationEvidenceRetentionFailureSink sink = new(
            markerDirectory,
            Path.Combine(root, "evidence"));
        RecoveryValidationEvidenceRetentionFailureMarker invalid = Marker(
            LiveRecoveryValidationJobs.Continuity,
            ContinuityDrillScenarios.EventStoreOutage,
            DateTimeOffset.UtcNow) with
        {
            JobId = "unknown-job",
        };

        _ = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sink.RecordAsync(invalid, TestContext.Current.CancellationToken).ConfigureAwait(true));

        Directory.Exists(markerDirectory).ShouldBeFalse();
    }

    [Fact]
    public async Task CoordinatorSinkLossFlowsThroughTheRealFileSinkIntoAReplayedRetentionFailure()
    {
        // Every other test covers ONE hop: coordinator to a capturing fake, marker to disk, or a hand-built file to the
        // gate. This composes the real chain, so the load-bearing assumption that the coordinator's correlation id is
        // the canonical run ULID the gate matches against cannot silently break. If it were not a ULID, IsValid() would
        // reject the marker inside the sink, the recorder would swallow the failure, and the whole feature would
        // degrade -- invisibly -- back to ordinary missing_evidence.
        string root = CreateRoot();
        string markerDirectory = Path.Combine(root, "runner-temp", "live-recovery-retention-failures");
        string artifactRoot = Path.Combine(root, "uploaded-TestResults");
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse(
            "2026-08-27T12:00:00Z",
            System.Globalization.CultureInfo.InvariantCulture);
        FileRecoveryValidationEvidenceRetentionFailureSink markerSink = new(
            markerDirectory,
            Path.Combine(root, "workspace", "TestResults", "live-recovery"));
        ContinuityDrillCoordinator coordinator = new(
            new MeasuringRunner(failedAtUtc),
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            new FixedClock(failedAtUtc),
            new TotallyUnavailableEvidenceSink(),
            markerSink);

        ContinuityDrillReport report = await coordinator.RunScenarioAndRecordAsync(
            ContinuityDrillScenarios.EventStoreOutage,
            "replay-test:recovery-validation",
            RunId,
            TestContext.Current.CancellationToken);

        report.Verdict.ShouldBe(ContinuityDrillVerdicts.Unmeasurable);
        report.Deviations.ShouldContain(ContinuityDrillReport.EvidenceRetentionFailedDeviation);

        // Stage exactly as the workflows' always-run `cp -R "$RETENTION_FAILURE_ROOT"/. TestResults/retention-failures/`.
        string uploadedMarkerRoot = Path.Combine(artifactRoot, "retention-failures");
        Directory.CreateDirectory(uploadedMarkerRoot);
        foreach (string produced in Directory.GetFiles(markerDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            File.Copy(produced, Path.Combine(uploadedMarkerRoot, Path.GetFileName(produced)));
        }

        IReadOnlyList<RecoveryValidationEvidenceRetentionFailureMarker?> replayed =
            await LiveRecoveryEvidenceGateReplayTests.LoadRetentionFailureMarkersAsync(
                artifactRoot,
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                TestContext.Current.CancellationToken).ConfigureAwait(true);

        LiveRecoveryValidationEvidenceGateDecision decision = LiveRecoveryValidationEvidenceGate.Evaluate(
            new LiveRecoveryValidationEvidenceAttempt(
                Enabled: true,
                RunId: RunId,
                StartedAtUtc: failedAtUtc - TimeSpan.FromMinutes(1),
                CompletedAtUtc: failedAtUtc + TimeSpan.FromMinutes(1),
                LatestAttemptCompletedSuccessfully: true,
                Evidence: [],
                RetentionFailureMarkers: replayed,
                AlertsDeliveredByJob: new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    [LiveRecoveryValidationJobs.Continuity] = 1,
                }),
            new LiveRecoveryValidationGatePolicy(
                ConfiguredProjectionDatasets: ["recovery-baseline"],
                TargetDeviationsBlockRelease: true,
                RequiredDriverMode: RecoveryValidationEvidenceManifest.LiveDriverMode,
                MaximumEvidenceAge: TimeSpan.FromDays(8)),
            failedAtUtc + TimeSpan.FromMinutes(2));

        decision.StopShipReasons.ShouldContain("continuity:evidence_retention_failed");
        decision.StopShipReasons.ShouldNotContain("continuity:missing_evidence");
        decision.StopShipReasons.ShouldNotContain("retention_failure_marker_invalid");
    }

    private static RecoveryValidationEvidenceRetentionFailureMarker Marker(
        string jobId,
        string scenario,
        DateTimeOffset failedAtUtc)
        => RecoveryValidationEvidenceRetentionFailureMarker.Create(RunId, jobId, scenario, failedAtUtc);

    private static string CreateRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), $"retention-failure-marker-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private sealed class TotallyUnavailableEvidenceSink : IRecoveryValidationEvidenceSink
    {
        public ValueTask RecordAsync(ContinuityDrillReport report, CancellationToken cancellationToken)
            => throw new IOException("evidence directory unavailable");

        public ValueTask RecordAsync(ProjectionRebuildReport report, CancellationToken cancellationToken)
            => throw new IOException("evidence directory unavailable");

        public ValueTask RecordAsync(ScopedOutageDegradationReport report, CancellationToken cancellationToken)
            => throw new IOException("evidence directory unavailable");
    }

    private sealed class MeasuringRunner(DateTimeOffset startedAtUtc) : IContinuityDrillScenarioRunner
    {
        public ValueTask<ContinuityDrillMeasurement> RunAsync(
            string scenario,
            string testTenantRef,
            string correlationId,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(new ContinuityDrillMeasurement(
                startedAtUtc,
                startedAtUtc + TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                DataLossDetected: false));
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : ISystemClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }
}
