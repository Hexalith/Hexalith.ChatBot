using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record ComplianceAuditQueryFilters(
    string QueryRef,
    IReadOnlyList<ComplianceAuditFilterRef> Filters,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int Limit);
