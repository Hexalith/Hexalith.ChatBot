namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Source identity captured from a controlled mailbox provider message.
/// </summary>
/// <param name="ProviderMessageId">Opaque provider message identifier used with mailbox id for intake idempotency.</param>
/// <param name="InternetMessageId">RFC internet message id when supplied by the provider.</param>
/// <param name="ConversationId">Provider conversation identifier.</param>
/// <param name="ThreadId">Provider thread identifier when distinct from conversation id.</param>
/// <param name="MailboxId">Controlled mailbox identifier.</param>
/// <param name="Sender">Provider sender identity.</param>
/// <param name="ReceivedAt">Provider received timestamp as a UTC DateTimeOffset.</param>
/// <param name="SentAt">Provider sent timestamp as a UTC DateTimeOffset when supplied.</param>
/// <param name="CreatedAt">Provider created timestamp as a UTC DateTimeOffset when supplied.</param>
/// <param name="SourceTimezone">Provider timezone context when supplied.</param>
/// <param name="SourceContext">Opaque provider context, such as Graph message or delta fetch.</param>
/// <param name="SourceSchemaVersion">Version of this source identity mapping.</param>
/// <param name="DelegatedSender">Optional delegated-send posture snapshot.</param>
/// <param name="ExternalSender">Optional external sender posture snapshot.</param>
public sealed record MailboxMessageSourceIdentity(
    string ProviderMessageId,
    string InternetMessageId,
    string ConversationId,
    string? ThreadId,
    string MailboxId,
    MailboxParticipantIdentity Sender,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? CreatedAt,
    string? SourceTimezone,
    string SourceContext,
    int SourceSchemaVersion,
    MailboxDelegatedSenderSnapshot? DelegatedSender = null,
    MailboxExternalSenderPosture? ExternalSender = null);
