using Dapr.Workflow;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Rejoins a single owner activity per failed generation on duplicate projection delivery.</summary>
internal sealed class ReviewRetryWorkflow : Workflow<WorkflowReviewRetry, bool>
{
    public override Task<bool> RunAsync(WorkflowContext context, WorkflowReviewRetry input)
        => context.CallActivityAsync<bool>(nameof(RejoinReviewOperationActivity), input,
            new WorkflowTaskOptions(new WorkflowRetryPolicy(5, TimeSpan.FromSeconds(2), backoffCoefficient: 2)));
}
