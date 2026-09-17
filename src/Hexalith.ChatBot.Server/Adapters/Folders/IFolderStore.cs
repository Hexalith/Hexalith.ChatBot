using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

internal interface IFolderStore
{
    ValueTask<MailboxAttachmentStorageResult> StoreMailboxAttachmentAsync(
        StoreMailboxAttachmentRequest request,
        CancellationToken cancellationToken = default);
}
