using System.Net;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Models actor-state response headers followed by a body that completes only on cancellation.</summary>
internal sealed class RecoveryProbeCancellationContent : HttpContent
{
    /// <summary>Gets whether reading the response body has started.</summary>
    internal bool SerializationStarted { get; private set; }

    /// <summary>Gets whether response-body reading observed the supplied cancellation.</summary>
    internal bool CancellationObserved { get; private set; }

    /// <inheritdoc />
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => SerializeToStreamAsync(stream, context, CancellationToken.None);

    /// <inheritdoc />
    protected override async Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context,
        CancellationToken cancellationToken)
    {
        SerializationStarted = true;
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            CancellationObserved = true;
            throw;
        }
    }

    /// <inheritdoc />
    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}
