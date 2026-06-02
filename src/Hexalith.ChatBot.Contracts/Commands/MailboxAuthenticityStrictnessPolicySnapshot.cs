using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Tenant mailbox authenticity strictness snapshot used by association routing.
/// </summary>
public sealed record MailboxAuthenticityStrictnessPolicySnapshot(
    MailboxAuthenticityStrictness Strictness,
    string PolicyVersion,
    string ReasonCode);
