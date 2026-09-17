using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ProjectConversationAttachmentStorageCandidate(
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
    ProjectConversationAttachmentStatus StorageStatus,
    string? FolderId,
    string? FileId,
    string RedactionState,
    long SourceVersion,
    string CorrelationId);
