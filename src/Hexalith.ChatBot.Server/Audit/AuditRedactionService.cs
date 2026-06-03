using System.Text.Json;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The registration produced when a record is redacted: the key handle, the subject, and the appended redaction record.</summary>
internal sealed record AuditRedactionRegistration(
    string TenantRef,
    string SubjectRef,
    string KeyHandle,
    string RedactedRecordLocator,
    WormAuditChainRecord RedactionRecord);

/// <summary>
/// Coordinates GDPR right-to-erasure over the immutable WORM chain (Story 9.1, AC3/NFR49a, cross-cutting #13). Erasure
/// is resolved by <b>crypto-shredding + projection tombstone</b>, never by mutating or deleting the chain:
/// <list type="number">
///   <item>encrypt the original envelope under a fresh per-subject key minted in the separate
///   <see cref="IKmsRedactionKeyStore"/>, holding only opaque ciphertext in the
///   <see cref="IEncryptedAuditOriginalStore"/>;</item>
///   <item><b>append</b> a metadata-only redaction record to the chain (a normal chained append — the chain grows, never
///   shrinks) referencing the redacted record by safe locator, the reason code, and the key handle;</item>
///   <item>on erasure, <see cref="IKmsRedactionKeyStore.Shred"/> the key (the encrypted original becomes unrecoverable)
///   and <see cref="IRedactionProjectionStore.Tombstone"/> the subject's projection (reads collapse to safe-not-found).</item>
/// </list>
/// The chain bytes are untouched throughout, so <see cref="WormAuditChainVerifier"/> still verifies end-to-end after
/// erasure (AC2 still passes).
/// </summary>
internal sealed class AuditRedactionService(
    IWormAuditStore wormStore,
    IKmsRedactionKeyStore kms,
    IEncryptedAuditOriginalStore originalStore,
    IRedactionProjectionStore projectionStore,
    ISystemClock clock)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Encrypts the original record under a fresh KMS key, stores the opaque ciphertext, and appends a redaction record
    /// to the tenant's chain. The chain grows by exactly one record; the original chained record is never touched.
    /// </summary>
    public async ValueTask<AuditRedactionRegistration> RegisterRedactionAsync(
        WormAuditChainRecord originalRecord,
        string subjectRef,
        string reasonCode,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(originalRecord);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        string tenantRef = originalRecord.Envelope.TenantId;

        // The original envelope is preserved only as opaque ciphertext under a per-subject KMS key.
        string keyHandle = kms.CreateKey(subjectRef);
        byte[] plaintext = JsonSerializer.SerializeToUtf8Bytes(originalRecord.Envelope, SerializerOptions);
        byte[] ciphertext = kms.Encrypt(keyHandle, plaintext);
        originalStore.Store(keyHandle, ciphertext);

        string redactedRecordLocator = $"seq:{originalRecord.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        AuditEnvelope redactionEnvelope = AuditEnvelopeFactory.AuditRecordRedacted(
            tenantRef,
            redactedRecordLocator,
            subjectRef,
            keyHandle,
            reasonCode,
            correlationId,
            clock.UtcNow);

        WormAuditAppendOutcome append = await wormStore.AppendAsync(redactionEnvelope, cancellationToken).ConfigureAwait(false);
        if (!append.Succeeded || append.Record is null)
        {
            throw new InvalidOperationException($"Failed to append redaction record to the WORM chain: {append.ReasonCode}.");
        }

        return new AuditRedactionRegistration(tenantRef, subjectRef, keyHandle, redactedRecordLocator, append.Record);
    }

    /// <summary>
    /// Completes erasure: shreds the redaction key (the encrypted original becomes unrecoverable) and tombstones the
    /// subject's projection (reads collapse to safe-not-found). The immutable chain is untouched.
    /// </summary>
    public void Erase(AuditRedactionRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        kms.Shred(registration.KeyHandle);
        projectionStore.Tombstone(registration.TenantRef, registration.SubjectRef);
    }

    /// <summary>
    /// Attempts to recover the original envelope for lawful access (before erasure). Returns <c>false</c> once the key
    /// has been shredded — the original is then unrecoverable.
    /// </summary>
    public bool TryRecoverOriginal(string keyHandle, out AuditEnvelope? original)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyHandle);
        original = null;

        if (!originalStore.TryGet(keyHandle, out byte[] ciphertext) ||
            !kms.TryDecrypt(keyHandle, ciphertext, out byte[] plaintext))
        {
            return false;
        }

        original = JsonSerializer.Deserialize<AuditEnvelope>(plaintext, SerializerOptions);
        return original is not null;
    }
}
