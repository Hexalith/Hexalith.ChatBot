namespace Hexalith.ChatBot.Server.Tests.Authentication;

/// <summary>Models failed or stalled body transport reads without starting a network listener.</summary>
/// <param name="read">The body transport behavior.</param>
internal sealed class BoundaryRequestBodyStream(Func<CancellationToken, Task<int>> read) : MemoryStream
{
    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => new(read(cancellationToken));
}
