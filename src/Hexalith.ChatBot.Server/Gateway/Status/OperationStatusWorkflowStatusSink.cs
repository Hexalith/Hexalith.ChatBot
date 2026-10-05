using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Gateway.Status;

internal sealed class OperationStatusWorkflowStatusSink(
    IOperationStatusStore statusStore,
    ISystemClock clock) : ICorrectionPropagationWorkflowStatusSink
{
    public async ValueTask ReportAsync(
        CorrectionPropagationRequest request,
        string workflowStatus,
        int workflowRetryCount,
        string? workflowLastFailureCode,
        CancellationToken cancellationToken,
        DateTimeOffset? retryDueAt = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.OperationId))
        {
            return;
        }

        OperationStatusRecord? existing = await statusStore
            .TryGetAsync(request.TenantId, request.OperationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        string? currentFailure = string.IsNullOrWhiteSpace(workflowLastFailureCode)
            || string.Equals(workflowLastFailureCode, CorrectionPropagationWorkflowFailureCodes.None, StringComparison.Ordinal)
            ? null
            : workflowLastFailureCode;
        bool delayed = string.Equals(workflowStatus, CorrectionPropagationWorkflowStatuses.Delayed, StringComparison.Ordinal);
        bool failed = string.Equals(workflowStatus, CorrectionPropagationWorkflowStatuses.Failed, StringComparison.Ordinal);
        bool retrying = string.Equals(workflowStatus, CorrectionPropagationWorkflowStatuses.Retrying, StringComparison.Ordinal);
        bool completed = string.Equals(workflowStatus, CorrectionPropagationWorkflowStatuses.Completed, StringComparison.Ordinal);
        DateTimeOffset now = clock.UtcNow;
        string reason = failed ? ChatBotMessageCodes.AssociationCorrectionPropagationFailed
            : currentFailure is not null && ChatBotMessageCodes.All.Contains(currentFailure)
                ? currentFailure
            : delayed ? ChatBotMessageCodes.AssociationCorrectionPropagationDelayed
            : completed
                ? ChatBotMessageCodes.AssociationCorrectionPropagationComplete
                : ChatBotMessageCodes.AssociationCorrectionPropagationPending;
        OperationStatusRecord updated = existing with
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            WorkflowStatus = workflowStatus,
            WorkflowRetryCount = workflowRetryCount,
            WorkflowLastFailureCode = currentFailure,
            FailureReasonCode = currentFailure,
            TerminalReasonCode = failed ? ChatBotMessageCodes.AssociationCorrectionPropagationFailed : null,
            ReasonCode = reason,
            SafeNextActions = [ChatBotMessageCatalog.Resolve(reason).NextAction],
            CompletionStatus = failed ? OperationStatusRecord.Failed
                : completed ? OperationStatusRecord.Completed
                : existing.CompletionStatus == OperationStatusRecord.Failed
                    ? OperationStatusRecord.AcceptedProjectionPending
                    : existing.CompletionStatus,
            MaxAttempts = retrying || failed ? Math.Max(existing.MaxAttempts, 5) : existing.MaxAttempts,
            RetryCount = retrying || failed ? workflowRetryCount : existing.RetryCount,
            NextRetryAt = retrying ? retryDueAt : null,
            LastUpdatedAt = now,
        };
        await statusStore.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }
}
