using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed record ComplianceAuditSearchQuery(
    ComplianceAuditQueryFilters? Filters,
    string? TaskId);
