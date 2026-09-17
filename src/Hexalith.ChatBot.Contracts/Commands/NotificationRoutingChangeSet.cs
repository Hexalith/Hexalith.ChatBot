using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed, typed routing map: a set of <c>(state-class × scope) → { recipient-role, channel }</c> entries.
/// </summary>
public sealed record NotificationRoutingChangeSet(
    IReadOnlyList<NotificationRoutingEntry> Entries);
