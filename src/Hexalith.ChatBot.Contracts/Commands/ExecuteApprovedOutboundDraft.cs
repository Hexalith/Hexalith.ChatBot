using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record ExecuteApprovedOutboundDraft(
    string SendId,
    string ApprovalId,
    string DraftId,
    string ProjectId,
    string RequesterId,
    string SendActorId,
    string? SourceConversationId,
    string? SourceMessageId,
    string? SourceConversationItemId,
    IReadOnlyList<string> RecipientRefs,
    IReadOnlyList<string> ContextRefs,
    string PolicySnapshotId,
    string CommandName,
    string CommandAllowlistVersion,
    SenderAuthorityClass SenderAuthorityClass,
    ApprovalEvidenceFreshness EvidenceFreshness,
    long ExpectedApprovalSourceVersion,
    long ExpectedDraftSourceVersion,
    string CorrelationId,
    string AdapterMode = "approved",
    string AdapterStatus = "sent",
    string AdapterRef = "adapter:mailbox-outbound",
    SenderAuthorityClassificationResult? AuthorityResult = null,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-send.v1") : IChatBotCommand;
