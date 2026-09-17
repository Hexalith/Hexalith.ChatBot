using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal sealed record AttachmentScanRequest(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string ProviderAttachmentId,
    int Ordinal,
    string? SafeDisplayName,
    string? ContentType,
    long? SizeInBytes,
    ReadOnlyMemory<byte> Content,
    string? ContentHashReference,
    long SourceVersion,
    string CorrelationId);
