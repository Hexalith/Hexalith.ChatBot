namespace Hexalith.ChatBot.Workers.Mailbox;

public sealed record GraphMailboxAttachment(
    string ProviderAttachmentId,
    string? Name,
    string? ContentType,
    long? SizeInBytes);
