using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record ComplianceAuditResultRow(
    string AuditRecordRef,
    string ActorRef,
    string ActorType,
    string CommandRef,
    string ResourceRef,
    string Decision,
    string ReasonCode,
    string CorrelationId,
    DateTimeOffset RecordedAtUtc,
    string PolicySnapshotId,
    ComplianceAuditRedactionState RedactionState,
    ComplianceEscalationStatus EscalationStatus,
    string SafeNextAction);
