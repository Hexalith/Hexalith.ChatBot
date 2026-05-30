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
    string? PredecessorHash);
