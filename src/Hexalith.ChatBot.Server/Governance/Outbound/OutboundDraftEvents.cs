using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

public sealed record OutboundDraftCreated(
    string DraftId,
    string ProjectId,
    string RequesterId,
    string SourceActorId,
    string? SourceConversationId,
    string? SourceMessageId,
    string? SourceConversationItemId,
    IReadOnlyList<string> RecipientRefs,
    IReadOnlyList<string> ContextRefs,
    string PolicySnapshotId,
    string CorrelationId,
    SenderAuthorityClass SenderAuthorityClass,
    OutboundDraftContent GovernedContent,
    DateTimeOffset CreatedAtUtc,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-draft-created.v1") : IEventPayload;

public sealed record OutboundDraftCreationRejected(
    string DraftId,
    string ProjectId,
    string RequesterId,
    string ReasonCode,
    string CorrelationId,
    string? PolicySnapshotId = null,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input") : IRejectionEvent;
