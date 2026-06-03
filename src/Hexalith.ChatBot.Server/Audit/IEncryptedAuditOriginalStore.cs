namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Holds the encrypted original audit payload as opaque ciphertext at rest (Story 9.1, AC3). It is deliberately
/// separate from the <see cref="IKmsRedactionKeyStore"/>: the key lives in the KMS, the ciphertext lives here, and
/// erasure works by destroying the key (crypto-shred) — never by deleting the chain. Storing is write-once per handle;
/// there is no in-place mutation or chain delete. The ciphertext may remain after a shred, but it is unrecoverable.
/// </summary>
internal interface IEncryptedAuditOriginalStore
{
    /// <summary>Stores the ciphertext for a redaction-key handle (opaque bytes, write-once).</summary>
    void Store(string keyHandle, byte[] ciphertext);

    /// <summary>Returns the stored ciphertext for a handle, or <c>false</c> when none was stored.</summary>
    bool TryGet(string keyHandle, out byte[] ciphertext);
}
