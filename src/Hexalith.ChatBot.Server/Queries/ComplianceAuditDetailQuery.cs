using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed record ComplianceAuditDetailQuery(
    string AuditRecordRef,
    string? TaskId);
