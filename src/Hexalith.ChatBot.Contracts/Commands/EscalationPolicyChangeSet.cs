using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed, typed escalation-policy map: a set of <c>(state-class × scope) → { age-threshold, severity-threshold,
/// escalation-target-role, escalation-channel }</c> entries.
/// </summary>
public sealed record EscalationPolicyChangeSet(
    IReadOnlyList<EscalationPolicyEntry> Entries);
