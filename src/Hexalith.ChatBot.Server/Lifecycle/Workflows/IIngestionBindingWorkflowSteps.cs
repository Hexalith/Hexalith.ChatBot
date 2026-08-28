namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Replay-safe orchestration boundary for the ingestion-binding workflow.</summary>
internal interface IIngestionBindingWorkflowSteps
{
    Task<IngestionBindingResolvedContext> ResolveAsync(IngestionBindingRequest request);

    Task<IngestionBindingSourceOperation> StartAsync(IngestionBindingSourceRequest request);

    Task<IngestionBindingSourceStatus> GetStatusAsync(IngestionBindingSourceOperation operation);

    Task DelayAsync(TimeSpan delay);

    Task FinalizeAsync(IngestionBindingFinalizeInput input);
}
