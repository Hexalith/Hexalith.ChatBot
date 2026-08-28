namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Metadata-only terminal result of the ChatBot ingestion-binding workflow.</summary>
internal sealed record IngestionBindingWorkflowResult(
    string Status,
    string PriorCaseId,
    int MemoryUnitCount);
