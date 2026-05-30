using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record OperationStatus(
    string OperationId,
    string CommandId,
    string CorrelationId,
    LifecycleState LifecycleState,
    int RetryCount,
    OperationCompletionStatus CompletionStatus,
    OperationAuditStatus AuditStatus,
    OperationStatusPartialOutputs PartialOutputs,
    IReadOnlyList<string> SafeNextActions,
    string? TerminalReason,
    DateTimeOffset AcceptedAt,
    DateTimeOffset LastUpdatedAt);
