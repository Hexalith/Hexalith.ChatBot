namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// A single appended record in a tenant's WORM (write-once, read-many) audit hash chain (Story 9.1, NFR49a). It wraps
/// the immutable <see cref="AuditEnvelope"/> and carries the chaining metadata the tamper-evidence guarantee depends on:
/// the per-tenant monotonic <see cref="Sequence"/>, the <see cref="PredecessorHash"/> linking it to the prior record in
/// the same tenant chain, the record's own cryptographic <see cref="RecordHash"/>, and the
/// <see cref="CanonicalSerializationVersion"/> that fixed the hash input. The record is metadata-only — the wrapped
/// envelope follows the existing <c>AuditMetadata</c> discipline and never carries raw item content; the only place raw
/// content may live is the separately-encrypted original held under a KMS key (see GDPR erasure, AC3).
/// </summary>
internal sealed record WormAuditChainRecord(
    AuditEnvelope Envelope,
    long Sequence,
    string PredecessorHash,
    string RecordHash,
    string CanonicalSerializationVersion);
