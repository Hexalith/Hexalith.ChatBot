using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class UnavailableCorrectionPropagationWorkflowRuntime(ISystemClock clock)
    : ICorrectionPropagationWorkflowRuntime
{
    public bool IsAvailable => false;

    public ValueTask ScheduleAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken)
        => throw new InvalidOperationException(CorrectionPropagationWorkflowFailureCodes.WorkflowUnavailable);

    public ValueTask<CorrectionPropagationWorkflowRuntimeStatus> CheckAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new CorrectionPropagationWorkflowRuntimeStatus(
            false,
            CorrectionPropagationWorkflowStatuses.RuntimeUnavailable,
            CorrectionPropagationWorkflowFailureCodes.WorkflowUnavailable,
            clock.UtcNow));
    }
}
