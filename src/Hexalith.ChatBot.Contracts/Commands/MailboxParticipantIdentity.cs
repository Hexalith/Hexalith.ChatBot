namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Provider participant identity preserved as mailbox source evidence.
/// </summary>
/// <param name="Address">Provider email address value.</param>
/// <param name="DisplayName">Provider display name when available.</param>
public sealed record MailboxParticipantIdentity(string Address, string? DisplayName);
