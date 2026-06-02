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
public sealed record ControlledMailboxPattern(
    string MailboxId,
    string SourceContext,
    MailboxSourceControlState ControlState = MailboxSourceControlState.Active);
