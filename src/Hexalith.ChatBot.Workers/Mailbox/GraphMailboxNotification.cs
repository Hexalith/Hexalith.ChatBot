namespace Hexalith.ChatBot.Workers.Mailbox;

/// <summary>
/// Metadata from a Graph notification. Delta tokens remain opaque provider state.
/// </summary>
/// <param name="MailboxId">Controlled mailbox provider id.</param>
/// <param name="ProviderMessageId">Provider message id.</param>
/// <param name="OpaqueProviderState">Opaque Graph delta/change-notification state, never parsed or surfaced.</param>
public sealed record GraphMailboxNotification(string MailboxId, string ProviderMessageId, string? OpaqueProviderState);
