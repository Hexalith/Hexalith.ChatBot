namespace Hexalith.ChatBot.Server.Adapters.Conversations;

internal sealed record ApprovedAiConversationAppendRequest(
    string TenantId,
    string ProjectId,
    string RequesterId,
    string ProposalId,
    string ApprovalId,
    string ExecutionId,
    string SourceMessageId,
    string? SourceConversationItemId,
    string CommandName,
    string CommandAllowlistVersion,
    string PolicySnapshotId,
    string CorrelationId,
    string AuditOperationId);
