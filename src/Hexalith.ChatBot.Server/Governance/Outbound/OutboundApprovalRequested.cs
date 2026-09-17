using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

public sealed record OutboundApprovalRequested(
    string ApprovalId,
    string DraftId,
    string ProjectId,
    string RequesterId,
    string RequesterActorType,
    string? SourceConversationId,
    string? SourceMessageId,
    string? SourceConversationItemId,
    IReadOnlyList<string> RecipientRefs,
    IReadOnlyList<string> ContextRefs,
    string PolicySnapshotId,
    string PolicySnapshotVisibility,
    string CommandName,
    string CommandAllowlistVersion,
    OutboundApprovalContentSnapshot ContentSnapshot,
    SenderAuthorityClass SenderAuthorityClass,
    ApprovalEvidenceFreshness EvidenceFreshness,
    string ExpectedPostStateRedactionState,
    long ExpectedDraftSourceVersion,
    long SourceVersion,
    DateTimeOffset RequestedAtUtc,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-approval-requested.v1",
    SenderAuthorityClassificationResult? AuthorityResult = null) : IEventPayload;
