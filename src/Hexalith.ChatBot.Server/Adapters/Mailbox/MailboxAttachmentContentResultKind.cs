namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

internal enum MailboxAttachmentContentResultKind
{
    Available,
    Unavailable,
    Retryable,
    TooLarge,
    Unauthorized,
}
