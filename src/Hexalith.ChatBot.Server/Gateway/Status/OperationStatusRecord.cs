using System.Text.Json.Serialization;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway.Idempotency;

namespace Hexalith.ChatBot.Server.Gateway.Status;

// The generated enums (LifecycleState, and ChatBotMessageCode inside the prior outcome) are persisted in DAPR state as
// their stable [EnumMember] wire values rather than generation-dependent ordinals. Integer tokens are still accepted but
// interpreted with the current generated numbering, so they decode correctly only for records written by the same enum
// generation; records persisted before a renumbering misdecode (accepted while the product is pre-release).
internal sealed record OperationStatusRecord(
    string TenantId,
    string OperationId,
    string CommandId,
    string CorrelationId,
    [property: JsonConverter(typeof(EnumMemberWireValueJsonConverter<LifecycleState>))] LifecycleState LifecycleState,
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
    string? WorkflowLastFailureCode = null,
    string ReasonCode = ChatBotMessageCodes.CommandAccepted,
    [property: JsonConverter(typeof(EnumMemberWireValueObjectJsonConverter<CommandSubmissionResponse>))] CommandSubmissionResponse? PriorOutcome = null)
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
        return string.IsNullOrWhiteSpace(response.OperationId) ? response.TaskId ?? response.CommandId : response.OperationId;
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
            response.AcceptedAt.ToUniversalTime(),
            lastUpdatedAt.ToUniversalTime(),
            operationClass,
            maxAttempts,
            NextRetryAt: null,
            DuplicateSafetyNote: null,
            OwnerRole: null,
            FailureReasonCode: null,
            TerminalReasonCode: null,
            PartialOutputCodes: [],
            OriginalOperationId: OperationIdFor(response),
            DuplicateAttemptCount: 0,
            ReasonCode: ChatBotMessageCodes.CommandAccepted);
    }
}
