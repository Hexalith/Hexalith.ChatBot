using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Summary-safe queue read for tenant-admin see-only scope.
/// </summary>
public sealed record GetAdminQueueSummary(
    string QueueRef,
    AdminScope ScopeUsed,
    string CorrelationId,
    int AggregationLimit);
