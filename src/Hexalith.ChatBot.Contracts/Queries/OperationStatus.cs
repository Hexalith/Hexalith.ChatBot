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
    DateTimeOffset LastUpdatedAt,
    string OperationClass = "command-execution",
    int MaxAttempts = 1,
    DateTimeOffset? NextRetryAt = null,
    string? DuplicateSafetyNote = null,
    string? OwnerRole = null,
    string? FailureReasonCode = null,
    string? TerminalReasonCode = null,
    string? OriginalOperationId = null,
    int DuplicateAttemptCount = 0);
