using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Projects-authoritative case identity and projection-backed provider source metadata.</summary>
internal sealed record IngestionBindingResolvedContext(
    string PriorCaseId,
    ProjectConversationIngestionSource Source);
