namespace Hexalith.ChatBot.Server.Audit;

internal sealed record AuditEnvelope(
    string TenantId,
    string ActorId,
    string ActorType,
    string CommandName,
    string ResourceId,
    string Decision,
    string ReasonCode,
    string CorrelationId,
    DateTimeOffset Timestamp,
    string PolicySnapshotId,
    IReadOnlyList<string> SourceEvidenceRefs,
    string? IdempotencyKey,
    string StateTransition,
    string RedactionDecision,
    string Outcome,
    AuditCommitPhase Phase,
    string EnvelopeSchemaVersion,
    string? PredecessorHash,
    string SurfaceOrigin,
    // Story 9.2 (FR95a): the replay/simulation run that produced this record, or null for a production record. It is
    // the marker the completeness measure uses to exclude replay events from both the numerator and the denominator
    // (NFR50a). It is security-relevant — a replay record masquerading as production must be tamper-evident — so it is
    // covered by the canonical hash from CanonicalSerializationVersion v2 onward (see WormAuditChainHasher). Story 9.2
    // only introduces the field, the hash coverage, and the exclusion predicate; Story 9.4 owns populating it during
    // replay runs against the test tenant, so today every production record leaves it null.
    string? ReplayRunId = null);
