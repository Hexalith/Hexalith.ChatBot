using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record RequestOutboundSendApproval(
    string ApprovalId,
    string DraftId,
    string ProjectId,
    string RequesterId,
    string? SourceConversationId,
    string? SourceMessageId,
    string? SourceConversationItemId,
    IReadOnlyList<string> RecipientRefs,
    IReadOnlyList<string> ContextRefs,
    string PolicySnapshotId,
    string PolicySnapshotVisibility,
    string CommandName,
    string CommandAllowlistVersion,
    string ExpectedPostStateRedactionState,
    OutboundApprovalContentSnapshot ContentSnapshot,
    SenderAuthorityClass SenderAuthorityClass,
    ApprovalEvidenceFreshness EvidenceFreshness,
    long ExpectedDraftSourceVersion,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-approval-request.v1") : IChatBotCommand;

