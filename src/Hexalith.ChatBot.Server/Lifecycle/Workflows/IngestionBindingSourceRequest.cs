namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>One deterministic message or attachment ingestion request.</summary>
internal sealed record IngestionBindingSourceRequest(
    IngestionBindingRequest Request,
    IngestionBindingResolvedContext Context,
    IngestionBindingRecordKind RecordKind,
    int Ordinal,
    string? ProviderAttachmentId,
    string? ContentType);
