using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record ApprovedAiActionExecutionRejected(
    string ExecutionId,
    string ProposalId,
    string ApprovalId,
    string ProjectId,
    string TaskIntentId,
    string SourceMessageId,
    string? SourceConversationItemId,
    string RequesterId,
    string CommandName,
    string CommandAllowlistVersion,
    string ReasonCode,
    long? ExpectedApprovalSourceVersion,
    string CorrelationId,
    string? PolicySnapshotId = null,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input") : IRejectionEvent;
