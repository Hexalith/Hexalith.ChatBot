using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>An immutable canonical event with trusted actor, server UTC and catalog reason facts.</summary>
/// <param name="Revision">The next canonical revision.</param>
/// <param name="State">The resulting execution state.</param>
/// <param name="ProjectionState">The separate projection state.</param>
/// <param name="ActorId">The trusted transition actor.</param>
/// <param name="OccurredAt">The server UTC transition time.</param>
/// <param name="ReasonCode">The catalog-owned safe reason.</param>
/// <param name="CorrelationId">The transition correlation identity.</param>
/// <param name="CommittedRevision">The proven owner commit revision.</param>
/// <param name="PreEffectFailure">Whether failure is proven to precede any effect.</param>
internal sealed record CommandOperationTransition(
    long Revision,
    CommandExecutionState State,
    CommandProjectionState ProjectionState,
    string ActorId,
    DateTimeOffset OccurredAt,
    string ReasonCode,
    string CorrelationId,
    long? CommittedRevision = null,
    bool PreEffectFailure = false);
