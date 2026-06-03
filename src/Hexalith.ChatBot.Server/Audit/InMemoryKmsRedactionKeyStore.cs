using System.Security.Cryptography;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// In-process, dev/test implementation of <see cref="IKmsRedactionKeyStore"/> (Story 9.1, AC3). It mints AES-256-GCM
/// keys, holds them behind a lock keyed by an opaque handle, and authenticated-encrypts payloads with a fresh random
/// nonce per call (output layout: 12-byte nonce ‖ 16-byte tag ‖ ciphertext). <see cref="Shred"/> removes the key from
/// the map; with the key gone no held ciphertext can be decrypted again — the crypto-shredding guarantee. This stands
/// in for a real KMS; production swaps the boundary (documented in <c>docs/adrs/worm-audit-backing.md</c>) without
/// changing the contract or the redaction flow.
/// </summary>
internal sealed class InMemoryKmsRedactionKeyStore : IKmsRedactionKeyStore
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private readonly Lock _gate = new();
    private readonly Dictionary<string, byte[]> _keys = new(StringComparer.Ordinal);

    public string CreateKey(string subjectRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectRef);

        // The handle is an opaque, AuditMetadata-safe token (alnum + '-'); it carries no subject content.
        string handle = $"rk-{Guid.NewGuid():N}";
        byte[] key = RandomNumberGenerator.GetBytes(KeySize);
        lock (_gate)
        {
            _keys[handle] = key;
        }

        return handle;
    }

    public byte[] Encrypt(string keyHandle, byte[] plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHandle);
        ArgumentNullException.ThrowIfNull(plaintext);

        byte[] key = GetKeyOrThrow(keyHandle);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] tag = new byte[TagSize];
        byte[] cipher = new byte[plaintext.Length];

        using (AesGcm aes = new(key, TagSize))
        {
            aes.Encrypt(nonce, plaintext, cipher, tag);
        }

        byte[] output = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, output, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, output, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, output, NonceSize + TagSize, cipher.Length);
        return output;
    }

    public bool TryDecrypt(string keyHandle, byte[] ciphertext, out byte[] plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHandle);
        ArgumentNullException.ThrowIfNull(ciphertext);

        plaintext = [];
        if (ciphertext.Length < NonceSize + TagSize)
        {
            return false;
        }

        byte[]? key;
        lock (_gate)
        {
            // After Shred the key is gone, so a held ciphertext can never be decrypted again (crypto-shred).
            if (!_keys.TryGetValue(keyHandle, out key))
            {
                return false;
            }
        }

        byte[] nonce = new byte[NonceSize];
        byte[] tag = new byte[TagSize];
        int bodyLength = ciphertext.Length - NonceSize - TagSize;
        byte[] body = new byte[bodyLength];
        Buffer.BlockCopy(ciphertext, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(ciphertext, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(ciphertext, NonceSize + TagSize, body, 0, bodyLength);

        byte[] result = new byte[bodyLength];
        try
        {
            using AesGcm aes = new(key, TagSize);
            aes.Decrypt(nonce, body, tag, result);
        }
        catch (CryptographicException)
        {
            return false;
        }

        plaintext = result;
        return true;
    }

    public void Shred(string keyHandle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHandle);
        lock (_gate)
        {
            if (_keys.Remove(keyHandle, out byte[]? key))
            {
                // Best-effort wipe of the in-memory key material on shred.
                CryptographicOperations.ZeroMemory(key);
            }
        }
    }

    public bool HasKey(string keyHandle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHandle);
        lock (_gate)
        {
            return _keys.ContainsKey(keyHandle);
        }
    }

    private byte[] GetKeyOrThrow(string keyHandle)
    {
        lock (_gate)
        {
            return _keys.TryGetValue(keyHandle, out byte[]? key)
                ? key
                : throw new InvalidOperationException("Redaction key handle is unknown or has been shredded.");
        }
    }
}
