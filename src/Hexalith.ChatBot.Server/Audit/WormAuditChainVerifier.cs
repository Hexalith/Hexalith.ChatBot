using System.Globalization;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Pure, deterministic verifier for a tenant's WORM audit chain (Story 9.1, AC2/NFR49a). Given a tenant's enumerated
/// chain it recomputes each record's hash and asserts predecessor linkage and per-tenant sequence continuity, returning
/// a metadata-only <see cref="WormAuditChainVerificationResult"/> — the status, a bounded reason code, and a safe
/// first-break locator token, never envelope content.
/// <para>
/// Fail-closed doctrine (Epic 8 no-fabrication carry-forward): the verifier never invents a <c>Verified</c> pass. The
/// caller treats an enumeration that cannot complete as <see cref="WormChainVerificationStatus.Unknown"/> — a breach
/// signal — rather than silent success. The first record must be the genesis record (sequence 0, genesis predecessor
/// sentinel); each subsequent record must carry sequence = index and predecessor = prior record's hash.
/// </para>
/// </summary>
internal static class WormAuditChainVerifier
{
    /// <summary>The detection→alert budget AC2 mandates: a broken chain alerts on-call within five minutes.</summary>
    public static readonly TimeSpan DetectionToAlertBudget = TimeSpan.FromMinutes(5);

    public static WormAuditChainVerificationResult Verify(string tenantRef, IReadOnlyList<WormAuditChainRecord> chain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentNullException.ThrowIfNull(chain);

        for (int index = 0; index < chain.Count; index++)
        {
            WormAuditChainRecord record = chain[index];

            // Sequence must be the dense, monotonic per-tenant index. A gap or reorder is a discontinuity.
            if (record.Sequence != index)
            {
                return Break(tenantRef, WormAuditChainVerificationResult.SequenceDiscontinuityReasonCode, index);
            }

            string expectedPredecessor = index == 0
                ? WormAuditChainHasher.GenesisPredecessorHash
                : chain[index - 1].RecordHash;

            if (!string.Equals(record.PredecessorHash, expectedPredecessor, StringComparison.Ordinal))
            {
                return Break(tenantRef, WormAuditChainVerificationResult.PredecessorLinkBrokenReasonCode, index);
            }

            // Re-hash from the stored envelope + predecessor + sequence under the version the record was stamped with
            // (Story 9.2): a mutated record no longer reproduces its hash, and a pre-9.2 (v1) record stays verifiable
            // even though the current canonical form (v2) folds in the replay marker.
            string recomputed = WormAuditChainHasher.ComputeRecordHash(record.Envelope, record.PredecessorHash, record.Sequence, record.CanonicalSerializationVersion);
            if (!string.Equals(recomputed, record.RecordHash, StringComparison.Ordinal))
            {
                return Break(tenantRef, WormAuditChainVerificationResult.RecordHashMismatchReasonCode, index);
            }
        }

        return new WormAuditChainVerificationResult(
            tenantRef,
            WormChainVerificationStatus.Verified,
            WormAuditChainVerificationResult.VerifiedReasonCode,
            FirstBreakLocator: null);
    }

    private static WormAuditChainVerificationResult Break(string tenantRef, string reasonCode, int index)
        => new(
            tenantRef,
            WormChainVerificationStatus.Broken,
            reasonCode,
            // Safe, bounded locator: the per-tenant sequence of the first offending record (no envelope content).
            $"seq:{index.ToString(CultureInfo.InvariantCulture)}");
}
