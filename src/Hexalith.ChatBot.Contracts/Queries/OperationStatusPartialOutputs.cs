namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record OperationStatusPartialOutputs(
    DateTimeOffset AcceptedAt,
    OperationCompletionStatus CompletionStatus,
    OperationAuditStatus AuditStatus);
