using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record ExecuteApprovedAIAction(
    string ProjectId,
    string ProposalId,
    string ApprovalId,
    string TaskIntentId,
    string SourceMessageId,
    string RequesterId,
    string CommandName,
    string CommandAllowlistVersion,
    long ExpectedApprovalSourceVersion,
    long ExpectedProposalSourceVersion,
    string CorrelationId,
    string ExecutionId,
    string TransitionId,
    IReadOnlyList<string> SourceEvidenceReferences,
    IReadOnlyList<string> AffectedResourceReferences,
    IReadOnlyList<string> RecipientReferences,
    string? SourceConversationItemId = null,
    string? PolicySnapshotId = null,
    string ActionSummaryRedactionState = ChatBotDetailVisibility.MetadataOnly,
    bool CorrectedContextReady = true,
    ApprovedAiActionExecutionRecord? ExecutionRecord = null,
    string RedactionState = ChatBotDetailVisibility.MetadataOnly,
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.approved-ai-action-execution.v1") : IChatBotCommand;
