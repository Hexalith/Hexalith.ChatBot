using Dapr.Workflow;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>A deterministic workflow ID makes delivery and scheduling independently retryable.</summary>
internal sealed class ReviewRetryScheduler(DaprWorkflowClient client)
{
    public async Task StartOrJoinAsync(WorkflowReviewRetry input, CancellationToken token)
    {
        try
        {
            await client.ScheduleNewWorkflowAsync(nameof(ReviewRetryWorkflow), input.InstanceId, input, startTime: null, cancellation: token).ConfigureAwait(false);
        }
        catch (Exception ex) when (DaprWorkflowDuplicateInstanceDetector.IsDuplicateInstance(ex))
        {
            // Its owner retains the original workflow input and progress.
        }
    }
}
