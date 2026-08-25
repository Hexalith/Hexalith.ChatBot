using System.Collections.Concurrent;

using Aspire.Hosting.ApplicationModel;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Keeps a bounded tail of one composed resource's log stream so a recovery-lane startup failure can name its own
/// cause instead of reporting only the HTTP status it observed from outside.
/// </summary>
/// <remarks>
/// The recovery lane previously discarded every resource log, so a persistent <c>503</c> at the mailbox admission
/// probe carried no reason code and could not be distinguished from a slow start. The tail is bounded, is read only
/// when a failure is already being reported, and is never written to an evidence manifest, report, or artifact — it
/// is diagnostic test output only.
/// </remarks>
internal sealed class RecoveryResourceLogTail : IAsyncDisposable
{
    private readonly Task _captureTask;
    private readonly ConcurrentQueue<string> _lines = new();
    private readonly int _maximumLines;
    private readonly string _resourceName;
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _stopSource;
    private int _stopRequested;

    private RecoveryResourceLogTail(
        string resourceName,
        IAsyncEnumerable<IReadOnlyList<LogLine>> logBatches,
        int maximumLines,
        CancellationToken cancellationToken)
    {
        _resourceName = resourceName;
        _maximumLines = maximumLines;
        _stopSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _captureTask = CaptureAsync(logBatches);
    }

    /// <summary>Starts capturing a bounded tail of the named resource's log stream.</summary>
    /// <param name="resourceName">The exact composed resource name.</param>
    /// <param name="logBatches">That resource's live log batches.</param>
    /// <param name="maximumLines">The maximum number of retained trailing lines.</param>
    /// <param name="cancellationToken">Cancels startup and capture.</param>
    /// <returns>The active tail.</returns>
    public static async Task<RecoveryResourceLogTail> StartAsync(
        string resourceName,
        IAsyncEnumerable<IReadOnlyList<LogLine>> logBatches,
        int maximumLines,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentNullException.ThrowIfNull(logBatches);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumLines, 1);
        RecoveryResourceLogTail tail = new(resourceName, logBatches, maximumLines, cancellationToken);
        try
        {
            await tail._started.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return tail;
        }
        catch
        {
            await tail.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Renders the retained tail, newest last, for a failure message or test output.</summary>
    /// <param name="matching">When supplied, keeps only lines containing this token (ordinal, case-insensitive).</param>
    /// <returns>The rendered tail.</returns>
    public string Render(string? matching = null)
    {
        IEnumerable<string> lines = _lines;
        if (!string.IsNullOrWhiteSpace(matching))
        {
            lines = lines.Where(line => line.Contains(matching, StringComparison.OrdinalIgnoreCase));
        }

        string[] rendered = [.. lines];
        return rendered.Length == 0
            ? $"[{_resourceName}] no captured log lines."
            : $"[{_resourceName}] last {rendered.Length} captured line(s):{Environment.NewLine}"
                + string.Join(Environment.NewLine, rendered);
    }

    /// <summary>Cancels and drains the capture loop.</summary>
    /// <returns>A task representing shutdown.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) == 0)
        {
            await _stopSource.CancelAsync().ConfigureAwait(false);
        }

        try
        {
            await _captureTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutdown is the expected end of the capture loop.
        }
        finally
        {
            _stopSource.Dispose();
        }
    }

    private async Task CaptureAsync(IAsyncEnumerable<IReadOnlyList<LogLine>> logBatches)
    {
        try
        {
            IAsyncEnumerator<IReadOnlyList<LogLine>> enumerator = logBatches
                .GetAsyncEnumerator(_stopSource.Token);
            await using (enumerator.ConfigureAwait(false))
            {
                ValueTask<bool> moveNext = enumerator.MoveNextAsync();
                _started.TrySetResult();
                while (await moveNext.ConfigureAwait(false))
                {
                    foreach (LogLine line in enumerator.Current)
                    {
                        _lines.Enqueue(line.Content);
                        while (_lines.Count > _maximumLines && _lines.TryDequeue(out _))
                        {
                            // Bounded tail: drop the oldest retained line.
                        }
                    }

                    moveNext = enumerator.MoveNextAsync();
                }
            }
        }
        catch (OperationCanceledException) when (_stopSource.IsCancellationRequested)
        {
            _started.TrySetResult();
        }
        catch (Exception exception)
        {
            _started.TrySetException(exception);
            throw;
        }
    }
}
