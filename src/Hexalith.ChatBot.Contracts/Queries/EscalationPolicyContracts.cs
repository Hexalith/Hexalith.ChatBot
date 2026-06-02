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

/// <summary>
/// A summary-safe escalation-policy row. All fields are declared enum/token values or bounded integers; no
/// recipient PII.
/// </summary>
public sealed record EscalationPolicySummaryRow(
    string StateClass,
    string Scope,
    int AgeThresholdSeconds,
    string SeverityThreshold,
    string EscalationTargetRole,
    string EscalationChannel);

public sealed record EscalationPolicySummary(
    string ActiveSnapshotRef,
    IReadOnlyList<EscalationPolicySummaryRow> Rows,
    string EscalationFingerprint,
    long SourceVersion,
    string SchemaVersion,
    string CorrelationId);
