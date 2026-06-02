using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only tenant-scoped mailbox configuration read request.
/// </summary>
public sealed record GetMailboxConfigurationSummary(
    AdminScope ScopeUsed,
    string ActiveSnapshotRef,
    string CorrelationId,
    int AggregationLimit);
