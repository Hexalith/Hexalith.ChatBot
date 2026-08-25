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

        // Extracted from the DEFAULT console formatter's shape, which is what these resources actually emit:
        // `level: Some.Logger.Category[eventId]`. The previous extractor looked for a JSON `"Category":"` field and
        // a `Stage=` token; no JSON console formatter is configured anywhere in this topology and `Stage=` appears
        // nowhere in the repository, so every render produced `categories=[] stages=[]` -- a line count standing in
        // for the cause it was added to name.
        string[] categories = [.. captured
            .Select(static line => ExtractCategory(line))
            .Where(static value => value is not null)
            .Select(static value => value!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Take(MaximumRenderedTokens)];
        string[] levels = [.. captured
            .Select(static line => ExtractLevel(line))
            .Where(static value => value is not null)
            .Select(static value => value!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Take(MaximumRenderedTokens)];

        return $"[{_resourceName}] capturedLines={captured.Length}"
            + $" levels=[{string.Join('|', levels)}]"
            + $" categories=[{string.Join('|', categories)}]"
            + fault;
    }

    /// <summary>Pulls the logger category out of a default-console-formatter line.</summary>
    /// <param name="line">One captured log line.</param>
    /// <returns>The category, or <see langword="null"/> when the line is not in that shape.</returns>
    private static string? ExtractCategory(string line)
    {
        int separator = line.IndexOf(": ", StringComparison.Ordinal);
        if (separator < 0)
        {
            return null;
        }

        int open = line.IndexOf('[', separator);
        return open < 0 ? null : SafeToken(line[(separator + 2)..open]);
    }

    /// <summary>Pulls the log level out of a default-console-formatter line.</summary>
    /// <param name="line">One captured log line.</param>
    /// <returns>The level, or <see langword="null"/> when the line is not in that shape.</returns>
    private static string? ExtractLevel(string line)
    {
        int separator = line.IndexOf(": ", StringComparison.Ordinal);
        return separator <= 0 ? null : SafeToken(line[..separator]);
    }

    /// <summary>Admits only bounded, safe stable identifiers -- never free text.</summary>
    /// <param name="token">The candidate token.</param>
    /// <returns>The token when it is a safe identifier, otherwise <see langword="null"/>.</returns>
    private static string? SafeToken(string token)
    {
        string trimmed = token.Trim();
        return trimmed.Length is > 0 and <= MaximumTokenLength
            && trimmed.All(static c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_')
                ? trimmed
                : null;
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
