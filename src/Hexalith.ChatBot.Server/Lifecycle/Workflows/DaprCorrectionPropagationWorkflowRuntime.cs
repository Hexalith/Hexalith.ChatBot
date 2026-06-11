using Dapr.Workflow;

using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class DaprCorrectionPropagationWorkflowRuntime(
    DaprWorkflowClient workflowClient,
    ISystemClock clock) : ICorrectionPropagationWorkflowRuntime
{
    private const string ReadinessProbeInstanceId = "chatbot:correction-propagation:readiness-probe";
    private volatile bool _lastProbeAvailable = true;

    public bool IsAvailable => _lastProbeAvailable;

    public async ValueTask ScheduleAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        _ = await workflowClient
            .ScheduleNewWorkflowAsync(nameof(CorrectionPropagationWorkflow), request.WorkflowInstanceId, request)
            .ConfigureAwait(false);
        _lastProbeAvailable = true;
    }

    public async ValueTask<CorrectionPropagationWorkflowRuntimeStatus> CheckAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            _ = await workflowClient.GetWorkflowStateAsync(ReadinessProbeInstanceId).ConfigureAwait(false);
            _lastProbeAvailable = true;
            return new CorrectionPropagationWorkflowRuntimeStatus(
                true,
                "available",
                CorrectionPropagationWorkflowFailureCodes.None,
                clock.UtcNow);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
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
