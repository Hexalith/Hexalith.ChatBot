using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Workers.Mailbox;

/// <summary>
/// M0 controlled-mailbox configuration: exactly one tenant mailbox pattern is active.
/// </summary>
/// <param name="MailboxId">Controlled mailbox provider id.</param>
/// <param name="SourceContext">Opaque source context recorded with intake commands.</param>
/// <param name="ControlState">
/// FR74 governance control state. <see cref="MailboxSourceControlState.Disabled"/> means the source was disabled
/// through the two-person admin path and intake must be blocked before any Graph fetch. Distinct from the Story 7.3
/// mailbox-configuration <c>IsEnabled</c> flag.
/// </param>
/// <param name="RateLimit">
/// Story 7.14 per-source intake rate-limit budget, or <see langword="null"/> when no limit is configured. Append-only
/// field: a configured budget defers (never drops) intake that exceeds the bounded per-window limit, independently of
/// the <see cref="ControlState"/> control-state blocks.
/// </param>
public sealed record ControlledMailboxPattern(
    string MailboxId,
    string SourceContext,
    MailboxSourceControlState ControlState = MailboxSourceControlState.Active,
    MailboxRateLimitState? RateLimit = null);
