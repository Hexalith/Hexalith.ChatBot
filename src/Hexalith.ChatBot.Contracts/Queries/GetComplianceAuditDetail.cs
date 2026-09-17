using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record GetComplianceAuditDetail(
    AdminScope ScopeUsed,
    string AuditRecordRef,
    string CorrelationId,
    string PolicySnapshotId);
