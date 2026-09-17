using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only tenant-scoped escalation-policy read request. Read-back is summary-safe (thresholds, roles,
/// channels, state-classes, severities), never recipient PII.
/// </summary>
public sealed record GetEscalationPolicySummary(
    AdminScope ScopeUsed,
    string ActiveSnapshotRef,
    string CorrelationId);
