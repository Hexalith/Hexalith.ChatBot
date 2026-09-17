using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed record ComplianceAuditDetailQuery(
    string AuditRecordRef,
    bool CanSearchTenantAudit,
    IReadOnlyList<string> ExplicitProjectGrants,
    string? TaskId);
