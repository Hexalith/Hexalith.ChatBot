using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Association.Intake;

/// <summary>
/// Past-tense metadata-only event recording mailbox source identity for later association stories.
/// </summary>
public sealed record MailboxMessageIntakeCaptured(
    string IntakeId,
    string ProviderMessageId,
    string InternetMessageId,
    string ConversationId,
    string? ThreadId,
    string MailboxId,
    MailboxParticipantIdentity Sender,
    IReadOnlyList<MailboxRecipientIdentity> Recipients,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset? SentAtUtc,
    DateTimeOffset? CreatedAtUtc,
    IReadOnlyList<MailboxAttachmentReference> AttachmentReferences,
    string? SourceTimezone,
    string SourceContext,
    string SourceProvenance,
    string DerivationKernelVersion,
    string RedactionState,
    string RetentionClass,
    int SchemaVersion) : IEventPayload;
