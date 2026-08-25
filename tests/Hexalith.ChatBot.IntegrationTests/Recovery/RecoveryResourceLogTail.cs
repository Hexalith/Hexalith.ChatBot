using System.Collections.Concurrent;

using Aspire.Hosting.ApplicationModel;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Keeps a bounded tail of one composed resource's log stream so a recovery-lane startup failure can name its own
/// cause instead of reporting only the HTTP status it observed from outside.
/// </summary>
/// <remarks>
/// The recovery lane previously discarded every resource log, so a persistent <c>503</c> at the mailbox admission
/// probe carried no reason code and could not be distinguished from a slow start.
/// </remarks>
/// <remarks>
/// <para>
/// <b>Metadata only.</b> An earlier revision rendered raw captured lines into the exception message, and the lane
/// that throws it uploads its whole <c>TestResults</c> directory — including the detailed console log — as the
/// retained evidence artifact. Raw resource logs therefore reached uploaded evidence, breaking the same
/// metadata-only obligation this story enforces everywhere else.
/// </para>
/// <para>
/// Captured content is now never rendered. <see cref="Render"/> emits only derived metadata: how many lines were
/// captured, and the distinct logger categories and stable <c>Stage=</c> tokens observed. Those name where to look
/// without carrying message bodies, identifiers, payloads or claims into an artifact.
/// </para>
/// </remarks>
internal sealed class RecoveryResourceLogTail : IAsyncDisposable
{
    private readonly Task _captureTask;
    private readonly ConcurrentQueue<string> _lines = new();
    private static readonly TimeSpan DrainBudget = TimeSpan.FromSeconds(5);
    private const int MaximumRenderedTokens = 20;
    private const int MaximumTokenLength = 128;
    private readonly int _maximumLines;
    private readonly string _resourceName;
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _stopSource;
    private int _stopRequested;
    private volatile string? _captureFault;

    private RecoveryResourceLogTail(
        string resourceName,
        IAsyncEnumerable<IReadOnlyList<LogLine>> logBatches,
        int maximumLines,
        CancellationToken cancellationToken)
    {
        _resourceName = resourceName;
        _maximumLines = maximumLines;

        // Deliberately NOT linked to the caller's token. Both tails are started under the startup CTS, and that is
        // the very CTS whose expiry triggers the diagnostic they exist to explain — a linked capture would be
        // cancelled by its own trigger and render an empty tail exactly when it is needed.
        _stopSource = new CancellationTokenSource();
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

    /// <summary>Renders derived, metadata-only evidence from the retained tail.</summary>
    /// <returns>Counts and observed categories/stages — never captured content.</returns>
    public string Render()
    {
        string[] captured = [.. _lines];
        string fault = _captureFault is { Length: > 0 } observed ? $" captureFault={observed}" : string.Empty;
        if (captured.Length == 0)
        {
            return $"[{_resourceName}] no captured log lines.{fault}";
        }

        string[] categories = [.. captured
            .Select(line => Extract(line, "\"Category\":\"", "\""))
            .Where(static value => value is not null)
            .Select(static value => value!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Take(MaximumRenderedTokens)];
        string[] stages = [.. captured
            .Select(line => Extract(line, "Stage=", ","))
            .Where(static value => value is not null)
            .Select(static value => value!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Take(MaximumRenderedTokens)];

        return $"[{_resourceName}] capturedLines={captured.Length}"
            + $" categories=[{string.Join('|', categories)}]"
            + $" stages=[{string.Join('|', stages)}]"
            + fault;
    }

    /// <summary>Pulls one bounded, delimiter-fenced token out of a captured line.</summary>
    private static string? Extract(string line, string prefix, string terminator)
    {
        int start = line.IndexOf(prefix, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += prefix.Length;
        int end = line.IndexOf(terminator, start, StringComparison.Ordinal);
        int length = end < 0 ? line.Length - start : end - start;
        if (length <= 0 || length > MaximumTokenLength)
        {
            return null;
        }

        string token = line[start..(start + length)];

        // Defence in depth: only emit tokens that are safe stable identifiers, never free text.
        return token.All(static c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_') ? token : null;
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
            // Bounded: an enumerator that does not observe cancellation promptly must not hang the lane at scope
            // exit — the same unbounded-join pathology this change set removes from the EventStore host. Nothing
            // is rethrown, because disposal runs while a real failure may already be unwinding.
            await _captureTask.WaitAsync(DrainBudget).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Includes TimeoutException from the drain budget and any capture fault. Diagnostics never win.
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
            // A diagnostic helper must never be able to become the failure. Rethrowing here surfaced at scope exit
            // and REPLACED the genuine test failure already unwinding — the precise way this session kept losing
            // diagnoses. The fault is retained as renderable metadata instead, and the capture ends quietly.
            _captureFault = exception.GetType().Name;
            _started.TrySetResult();
        }
    }
}
