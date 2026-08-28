namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Complete ordered ingestion result set submitted for atomic binding publication.</summary>
internal sealed record IngestionBindingFinalizeInput(
    IngestionBindingRequest Request,
    IngestionBindingResolvedContext Context,
    IReadOnlyList<IngestionBindingCompletedSource> CompletedSources);
