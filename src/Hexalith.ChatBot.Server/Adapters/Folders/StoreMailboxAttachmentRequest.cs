using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

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
