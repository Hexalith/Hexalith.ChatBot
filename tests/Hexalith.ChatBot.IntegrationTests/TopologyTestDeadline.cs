using System.Diagnostics;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>Bounds live topology work while leaving the runner time to retain results.</summary>
internal sealed class TopologyTestDeadline(TimeSpan? budget = null)
{
    // Required and release topology jobs allow 30 minutes. Reserve five for
    // cooperative shutdown, test reporting, and the runner's evidence steps.
    private readonly TimeSpan _budget = budget ?? TimeSpan.FromMinutes(25);
    private readonly Lazy<long> _started = new(Stopwatch.GetTimestamp);

    /// <summary>Runs the test under a linked deadline and awaits its cleanup before reporting the result.</summary>
    public async Task RunAsync(
        Func<CancellationToken, Task> run,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TimeSpan remaining = _budget - Stopwatch.GetElapsedTime(_started.Value);
        if (remaining <= TimeSpan.Zero)
        {
            throw new TimeoutException($"The live topology lane already used its {_budget} execution budget.");
        }

        using CancellationTokenSource deadline = new(remaining);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token, cancellationToken);
        try
        {
            await run(linked.Token).ConfigureAwait(false);
            linked.Token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        catch (OperationCanceledException exception) when (deadline.IsCancellationRequested)
        {
            throw new TimeoutException($"The live topology lane exceeded its {_budget} execution budget.", exception);
        }
    }
}
