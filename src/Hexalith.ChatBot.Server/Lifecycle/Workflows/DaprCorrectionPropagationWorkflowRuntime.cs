using Dapr.Workflow;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class DaprCorrectionPropagationWorkflowRuntime(
    DaprWorkflowClient workflowClient,
    ISystemClock clock,
    IChatBotMetrics? metrics = null) : ICorrectionPropagationWorkflowRuntime
{
    private const string ReadinessProbeInstanceId = "chatbot:correction-propagation:readiness-probe";
    private readonly IChatBotMetrics _metrics = metrics ?? NullChatBotMetrics.Instance;
    public bool IsAvailable => true;

    public async ValueTask ScheduleAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _ = await workflowClient
                .ScheduleNewWorkflowAsync(
                    nameof(CorrectionPropagationWorkflow),
                    request.WorkflowInstanceId,
                    request,
                    startTime: null,
                    cancellation: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not OperationCanceledException)
        {
            if (DaprWorkflowDuplicateInstanceDetector.IsDuplicateInstance(ex))
            {
                _metrics.RecordWorkflowLifecycle(
                    request.TenantId,
                    "duplicate-schedule-replay",
                    CorrectionPropagationWorkflowFailureCodes.None);
                return;
            }

            throw;
        }
    }

    public async ValueTask<CorrectionPropagationWorkflowRuntimeStatus> CheckAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            _ = await workflowClient
                .GetWorkflowStateAsync(ReadinessProbeInstanceId, getInputsAndOutputs: false, cancellation: cancellationToken)
                .ConfigureAwait(false);
            return new CorrectionPropagationWorkflowRuntimeStatus(
                true,
                "available",
                CorrectionPropagationWorkflowFailureCodes.None,
                clock.UtcNow);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not OperationCanceledException)
        {
            return new CorrectionPropagationWorkflowRuntimeStatus(
                false,
                CorrectionPropagationWorkflowStatuses.RuntimeUnavailable,
                CorrectionPropagationWorkflowFailureCodes.WorkflowUnavailable,
                clock.UtcNow);
        }
    }
}
