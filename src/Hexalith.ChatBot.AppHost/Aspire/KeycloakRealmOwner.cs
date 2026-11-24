namespace Hexalith.ChatBot.AppHost.Aspire;

/// <summary>Coordinates ownership of a generated Keycloak realm through the shared filesystem.</summary>
internal static class KeycloakRealmOwner
{
    private const string FileName = "owner.marker";
    private static ReadOnlySpan<byte> Marker => "hexalith-chatbot-realm-owner-v2"u8;

    /// <summary>Writes a recognized marker and holds its ownership claim until the stream is disposed.</summary>
    public static FileStream? Claim(string directory)
    {
        string path = Path.Combine(directory, FileName);
        FileStream? owner = null;
        try
        {
            // Other readers may inspect the marker, and the owning process can still remove its directory.
            // A competing exclusive read-write claim conflicts with this shared handle.
            owner = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete);
            owner.Write(Marker);
            owner.Flush();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // If creation succeeded, keep its handle even when the write failed. An unknown or missing marker
            // causes the sweep to preserve the directory.
            Console.Error.WriteLine($"Keycloak realm owner marker could not be written. Cause={exception.GetType().Name}");
        }

        return owner;
    }

    /// <summary>Checks whether the marker uses the current ownership protocol.</summary>
    internal static bool HasKnownMarker(string directory)
    {
        try
        {
            using FileStream stream = new(Path.Combine(directory, FileName), FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            return IsKnown(stream);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Checks for a recognized, unlocked ownership claim.</summary>
    public static bool CanReclaim(string directory)
    {
        try
        {
            using FileStream stream = new(Path.Combine(directory, FileName), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return IsKnown(stream);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Checks that the hosting filesystem makes competing opens fail while a claim is held.</summary>
    public static bool SupportsExclusiveClaims(string temporaryRoot)
    {
        string path = Path.Combine(temporaryRoot, $"hexalith-chatbot-lock-check-{Guid.NewGuid():N}");
        try
        {
            using FileStream owner = new(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete,
                bufferSize: 1, FileOptions.DeleteOnClose);
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                using FileStream competitor = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool IsKnown(FileStream stream)
    {
        if (stream.Length != Marker.Length)
        {
            return false;
        }

        Span<byte> actual = stackalloc byte[Marker.Length];
        stream.ReadExactly(actual);
        return actual.SequenceEqual(Marker);
    }
}
