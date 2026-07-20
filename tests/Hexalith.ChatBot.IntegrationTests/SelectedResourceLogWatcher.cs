using Aspire.Hosting.ApplicationModel;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Watches one selected resource's logs and retains only safe same-line bind-and-port correlation evidence.
/// </summary>
internal sealed class SelectedResourceLogWatcher : IAsyncDisposable
{
    private readonly Task _captureTask;
    private readonly TaskCompletionSource<bool> _evidenceOrCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly int _expectedPort;
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _stopSource;
    private int _stopRequested;

    private SelectedResourceLogWatcher(
        IAsyncEnumerable<IReadOnlyList<LogLine>> logBatches,
        int expectedPort,
        CancellationToken cancellationToken)
    {
        _expectedPort = expectedPort;
        _stopSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _captureTask = CaptureAsync(logBatches);
    }

    /// <summary>
    /// Gets a value indicating whether one captured line contained bind wording and the whole selected port.
    /// </summary>
    public bool HasCorrelatedBindEvidence { get; private set; }

    /// <summary>
    /// Starts log enumeration and waits until the first asynchronous move is active.
    /// </summary>
    /// <param name="logBatches">The exact selected resource's live log batches.</param>
    /// <param name="expectedPort">The exact selected resource port.</param>
    /// <param name="cancellationToken">Cancels watcher startup and capture.</param>
    /// <returns>The active watcher.</returns>
    public static async Task<SelectedResourceLogWatcher> StartAsync(
        IAsyncEnumerable<IReadOnlyList<LogLine>> logBatches,
        int expectedPort,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logBatches);
        SelectedResourceLogWatcher watcher = new(logBatches, expectedPort, cancellationToken);
        try
        {
            await watcher._started.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return watcher;
        }
        catch
        {
            await watcher.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Waits until correlated evidence is captured or the selected resource's log stream completes.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait without stopping capture or discarding already-derived evidence.</param>
    /// <returns><see langword="true"/> when correlated evidence was captured; otherwise, <see langword="false"/>.</returns>
    public Task<bool> WaitForEvidenceOrCompletionAsync(CancellationToken cancellationToken)
        => _evidenceOrCompletion.Task.WaitAsync(cancellationToken);

    /// <summary>
    /// Cancels and drains the watcher.
    /// </summary>
    /// <returns>A task representing watcher shutdown.</returns>
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
                    if (!HasCorrelatedBindEvidence
                        && enumerator.Current.Any(line =>
                            TopologyFailureCorrelation.IsCorrelatedLogLine(line.Content, _expectedPort)))
                    {
                        HasCorrelatedBindEvidence = true;
                        _evidenceOrCompletion.TrySetResult(result: true);
                    }

                    moveNext = enumerator.MoveNextAsync();
                }

                _evidenceOrCompletion.TrySetResult(HasCorrelatedBindEvidence);
            }
        }
        catch (OperationCanceledException) when (_stopSource.IsCancellationRequested)
        {
            _started.TrySetResult();
            _evidenceOrCompletion.TrySetResult(HasCorrelatedBindEvidence);
        }
        catch (Exception exception)
        {
            _started.TrySetException(exception);
            _evidenceOrCompletion.TrySetException(exception);
            throw;
        }
    }
}
