using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway.Idempotency;

namespace Hexalith.ChatBot.Server.Gateway.Status;

internal sealed record OperationStatusRecord(
    string TenantId,
    string OperationId,
    string CommandId,
    string CorrelationId,
    LifecycleState LifecycleState,
    int RetryCount,
    string CompletionStatus,
    string AuditStatus,
    string[] SafeNextActions,
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
    string[]? PartialOutputCodes = null,
    string? OriginalOperationId = null,
    int DuplicateAttemptCount = 0,
    string? WorkflowInstanceId = null,
    string? WorkflowStatus = null,
    int WorkflowRetryCount = 0,
    string? WorkflowLastFailureCode = null)
{
    public const string AcceptedProjectionPending = "accepted-projection-pending";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string AuditCommitted = "committed";
    public const string AuditReconciling = "reconciling";

    /// <summary>
    /// Resolves the stable operation identity for a submission response: the supplied task id when present,
    /// otherwise the command id. Keeping this in one place guarantees the accept and idempotent-replay paths
    /// key the same record.
    /// </summary>
    public static string OperationIdFor(CommandSubmissionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.TaskId ?? response.CommandId;
    }

    public static OperationStatusRecord Accepted(
        string tenantId,
        CommandSubmissionResponse response,
        bool auditReconciliationRequired,
        DateTimeOffset lastUpdatedAt,
        string operationClass = "command-execution")
    {
        ArgumentNullException.ThrowIfNull(response);

        int retryCount = string.Equals(operationClass, CoarseIdempotencyOperationClass.Retry.Code, StringComparison.Ordinal) ? 1 : 0;
        int maxAttempts = string.Equals(operationClass, CoarseIdempotencyOperationClass.Retry.Code, StringComparison.Ordinal) ? 5 : 1;

        return new OperationStatusRecord(
            tenantId,
            OperationIdFor(response),
            response.CommandId,
            response.CorrelationId,
            response.LifecycleState,
            retryCount,
            AcceptedProjectionPending,
            auditReconciliationRequired ? AuditReconciling : AuditCommitted,
            [ChatBotMessageNextActions.None],
            null,
            response.AcceptedAt,
            lastUpdatedAt,
            operationClass,
            maxAttempts,
            NextRetryAt: null,
            DuplicateSafetyNote: null,
            OwnerRole: null,
            FailureReasonCode: null,
            TerminalReasonCode: null,
            PartialOutputCodes: [],
            OriginalOperationId: OperationIdFor(response),
            DuplicateAttemptCount: 0);
    }
}
