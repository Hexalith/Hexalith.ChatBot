using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record SearchComplianceAuditRecords(
    AdminScope ScopeUsed,
    ComplianceAuditQueryFilters Query,
    string CorrelationId);
