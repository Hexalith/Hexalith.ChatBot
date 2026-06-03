namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The separate-KMS boundary for GDPR-safe audit redaction (Story 9.1, AC3/NFR49a, architecture cross-cutting #13).
/// It mints and holds a per-record (or per-subject) redaction key, encrypts the original audit payload under it, and
/// <see cref="Shred"/>s the key irrevocably on erasure — so the encrypted original becomes unrecoverable without ever
/// mutating or deleting the immutable hash chain. Keys live <b>here</b>, ciphertext lives in a separate
/// <see cref="IEncryptedAuditOriginalStore"/>: destroying the key is the act of erasure (crypto-shredding). The
/// in-process implementation is the dev/test default; production is a real KMS (documented in the ADR).
/// </summary>
internal interface IKmsRedactionKeyStore
{
    /// <summary>Mints and holds a fresh redaction key for the subject, returning its safe, metadata-only handle.</summary>
    string CreateKey(string subjectRef);

    /// <summary>Encrypts plaintext under the held key, producing opaque ciphertext (nonce + tag + body).</summary>
    byte[] Encrypt(string keyHandle, byte[] plaintext);

    /// <summary>
    /// Attempts to decrypt ciphertext under the held key. Returns <c>false</c> (and an empty plaintext) when the key
    /// has been shredded or never existed — the original is then unrecoverable.
    /// </summary>
    bool TryDecrypt(string keyHandle, byte[] ciphertext, out byte[] plaintext);

    /// <summary>Irrevocably destroys the key (crypto-shred). After this the original encrypted under it cannot be recovered.</summary>
    void Shred(string keyHandle);

    /// <summary>Whether a live (un-shredded) key exists for the handle.</summary>
    bool HasKey(string keyHandle);
}
