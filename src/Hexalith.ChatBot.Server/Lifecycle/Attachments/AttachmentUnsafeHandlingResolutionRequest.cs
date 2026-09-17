using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal sealed record AttachmentUnsafeHandlingResolutionRequest(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string ProviderMessageId,
    string ProviderAttachmentId,
    int Ordinal,
    long SourceVersion,
    string CorrelationId);
