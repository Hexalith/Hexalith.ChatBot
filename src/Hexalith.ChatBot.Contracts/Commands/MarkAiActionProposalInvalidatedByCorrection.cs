namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record MarkAiActionProposalInvalidatedByCorrection(
    string ProjectId,
    string ProposalId,
    string? ApprovalId,
    string TaskIntentId,
    string SourceMessageId,
    string? SourceConversationItemId,
    string RequesterId,
    string AssociationId,
    string CorrectionId,
    string CorrectedEvidenceState,
    long EvidenceSnapshotSourceVersion,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.ai-action-proposal-invalidation.v1") : IChatBotCommand;
