using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

internal interface IFolderStore
{
    ValueTask<MailboxAttachmentStorageResult> StoreMailboxAttachmentAsync(
        StoreMailboxAttachmentRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed record StoreMailboxAttachmentRequest(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string MailboxId,
    string ProviderMessageId,
    string ProviderAttachmentId,
    int Ordinal,
    string? SafeDisplayName,
    string? ContentType,
    long? SizeInBytes,
    MailboxAttachmentContentResult Content,
    long SourceVersion,
    string CorrelationId);

internal sealed record StoredMailboxAttachmentReference(
    string FolderId,
    string FileId,
    string DuplicateState,
    string RetryState,
    string AiContextEligibility,
    IReadOnlyList<string> AllowedActions,
    string StorageOperationId,
    string IdempotencyKey);

internal sealed record AttachmentStorageFailure(
    ProjectConversationAttachmentStatus Status,
    string DuplicateState,
    string RetryState,
    string AiContextEligibility,
    string ReasonCode);

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
