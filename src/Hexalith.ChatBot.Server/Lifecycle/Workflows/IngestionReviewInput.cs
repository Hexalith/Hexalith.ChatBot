namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>The workflow's deterministic, metadata-only outcome for one ingestion phase.</summary>
internal sealed record IngestionReviewInput(IngestionBindingRequest Request, string Phase, int Attempt, string State, string Reason, string[]? IndexedUnits = null);
