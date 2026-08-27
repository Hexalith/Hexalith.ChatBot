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
    bool IsTerminal)
{
    /// <summary>Gets the EventStore aggregate that owns the exact generation lifecycle.</summary>
    public string? StateOwnerAggregateId { get; init; }

    /// <summary>Gets the authoritative persisted Started event version for Stop concurrency.</summary>
    public long? StartedSourceVersion { get; init; }

    /// <summary>Gets the server-derived deadline after which durable recovery should have produced a terminal fact.</summary>
    public DateTimeOffset? RecoveryDeadlineUtc { get; init; }
}
