using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Read-only request for the M2 operational dashboard overview (S8/S10, FR67). It carries no project/tenant id —
/// tenant identity comes from the authenticated gateway binding only — and never mutates state. It is a query,
/// not an <c>IChatBotCommand</c>: it adds no write path and no allowlist entry.
/// </summary>
/// <param name="ScopeUsed">The admin see-only scope the caller reads under.</param>
/// <param name="CorrelationId">The correlation identity carried through the spine.</param>
/// <param name="AggregationLimit">The maximum number of source rows aggregated per view.</param>
public sealed record GetOperationalDashboard(
    AdminScope ScopeUsed,
    string CorrelationId,
    int AggregationLimit);
