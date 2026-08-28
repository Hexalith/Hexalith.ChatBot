using Dapr.Workflow;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Dapr scheduler for deterministic association ingestion-binding workflows.</summary>
internal sealed class DaprIngestionBindingWorkflowRuntime(DaprWorkflowClient workflowClient)
    : IIngestionBindingWorkflowRuntime
{
    public bool IsAvailable => true;

    public async ValueTask ScheduleAsync(IngestionBindingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            _ = await workflowClient
                .ScheduleNewWorkflowAsync(
                    nameof(IngestionBindingWorkflow),
                    request.WorkflowInstanceId,
                    request,
                    startTime: null,
                    cancellation: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException and not OperationCanceledException)
        {
            if (!DaprWorkflowDuplicateInstanceDetector.IsDuplicateInstance(ex))
            {
                throw;
            }
        }
    }
}
