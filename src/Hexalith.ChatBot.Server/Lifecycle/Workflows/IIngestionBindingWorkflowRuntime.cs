namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Schedules the deterministic ingestion-binding workflow instance.</summary>
internal interface IIngestionBindingWorkflowRuntime
{
    bool IsAvailable { get; }

    ValueTask ScheduleAsync(IngestionBindingRequest request, CancellationToken cancellationToken);
}
