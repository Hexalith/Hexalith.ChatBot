namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Models an actor-state transport that responds only to request cancellation.</summary>
internal sealed class RecoveryProbeCancellationHandler : HttpMessageHandler
{
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Gets when the first request reaches the transport.</summary>
    internal Task Started => _started.Task;

    /// <summary>Gets whether the transport observed request cancellation.</summary>
    internal bool CancellationObserved { get; private set; }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _started.TrySetResult();
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("The unresponsive transport unexpectedly resumed without cancellation.");
        }
        catch (OperationCanceledException)
        {
            CancellationObserved = true;
            throw;
        }
    }
}
