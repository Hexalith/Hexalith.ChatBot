using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record ComplianceAuditDetail(
    string AuditRecordRef,
    string CommandRef,
    string ResourceRef,
    string CorrelationId,
    DateTimeOffset RecordedAtUtc,
    string PolicySnapshotId,
    ComplianceAuditRedactionState RedactionState,
    ComplianceEscalationStatus EscalationStatus,
    IReadOnlyList<string> VisibleMetadataRefs,
    string SafeNextAction,
    string RedactionReasonCode);
