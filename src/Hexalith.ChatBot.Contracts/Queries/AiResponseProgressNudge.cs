using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only SignalR projection nudge for AI response progress. It is advisory and never carries generated content.
/// </summary>
public sealed record AiResponseProgressNudge(
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    string CorrelationId,
    long SourceVersion,
    long Sequence,
    AiResponseProgressState State,
    string RedactionState,
    string VisibilityState);
