namespace Hexalith.ChatBot.Workers.Mailbox;

public sealed record GraphMailboxMessage(
    string MailboxId,
    string ProviderMessageId,
    string InternetMessageId,
    string ConversationId,
    string? ThreadId,
    GraphMailboxParticipant From,
    IReadOnlyList<GraphMailboxRecipient> Recipients,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? CreatedAt,
    string? SourceTimezone,
    IReadOnlyList<GraphMailboxAttachment> Attachments);
