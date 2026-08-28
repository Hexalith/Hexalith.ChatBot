namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Safe ChatBot-owned status returned through the sibling-context ingestion adapter.</summary>
internal sealed record IngestionBindingSourceStatus(
    string RuntimeStatus,
    string? MemoryUnitId,
    bool IsIndexed);
