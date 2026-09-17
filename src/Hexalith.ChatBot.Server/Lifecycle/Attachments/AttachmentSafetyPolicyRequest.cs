using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal sealed record AttachmentSafetyPolicyRequest(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string ProviderMessageId,
    string ProviderAttachmentId,
    int Ordinal,
    string? SafeDisplayName,
    string? ContentType,
    long? SizeInBytes,
    ProjectConversationAttachmentStatus CurrentStorageStatus,
    string? FolderId,
    string? FileId,
    MailboxAttachmentContentResult Content,
    long SourceVersion,
    string CorrelationId,
    string? UnsafeHandling);
