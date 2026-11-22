using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

internal interface IFolderStore
{
    bool NeedsSubmissionFence => false;
    ValueTask<MailboxAttachmentStorageResult> ConfirmMailboxAttachmentAsync(StoreMailboxAttachmentRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
            ProjectConversationAttachmentStatus.Retryable, "not-evaluated", "awaiting-confirmation", "not-eligible", "attachment_confirmation_pending")));

    ValueTask<MailboxAttachmentStorageResult> StoreMailboxAttachmentAsync(
        StoreMailboxAttachmentRequest request,
        CancellationToken cancellationToken = default);
}
