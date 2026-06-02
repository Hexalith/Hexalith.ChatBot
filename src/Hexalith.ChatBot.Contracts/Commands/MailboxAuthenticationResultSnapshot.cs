using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Provider-supplied authentication verdicts parsed from Authentication-Results without re-verification.
/// </summary>
public sealed record MailboxAuthenticationResultSnapshot(
    MailboxAuthenticationVerdictKind Spf,
    MailboxAuthenticationVerdictKind Dkim,
    MailboxAuthenticationVerdictKind Dmarc,
    MailboxAuthenticationVerdictKind CompositeAuthentication,
    string? CompositeAuthenticationReason,
    IReadOnlyList<MailboxSelectedHeaderSnapshot> AuthenticationResultsHeaders);
