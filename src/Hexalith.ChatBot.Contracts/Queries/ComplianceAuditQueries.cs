using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record SearchComplianceAuditRecords(
    AdminScope ScopeUsed,
    ComplianceAuditQueryFilters Query,
    string CorrelationId);

public sealed record GetComplianceAuditDetail(
    AdminScope ScopeUsed,
    string AuditRecordRef,
    string CorrelationId,
    string PolicySnapshotId);

public sealed record ComplianceAuditSearchResult(
    string QueryRef,
    IReadOnlyList<ComplianceAuditResultRow> Rows,
    string ResultFingerprint,
    DateTimeOffset GeneratedAtUtc,
    string CorrelationId);
