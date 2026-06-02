using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record CreateOutboundDraft(
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
    OutboundDraftContent GovernedContent,
    SenderAuthorityClass SenderAuthorityClass = SenderAuthorityClass.DraftOnly,
    bool HasM365SendPosture = false,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-draft.v1") : IChatBotCommand;
