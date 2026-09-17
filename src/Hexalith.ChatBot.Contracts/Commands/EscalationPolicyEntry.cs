using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// A single closed escalation-policy entry: for a <c>(state-class × scope)</c> pair, the age/severity thresholds at
/// which escalation fires plus the escalation-target role and delivery channel. Keys and values are finite
/// enums/tokens only — never free-form strings. Mirrors <see cref="NotificationRoutingEntry"/>.
/// </summary>
public sealed record EscalationPolicyEntry(
    NotificationStateClass StateClass,
    AdminScope Scope,
    int AgeThresholdSeconds,
    EscalationSeverity SeverityThreshold,
    AdminRole EscalationTargetRole,
    NotificationChannel EscalationChannel);
