using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Mailbox;

/// <summary>
/// Structured rejection for an invalid or unauthorized mailbox-source rate-limit submission (including an
/// out-of-bounds budget). Carries only safe tokens.
/// </summary>
public sealed record MailboxSourceRateLimitRejected(
    string RateLimitChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
