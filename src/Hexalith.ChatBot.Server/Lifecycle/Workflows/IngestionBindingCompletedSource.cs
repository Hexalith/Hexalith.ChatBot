namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Validated terminal canonical identity returned by Memories ingestion status.</summary>
internal sealed record IngestionBindingCompletedSource(
    IngestionBindingRecordKind RecordKind,
    int Ordinal,
    string MemoryUnitId);
