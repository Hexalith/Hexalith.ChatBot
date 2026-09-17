using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only tenant-scoped notification routing read request. Read-back is summary-safe
/// (roles/channels/state-classes), never recipient PII.
/// </summary>
public sealed record GetNotificationRoutingSummary(
    AdminScope ScopeUsed,
    string ActiveSnapshotRef,
    string CorrelationId);
