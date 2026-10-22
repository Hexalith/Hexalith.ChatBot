using System.Net;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Fails if a status-only control probe attempts to buffer an irrelevant response body.</summary>
internal sealed class FailingRecoveryHttpContent : HttpContent
{
    /// <summary>Gets the number of attempted response-body reads.</summary>
    internal int Reads { get; private set; }

    /// <inheritdoc />
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        Reads++;
        throw new HttpRequestException("response-body-failure");
    }

    /// <inheritdoc />
    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}
