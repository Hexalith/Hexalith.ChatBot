namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

internal sealed class UnavailableMailboxAttachmentContentSource : IMailboxAttachmentContentSource
{
    public ValueTask<MailboxAttachmentContentResult> FetchAttachmentContentAsync(
        MailboxAttachmentContentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(MailboxAttachmentContentResult.Retryable("mailbox_attachment_content_unavailable"));
    }
}
