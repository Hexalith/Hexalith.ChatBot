using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Exercises retained failed attempts across the complete coordinator sequence.</summary>
public sealed class RecoveryAttemptFailureBoundaryTests
{
    private const string RunId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    /// <summary>Every stage exception preserves the original failure and observed delivered alerts.</summary>
    /// <param name="failedStage">Continuity, dataset setup, rebuild or scoped-outage stage index.</param>
    /// <param name="canceled">Whether the stage fails with caller cancellation.</param>
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public async Task AnyStageFailureRetainsIncompleteSummaryAndActualAlerts(int failedStage, bool canceled)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"recovery-attempt-{Guid.NewGuid():N}");
        using CancellationTokenSource caller = new();
        Exception primary = canceled ? new OperationCanceledException(caller.Token) : new IOException("stage failure");
        InMemoryOperatorAlertSink alerts = new();
        DateTimeOffset started = DateTimeOffset.UtcNow;
        int executed = 0;
        try
        {
            Exception observed = await Should.ThrowAsync<Exception>(async () =>
                await LiveContinuityAspireE2eTests.RunRecoveryAttemptAsync(async () =>
                {
                    for (int stage = 0; stage < 4; stage++)
                    {
                        executed++;
                        OperatorAlertKind kind = stage switch
                        {
                            2 => OperatorAlertKind.ProjectionRebuildValidationFailed,
                            3 => OperatorAlertKind.ScopedOutageDegradationBreach,
                            _ => OperatorAlertKind.ContinuityDrillTargetMissed,
                        };
                        if (stage != 1)
                        {
                            await alerts.EmitAsync(new(kind, "validation_failed", "tenant-a", "validation", RunId, started), caller.Token).ConfigureAwait(true);
                        }

                        await alerts.EmitAsync(new(kind, "validation_failed", "tenant-a", "validation", "other-run", started), caller.Token).ConfigureAwait(true);
                        if (stage == failedStage)
                        {
                            if (canceled)
                            {
                                await caller.CancelAsync().ConfigureAwait(true);
                            }

                            throw primary;
                        }
                    }
                }, directory, true, RunId, started, alerts, TimeSpan.FromSeconds(5)).ConfigureAwait(true)).ConfigureAwait(true);

            if (!canceled)
            {
                observed.ShouldBeSameAs(primary);
            }
            executed.ShouldBe(failedStage + 1);
            LiveRecoveryValidationAttemptSummary summary = JsonSerializer.Deserialize<LiveRecoveryValidationAttemptSummary>(
                await File.ReadAllTextAsync(Path.Combine(directory, LiveRecoveryValidationAttemptSummary.FileName), TestContext.Current.CancellationToken).ConfigureAwait(true),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldNotBeNull();
            summary.Enabled.ShouldBeTrue();
            summary.RunId.ShouldBe(RunId);
            summary.StartedAtUtc.ShouldBe(started);
            summary.CompletedAtUtc.ShouldNotBeNull().ShouldBeGreaterThanOrEqualTo(started);
            summary.LatestAttemptCompletedSuccessfully.ShouldBeFalse();
            summary.AlertsDeliveredByJob[LiveRecoveryValidationJobs.Continuity].ShouldBe(1);
            summary.AlertsDeliveredByJob[LiveRecoveryValidationJobs.ControlledLossPath].ShouldBe(0);
            summary.AlertsDeliveredByJob[LiveRecoveryValidationJobs.ProjectionRebuild].ShouldBe(failedStage >= 2 ? 1 : 0);
            summary.AlertsDeliveredByJob[LiveRecoveryValidationJobs.ScopedOutage].ShouldBe(failedStage >= 3 ? 1 : 0);
            if (canceled)
            {
                ((OperationCanceledException)observed).CancellationToken.ShouldBe(caller.Token);
            }
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>A retention failure preserves both the original and filesystem exceptions.</summary>
    [Fact]
    public async Task SummaryWriteFailurePreservesBothFailures()
    {
        string path = Path.GetTempFileName();
        IOException primary = new("stage failure");
        try
        {
            AggregateException failure = await Should.ThrowAsync<AggregateException>(async () =>
                await LiveContinuityAspireE2eTests.RunRecoveryAttemptAsync(
                    () => ValueTask.FromException(primary), path, true, RunId, DateTimeOffset.UtcNow,
                    new InMemoryOperatorAlertSink(), TimeSpan.FromSeconds(5)).ConfigureAwait(true)).ConfigureAwait(true);
            failure.InnerExceptions.Count.ShouldBe(2);
            failure.InnerExceptions[0].ShouldBeSameAs(primary);
            failure.InnerExceptions[1].ShouldBeOfType<IOException>();
            File.Exists(path).ShouldBeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Controlled loss retains its real fourth-job marker together with the outer failed summary.</summary>
    [Fact]
    public async Task ControlledLossFailureRetainsBothIndependentArtifacts()
    {
        string root = Path.Combine(Path.GetTempPath(), $"recovery-attempt-{Guid.NewGuid():N}");
        string markers = Path.Combine(root, "markers");
        FileRecoveryValidationEvidenceRetentionFailureSink sink = new(markers, Path.Combine(root, "evidence"));
        IOException primary = new("controlled loss failure");
        try
        {
            Exception observed = await Should.ThrowAsync<IOException>(async () =>
                await LiveContinuityAspireE2eTests.RunRecoveryAttemptAsync(async () =>
                {
                    _ = await LiveContinuityAspireE2eTests.RunControlledLossAndRetainAsync(
                        _ => ValueTask.FromException<ControlledLossPathReport>(primary),
                        (_, _) => ValueTask.CompletedTask, sink, RunId, TimeSpan.FromSeconds(5),
                        TestContext.Current.CancellationToken).ConfigureAwait(true);
                }, markers, true, RunId, DateTimeOffset.UtcNow, new InMemoryOperatorAlertSink(),
                    TimeSpan.FromSeconds(5)).ConfigureAwait(true)).ConfigureAwait(true);
            observed.ShouldBeSameAs(primary);
            string markerFile = Directory.GetFiles(markers, "*.retention-failure.json").ShouldHaveSingleItem();
            RecoveryValidationEvidenceRetentionFailureMarker marker = JsonSerializer.Deserialize<RecoveryValidationEvidenceRetentionFailureMarker>(
                await File.ReadAllTextAsync(markerFile, TestContext.Current.CancellationToken).ConfigureAwait(true),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldNotBeNull();
            marker.JobId.ShouldBe(LiveRecoveryValidationJobs.ControlledLossPath);
            marker.RunId.ShouldBe(RunId);
            marker.IsValid().ShouldBeTrue();
            LiveRecoveryValidationAttemptSummary summary = JsonSerializer.Deserialize<LiveRecoveryValidationAttemptSummary>(
                await File.ReadAllTextAsync(Path.Combine(markers, LiveRecoveryValidationAttemptSummary.FileName), TestContext.Current.CancellationToken).ConfigureAwait(true),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldNotBeNull();
            summary.LatestAttemptCompletedSuccessfully.ShouldBeFalse();
            summary.RunId.ShouldBe(RunId);
            Directory.Exists(Path.Combine(root, "evidence")).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>An exhausted independent budget preserves the stage failure and retention cancellation.</summary>
    [Fact]
    public async Task ExhaustedSummaryBudgetPreservesPrimaryFailureAndCancellation()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"recovery-attempt-{Guid.NewGuid():N}");
        IOException primary = new("stage failure");
        AggregateException failure = await Should.ThrowAsync<AggregateException>(async () =>
            await LiveContinuityAspireE2eTests.RunRecoveryAttemptAsync(() => ValueTask.FromException(primary),
                directory, true, RunId, DateTimeOffset.UtcNow, new InMemoryOperatorAlertSink(),
                TimeSpan.Zero).ConfigureAwait(true)).ConfigureAwait(true);
        failure.InnerExceptions.Count.ShouldBe(2);
        failure.InnerExceptions[0].ShouldBeSameAs(primary);
        ((OperationCanceledException)failure.InnerExceptions[1]).CancellationToken.IsCancellationRequested.ShouldBeTrue();
        Directory.Exists(directory).ShouldBeFalse();
    }

    /// <summary>A late validation failure supersedes a previously retained successful domain summary.</summary>
    [Fact]
    public async Task LateFailureReplacesSuccessfulSummaryWithIncompleteObservedState()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"recovery-attempt-{Guid.NewGuid():N}");
        string path = Path.Combine(directory, LiveRecoveryValidationAttemptSummary.FileName);
        InMemoryOperatorAlertSink alerts = new();
        IOException primary = new("late validation failure");
        DateTimeOffset started = DateTimeOffset.UtcNow;
        try
        {
            _ = await Should.ThrowAsync<IOException>(async () =>
                await LiveContinuityAspireE2eTests.RunRecoveryAttemptAsync(async () =>
                {
                    Directory.CreateDirectory(directory);
                    await File.WriteAllTextAsync(path, "{\"latestAttemptCompletedSuccessfully\":true}", TestContext.Current.CancellationToken).ConfigureAwait(true);
                    await alerts.EmitAsync(new(OperatorAlertKind.ScopedOutageDegradationBreach, "validation_failed", "tenant-a", "validation", RunId, started),
                        TestContext.Current.CancellationToken).ConfigureAwait(true);
                    throw primary;
                }, directory, true, RunId, started, alerts, TimeSpan.FromSeconds(5)).ConfigureAwait(true)).ConfigureAwait(true);
            LiveRecoveryValidationAttemptSummary summary = JsonSerializer.Deserialize<LiveRecoveryValidationAttemptSummary>(
                await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldNotBeNull();
            summary.LatestAttemptCompletedSuccessfully.ShouldBeFalse();
            summary.AlertsDeliveredByJob[LiveRecoveryValidationJobs.ScopedOutage].ShouldBe(1);
            summary.StartedAtUtc.ShouldBe(started);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>A successful sequence preserves its existing final summary without creating a failure record.</summary>
    [Fact]
    public async Task SuccessfulSequencePreservesItsFinalSummary()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"recovery-attempt-{Guid.NewGuid():N}");
        string summary = Path.Combine(directory, LiveRecoveryValidationAttemptSummary.FileName);
        try
        {
            await LiveContinuityAspireE2eTests.RunRecoveryAttemptAsync(() =>
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(summary, "completed-summary");
                return ValueTask.CompletedTask;
            }, directory, true, RunId, DateTimeOffset.UtcNow, new InMemoryOperatorAlertSink(), TimeSpan.FromSeconds(5)).ConfigureAwait(true);
            (await File.ReadAllTextAsync(summary, TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe("completed-summary");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
