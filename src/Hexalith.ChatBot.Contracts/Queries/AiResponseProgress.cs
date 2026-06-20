using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only server-verified AI response progress exposed by the project conversation read model.
/// </summary>
public sealed record AiResponseProgress(
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    string CorrelationId,
    long SourceVersion,
    long Sequence,
    AiResponseProgressState State,
    AiResponseTerminalReason TerminalReason,
    string SafeNextAction,
    string RedactionState,
    string VisibilityState,
    bool IsTerminal);
