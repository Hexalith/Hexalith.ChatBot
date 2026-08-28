namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Safe durable handle for one started Memories ingestion.</summary>
internal sealed record IngestionBindingSourceOperation(
    IngestionBindingSourceRequest Source,
    string InstanceId);
