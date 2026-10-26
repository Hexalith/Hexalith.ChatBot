namespace Hexalith.ChatBot.Server.Tests.Adapters.Folders;

/// <summary>Fails while the generated client reads an owner response body.</summary>
internal sealed class FailingFoldersResponseStream : MemoryStream
{
    /// <summary>Gets whether the response body was read.</summary>
    public bool ReadAttempted { get; private set; }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
        => FailRead();

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
        => FailRead();

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => Task.FromResult(FailRead());

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(FailRead());

    private int FailRead()
    {
        ReadAttempted = true;
        throw new IOException("Private provider response at /home/secret was interrupted.");
    }
}
