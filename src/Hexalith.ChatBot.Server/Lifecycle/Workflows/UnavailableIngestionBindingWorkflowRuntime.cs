namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Fail-closed default when durable ingestion binding is not configured.</summary>
internal sealed class UnavailableIngestionBindingWorkflowRuntime : IIngestionBindingWorkflowRuntime
{
    public bool IsAvailable => false;

    public ValueTask ScheduleAsync(IngestionBindingRequest request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("ingestion_binding_workflow_unavailable");
}
