using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record RequestComplianceInvestigation(
    string InvestigationId,
    string QueryRef,
    IReadOnlyList<string> FilterRefs,
    string ReasonCode,
    string RequesterRef,
    long SourceVersion,
    string CorrelationId,
    string PolicySnapshotId,
    ComplianceAuditRedactionState RedactionState,
    ComplianceEscalationStatus EscalationStatus,
    string SchemaVersion) : IChatBotCommand;
