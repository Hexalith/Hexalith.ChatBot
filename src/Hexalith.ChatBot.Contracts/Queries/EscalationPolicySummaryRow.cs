using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

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
