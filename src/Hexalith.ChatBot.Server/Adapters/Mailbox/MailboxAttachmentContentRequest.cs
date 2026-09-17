namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

internal sealed record MailboxAttachmentContentRequest(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string MailboxId,
    string ProviderMessageId,
    string ProviderAttachmentId,
    int Ordinal,
    long SourceVersion,
    string CorrelationId);
