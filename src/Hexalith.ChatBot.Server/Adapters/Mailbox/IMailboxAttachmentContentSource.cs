namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

internal interface IMailboxAttachmentContentSource
{
    ValueTask<MailboxAttachmentContentResult> FetchAttachmentContentAsync(
        MailboxAttachmentContentRequest request,
        CancellationToken cancellationToken = default);
}
