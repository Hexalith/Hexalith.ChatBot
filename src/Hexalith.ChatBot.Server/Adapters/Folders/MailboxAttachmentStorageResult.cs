using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

internal sealed record MailboxAttachmentStorageResult(
    StoredMailboxAttachmentReference? Stored,
    AttachmentStorageFailure? Failure)
{
    public bool IsStored => Stored is not null;

    public static MailboxAttachmentStorageResult Succeeded(StoredMailboxAttachmentReference stored)
        => new(stored, null);

    public static MailboxAttachmentStorageResult Failed(AttachmentStorageFailure failure)
        => new(null, failure);
}
