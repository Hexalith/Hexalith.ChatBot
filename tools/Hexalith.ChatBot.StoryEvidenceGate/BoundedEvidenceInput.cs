using System.Text;

namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>Bounds evidence inputs before parsing or hashing their exact bytes.</summary>
internal static class BoundedEvidenceInput
{
    /// <summary>The maximum JSON input size, in bytes.</summary>
    internal const int MaximumJsonBytes = 4 * 1024 * 1024;

    /// <summary>The maximum TRX, source, text, or process-stream size, in bytes.</summary>
    internal const int MaximumInputBytes = 64 * 1024 * 1024;

    /// <summary>The maximum number of retained lines in a local line-array input.</summary>
    internal const int MaximumTextLines = 65_536;

    /// <summary>The maximum supported JSON nesting depth.</summary>
    internal const int MaximumJsonDepth = 64;

    /// <summary>Reads a file while enforcing its byte ceiling even when it grows during the read.</summary>
    internal static byte[] ReadFileBytes(string path, int maximumBytes, string reasonCode, string subject)
    {
        using FileStream stream = File.OpenRead(path);
        if (stream.Length > maximumBytes)
        {
            throw new GateValidationException(reasonCode, subject);
        }

        return ReadBytes(stream, maximumBytes, reasonCode, subject);
    }

    /// <summary>Reads bounded text with the same encoding and BOM handling as ordinary file text reads.</summary>
    internal static string ReadFileText(string path, int maximumBytes, string reasonCode, string subject)
    {
        byte[] bytes = ReadFileBytes(path, maximumBytes, reasonCode, subject);
        using MemoryStream stream = new(bytes, writable: false);
        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>Reads bounded text lines without changing line-ending semantics.</summary>
    internal static string[] ReadFileLines(string path, string reasonCode, string subject)
        => ReadTextLines(ReadFileText(path, MaximumInputBytes, reasonCode, subject), reasonCode, subject);

    /// <summary>Bounds decoded text lines before retaining an excess entry.</summary>
    internal static string[] ReadTextLines(string text, string reasonCode, string subject)
    {
        using StringReader reader = new(text);
        List<string> lines = [];
        while (reader.ReadLine() is { } line)
        {
            if (lines.Count == MaximumTextLines)
            {
                throw new GateValidationException(reasonCode, subject);
            }

            lines.Add(line);
        }

        return lines.ToArray();
    }

    /// <summary>Reads an arbitrary stream using a bounded buffer and refuses the first excess byte.</summary>
    internal static byte[] ReadBytes(Stream stream, int maximumBytes, string reasonCode, string subject)
    {
        using MemoryStream output = new();
        byte[] buffer = new byte[64 * 1024];
        while (true)
        {
            int count = stream.Read(buffer, 0, Math.Min(buffer.Length, maximumBytes - (int)output.Length + 1));
            if (count == 0)
            {
                return output.ToArray();
            }

            if (output.Length + count > maximumBytes)
            {
                throw new GateValidationException(reasonCode, subject);
            }

            output.Write(buffer, 0, count);
        }
    }

    /// <summary>Reads a process stream with a byte ceiling and cancellation support.</summary>
    internal static async Task<byte[]> ReadBytesAsync(
        Stream stream,
        int maximumBytes,
        string reasonCode,
        string subject,
        CancellationToken cancellationToken)
    {
        using MemoryStream output = new();
        byte[] buffer = new byte[64 * 1024];
        while (true)
        {
            int count = await stream.ReadAsync(
                buffer.AsMemory(0, Math.Min(buffer.Length, maximumBytes - (int)output.Length + 1)),
                cancellationToken).ConfigureAwait(false);
            if (count == 0)
            {
                return output.ToArray();
            }

            if (output.Length + count > maximumBytes)
            {
                throw new GateValidationException(reasonCode, subject);
            }

            output.Write(buffer, 0, count);
        }
    }

    /// <summary>Enforces the JSON byte ceiling on already decoded Git text.</summary>
    internal static void ValidateJsonText(string text, string reasonCode)
    {
        if (Encoding.UTF8.GetByteCount(text) > MaximumJsonBytes)
        {
            throw new GateValidationException(reasonCode, "json-input-limit");
        }
    }
}
