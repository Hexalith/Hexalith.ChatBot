namespace Hexalith.ChatBot.StoryEvidenceGate.Tests;

/// <summary>Produces bytes without a useful initial length, as a growing input or redirected pipe can.</summary>
/// <param name="producedLength">The number of available bytes.</param>
internal sealed class GrowingEvidenceStream(long producedLength) : Stream
{
    /// <summary>Gets the number of bytes consumed by the reader.</summary>
    internal long BytesRead { get; private set; }

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => 0;

    /// <inheritdoc/>
    public override long Position
    {
        get => BytesRead;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        int available = (int)Math.Min(count, producedLength - BytesRead);
        Array.Clear(buffer, offset, available);
        BytesRead += available;
        return available;
    }

    /// <inheritdoc/>
    public override void Flush() => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
