namespace Hexalith.ChatBot.Server.Audit;

/// <summary>Records the diagnostic retention-failure side channel without delaying audit and alert indefinitely.</summary>
internal static class RecoveryValidationEvidenceRetentionFailureRecorder
{
    private static readonly TimeSpan FallbackTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Writes the measured report while the scenario still has a live caller.</summary>
    internal static Task WritePrimaryAsync(Func<CancellationToken, ValueTask> recordAsync, CancellationToken cancellationToken)
        => Task.Run(() => recordAsync(cancellationToken).AsTask(), cancellationToken).WaitAsync(cancellationToken);

    /// <summary>Tries one canonical unmeasurable write with a separate budget after the primary write fails.</summary>
    internal static async ValueTask<bool> TryRetainFallbackAsync(Func<CancellationToken, ValueTask> recordAsync)
    {
        using CancellationTokenSource deadline = new(FallbackTimeout);
        CancellationToken token = deadline.Token;
        try
        {
            // Apply the deadline even if the sink blocks before returning its asynchronous operation.
            await Task.Run(() => recordAsync(token).AsTask(), token).WaitAsync(token).ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>The single product-wide upper bound for a best-effort marker write.</summary>
    internal static readonly TimeSpan SinkTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Attempts the entire side channel, including timestamp acquisition and marker construction. No failure from this
    /// diagnostic path may mask the unmeasurable report or prevent its audit and alert.
    /// </summary>
    public static async ValueTask TryRecordAsync(
        IRecoveryValidationEvidenceRetentionFailureSink sink,
        ISystemClock clock,
        DateTimeOffset reportStartedAtUtc,
        string runId,
        string jobId,
        string scenario)
    {
        try
        {
            DateTimeOffset failedAtUtc = clock.UtcNow;
            if (failedAtUtc < reportStartedAtUtc)
            {
                failedAtUtc = reportStartedAtUtc;
            }

            RecoveryValidationEvidenceRetentionFailureMarker marker =
                RecoveryValidationEvidenceRetentionFailureMarker.Create(
                    runId,
                    jobId,
                    scenario,
                    failedAtUtc);
            // Offloaded before the bound is applied. `WaitAsync` only bounds the AWAITED portion of a task, so a
            // sink that blocks synchronously -- a hung filesystem inside `Directory.CreateDirectory`, `Serialize`, or
            // `File.Move` -- never yields a task to bound and held audit-then-alert open past the documented one
            // second. Running the attempt on a pool thread makes the bound real: the caller resumes on time and an
            // abandoned side-channel write can no longer delay the operator alert it exists to annotate.
            await Task.Run(
                    () => sink.RecordAsync(marker, CancellationToken.None).AsTask(),
                    CancellationToken.None)
                .WaitAsync(SinkTimeout)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Best effort only. Clock, construction, timeout, and sink failures must not escape this side channel.
        }
    }
}
