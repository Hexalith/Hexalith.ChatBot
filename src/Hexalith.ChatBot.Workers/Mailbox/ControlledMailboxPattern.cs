namespace Hexalith.ChatBot.Workers.Mailbox;

/// <summary>
/// M0 controlled-mailbox configuration: exactly one tenant mailbox pattern is active.
/// </summary>
/// <param name="MailboxId">Controlled mailbox provider id.</param>
/// <param name="SourceContext">Opaque source context recorded with intake commands.</param>
public sealed record ControlledMailboxPattern(string MailboxId, string SourceContext);
