using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record RequestComplianceEscalation(
    string EscalationId,
    string InvestigationId,
    string AuditRecordRef,
    string ReasonCode,
    string RequesterRef,
    string EscalationTargetRef,
    long SourceVersion,
    string CorrelationId,
    string PolicySnapshotId,
    ComplianceAuditRedactionState RedactionState,
    ComplianceEscalationStatus EscalationStatus,
    string SchemaVersion) : IChatBotCommand;
