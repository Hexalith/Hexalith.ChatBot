using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record ApprovedAiActionExecutionRecord(
    string ExecutionId,
    string ProposalId,
    string ApprovalId,
    string CommandName,
    string CommandAllowlistVersion,
    string Outcome,
    DateTimeOffset ExecutedAtUtc,
    string AuditOperationId,
    string AuditStatus,
    string CorrelationId,
    string GeneratedContentVisibility,
    string SafeNextAction,
    string? FailureCode = null,
    string? Retryability = null,
    string RedactionState = ChatBotDetailVisibility.MetadataOnly,
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.approved-ai-action-execution-record.v1");
