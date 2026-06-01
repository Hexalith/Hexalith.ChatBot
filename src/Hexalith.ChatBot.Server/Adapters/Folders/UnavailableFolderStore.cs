using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

internal sealed class UnavailableFolderStore : IFolderStore
{
    public ValueTask<MailboxAttachmentStorageResult> StoreMailboxAttachmentAsync(
        StoreMailboxAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(MailboxAttachmentStorageResult.Failed(new AttachmentStorageFailure(
            ProjectConversationAttachmentStatus.Retryable,
            "not-evaluated",
            "retryable",
            "not-eligible",
            "folders_store_unavailable")));
    }
}
