using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

public sealed record ApprovedAiActionExecutionStarted(
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
    long ExpectedApprovalSourceVersion,
    long ExpectedProposalSourceVersion,
    string PolicySnapshotId,
    string CorrelationId,
    DateTimeOffset StartedAtUtc,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.approved-ai-action-execution-started.v1") : IEventPayload;

public sealed record ApprovedAiActionExecutionSucceeded(
    ApprovedAiActionExecutionRecord Record,
    string ProjectId,
    string RequesterId,
    string SourceMessageId,
    string? SourceConversationItemId) : IEventPayload;

public sealed record ApprovedAiActionExecutionFailed(
    ApprovedAiActionExecutionRecord Record,
    string ProjectId,
    string RequesterId,
    string SourceMessageId,
    string? SourceConversationItemId) : IEventPayload;

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
