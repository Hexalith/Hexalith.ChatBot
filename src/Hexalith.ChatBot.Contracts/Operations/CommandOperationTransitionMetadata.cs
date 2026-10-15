using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts;

/// <summary>Metadata-only canonical transition facts exposed by authorized status reads.</summary>
/// <param name="Revision">The canonical revision.</param>
/// <param name="State">The canonical execution state.</param>
/// <param name="ProjectionState">The separate projection state.</param>
/// <param name="ActorId">The trusted actor identity.</param>
/// <param name="OccurredAt">The server UTC transition time.</param>
/// <param name="ReasonCode">The catalog-owned safe reason.</param>
/// <param name="CorrelationId">The transition correlation identity.</param>
public sealed record CommandOperationTransitionMetadata(
    long Revision,
    CommandExecutionState State,
    CommandProjectionState ProjectionState,
    string ActorId,
    DateTimeOffset OccurredAt,
    string ReasonCode,
    string CorrelationId);
