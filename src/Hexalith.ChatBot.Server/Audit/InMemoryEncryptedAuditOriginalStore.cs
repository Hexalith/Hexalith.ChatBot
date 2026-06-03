namespace Hexalith.ChatBot.Server.Audit;

/// <summary>In-process, dev/test implementation of <see cref="IEncryptedAuditOriginalStore"/> (Story 9.1, AC3).</summary>
internal sealed class InMemoryEncryptedAuditOriginalStore : IEncryptedAuditOriginalStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, byte[]> _ciphertexts = new(StringComparer.Ordinal);

    public void Store(string keyHandle, byte[] ciphertext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHandle);
        ArgumentNullException.ThrowIfNull(ciphertext);
        lock (_gate)
        {
            _ciphertexts[keyHandle] = [.. ciphertext];
        }
    }

    public bool TryGet(string keyHandle, out byte[] ciphertext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHandle);
        lock (_gate)
        {
            if (_ciphertexts.TryGetValue(keyHandle, out byte[]? stored))
            {
                ciphertext = [.. stored];
                return true;
            }
        }

        ciphertext = [];
        return false;
    }
}
