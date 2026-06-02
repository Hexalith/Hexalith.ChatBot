using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record PublishedMailboxIntakeEvent(
    [property: JsonPropertyName("tenantId")] string? TenantId,
    [property: JsonPropertyName("domain")] string? Domain,
    [property: JsonPropertyName("eventTypeName")] string? EventTypeName,
    [property: JsonPropertyName("sequenceNumber")] long SequenceNumber,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("intakeId")] string? IntakeId,
    [property: JsonPropertyName("providerMessageId")] string? ProviderMessageId,
    [property: JsonPropertyName("internetMessageId")] string? InternetMessageId,
    [property: JsonPropertyName("conversationId")] string? ConversationId,
    [property: JsonPropertyName("threadId")] string? ThreadId,
    [property: JsonPropertyName("mailboxId")] string? MailboxId,
    [property: JsonPropertyName("sender")] MailboxParticipantIdentity? Sender,
    [property: JsonPropertyName("recipients")] IReadOnlyList<MailboxRecipientIdentity>? Recipients,
    [property: JsonPropertyName("receivedAtUtc")] DateTimeOffset ReceivedAtUtc,
    [property: JsonPropertyName("sentAtUtc")] DateTimeOffset? SentAtUtc,
    [property: JsonPropertyName("createdAtUtc")] DateTimeOffset? CreatedAtUtc,
    [property: JsonPropertyName("attachmentReferences")] IReadOnlyList<MailboxAttachmentReference>? AttachmentReferences,
    [property: JsonPropertyName("sourceTimezone")] string? SourceTimezone,
    [property: JsonPropertyName("sourceContext")] string? SourceContext,
    [property: JsonPropertyName("sourceProvenance")] string? SourceProvenance,
    [property: JsonPropertyName("derivationKernelVersion")] string? DerivationKernelVersion,
    [property: JsonPropertyName("redactionState")] string? RedactionState,
    [property: JsonPropertyName("retentionClass")] string? RetentionClass,
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("authenticity")] MailboxAuthenticityMetadata? Authenticity = null,
    [property: JsonPropertyName("delegatedSender")] MailboxDelegatedSenderSnapshot? DelegatedSender = null,
    [property: JsonPropertyName("externalSender")] MailboxExternalSenderPosture? ExternalSender = null);
