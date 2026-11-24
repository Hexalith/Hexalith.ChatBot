namespace Hexalith.ChatBot.AppHost.Aspire;

/// <summary>Owns the generated realm directory for the lifetime of the local AppHost.</summary>
internal sealed class KeycloakRealmImport(string directoryPath) : IDisposable
{
    private FileStream? _owner;
    private int _disposed;

    /// <summary>Gets the directory mounted as the Keycloak realm import.</summary>
    public string DirectoryPath { get; } = directoryPath;

    /// <summary>Registers a live filesystem claim before the realm is written.</summary>
    public void Claim()
        => _owner = KeycloakRealmOwner.Claim(DirectoryPath)
            ?? throw new IOException("Could not claim the generated Keycloak realm directory.");

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            _owner?.Dispose();
            Directory.Delete(DirectoryPath, recursive: true);
        }
        catch (IOException)
        {
            // The next startup sweep can retry cleanup of a directory whose files are still locked.
        }
        catch (UnauthorizedAccessException)
        {
            // Keep shutdown best effort when a filesystem or ACL prevents deletion.
        }
    }
}
