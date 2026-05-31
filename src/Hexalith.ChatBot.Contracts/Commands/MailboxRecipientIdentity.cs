namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Provider recipient identity preserved as mailbox source evidence.
/// </summary>
/// <param name="Address">Provider email address value.</param>
/// <param name="DisplayName">Provider display name when available.</param>
/// <param name="Kind">Recipient kind, for example to, cc, or bcc.</param>
public sealed record MailboxRecipientIdentity(string Address, string? DisplayName, string Kind);
