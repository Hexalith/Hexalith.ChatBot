using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// A single closed routing-map entry: the recipient role and delivery channel that hear about a
/// <c>(state-class × scope)</c> pair. Keys and values are finite enums/tokens only — never free-form strings.
/// </summary>
public sealed record NotificationRoutingEntry(
    NotificationStateClass StateClass,
    AdminScope Scope,
    AdminRole RecipientRole,
    NotificationChannel Channel);
