using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Story 9.9 (AC3/AC5): the thin server-internal coordinator that runs the <b>audit-chain</b> portion of an
/// <c>erasure</c>-mode run through the EXISTING Story 9.1 <see cref="AuditRedactionService"/> seam — it does NOT
/// reimplement crypto-shred or tombstone. For an audited subject it appends a redaction record
/// (<see cref="AuditRedactionService.RegisterRedactionAsync"/>, the chain grows by one) then erases
/// (<see cref="AuditRedactionService.Erase"/> = KMS key-shred + projection tombstone), and turns the returned
/// <see cref="AuditRedactionRegistration"/> into a populated metadata-only <see cref="ErasureProofEntry"/>
/// (tenant-scoped subject locator + <c>tombstoned</c>, safe KMS key handle + <c>shredded</c>). The chain bytes are
/// untouched, so <see cref="WormAuditChainVerifier"/> still verifies end-to-end afterward (the AC3 guarantee).
/// <para>
/// The non-audit-store destruction runtime (vector indexes, embedding/cache stores, attachment folders, projection
/// stores) is the DEFERRED seam: <see cref="DestroyNonAuditStoreSubjectAsync"/> models the fan-out call site as a
/// documented hook, but this story ships no byte-level destruction for those stores. The audit-chain erasure path is
/// NOT deferred — Story 9.1 already built it and this runner wires the workflow to it.
/// </para>
/// </summary>
internal sealed class DeletionErasureRunner(AuditRedactionService redactionService)
{
    private readonly AuditRedactionService _redactionService = redactionService
        ?? throw new ArgumentNullException(nameof(redactionService));

    /// <summary>
    /// Erases one audited subject through the Story 9.1 seam and returns its erasure-proof confirmation. Append the
    /// redaction record, shred the key, tombstone the projection — then build the proof entry from the registration.
    /// </summary>
    public async ValueTask<ErasureProofEntry> EraseAuditSubjectAsync(
        WormAuditChainRecord originalRecord,
        string dataClassId,
        string subjectRef,
        string reasonCode,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(originalRecord);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataClassId);

        AuditRedactionRegistration registration = await _redactionService
            .RegisterRedactionAsync(originalRecord, subjectRef, reasonCode, correlationId, cancellationToken)
            .ConfigureAwait(false);

        // Crypto-shred (key gone ⇒ original unrecoverable) + projection tombstone (reads collapse to safe-not-found).
        _redactionService.Erase(registration);

        return new ErasureProofEntry(
            dataClassId,
            SubjectLocator: $"{registration.TenantRef}:{registration.SubjectRef}",
            Tombstoned: true,
            KeyHandle: registration.KeyHandle,
            KeyShredded: true);
    }

    /// <summary>
    /// DEFERRED hook (AC1 inert-control-floor): the byte-and-key destruction runtime for a NON-audit derived store
    /// (vector/embedding/cache/attachment/projection). The live worker will fan each <c>crypto-shredded</c>/
    /// <c>tombstoned</c>/<c>hard-deleted</c> class out to this seam; this story models the call site only and performs
    /// no destruction here — the request is governed, the plan is behavior/authority bounded, and the audit-chain
    /// erasure is real, but the non-audit-store runtime is intentionally not wired.
    /// </summary>
    public ValueTask DestroyNonAuditStoreSubjectAsync(
        DeletionErasureClassResult classResult,
        string subjectRef,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(classResult);
        _ = subjectRef;
        _ = cancellationToken;
        _ = _redactionService;

        // Deferred: no non-audit-store destruction runtime ships in Story 9.9. See ADR deferrals.
        return ValueTask.CompletedTask;
    }
}
