using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;

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
    DateTimeOffset LastUpdatedAt)
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
        DateTimeOffset lastUpdatedAt)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new OperationStatusRecord(
            tenantId,
            OperationIdFor(response),
            response.CommandId,
            response.CorrelationId,
            response.LifecycleState,
            0,
            AcceptedProjectionPending,
            auditReconciliationRequired ? AuditReconciling : AuditCommitted,
            [ChatBotMessageNextActions.None],
            null,
            response.AcceptedAt,
            lastUpdatedAt);
    }
}
