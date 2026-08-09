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
    private volatile bool _lastProbeAvailable;

    public bool IsAvailable => _lastProbeAvailable;

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
            _lastProbeAvailable = true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not OperationCanceledException)
        {
            if (DaprWorkflowDuplicateInstanceDetector.IsDuplicateInstance(ex))
            {
                _lastProbeAvailable = true;
                _metrics.RecordWorkflowLifecycle(
                    request.TenantId,
                    "duplicate-schedule-replay",
                    CorrectionPropagationWorkflowFailureCodes.None);
                return;
            }

            _lastProbeAvailable = false;
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
            _lastProbeAvailable = true;
            return new CorrectionPropagationWorkflowRuntimeStatus(
                true,
                "available",
                CorrectionPropagationWorkflowFailureCodes.None,
                clock.UtcNow);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not OperationCanceledException)
        {
            _lastProbeAvailable = false;
            return new CorrectionPropagationWorkflowRuntimeStatus(
                false,
                CorrectionPropagationWorkflowStatuses.RuntimeUnavailable,
                CorrectionPropagationWorkflowFailureCodes.WorkflowUnavailable,
                clock.UtcNow);
        }
    }
}
