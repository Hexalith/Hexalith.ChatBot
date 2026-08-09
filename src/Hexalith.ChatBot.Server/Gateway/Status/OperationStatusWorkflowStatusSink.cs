using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;

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
        CancellationToken cancellationToken)
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

        OperationStatusRecord updated = existing with
        {
            WorkflowInstanceId = request.WorkflowInstanceId,
            WorkflowStatus = workflowStatus,
            WorkflowRetryCount = workflowRetryCount,
            WorkflowLastFailureCode = string.IsNullOrWhiteSpace(workflowLastFailureCode)
                || string.Equals(workflowLastFailureCode, CorrectionPropagationWorkflowFailureCodes.None, StringComparison.Ordinal)
                ? null
                : workflowLastFailureCode,
            LastUpdatedAt = clock.UtcNow,
        };
        await statusStore.UpsertAsync(updated, cancellationToken).ConfigureAwait(false);
    }
}
