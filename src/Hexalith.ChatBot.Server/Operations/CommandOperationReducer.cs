using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts;
using Hexalith.ChatBot.Server.Gateway.Idempotency;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Pure canonical contract evaluation for synthetic evidence. Not registered with a live execution or persistence
/// service: A6/A13 owner/protection acceptance and retry-profile approval remain required before runtime use.
/// </summary>
internal static class CommandOperationReducer
{
    /// <summary>Creates an attempt or returns its exact stored fold for equivalent lifetime replay.</summary>
    public static CommandOperationResult Receive(CommandOperationIdentity identity, JsonElement input,
        long? expectedRevision, string? decisionSlotId = null, CommandOperationState? current = null,
        CommandOperationState? occupiedDecision = null, int maxAttempts = 1)
    {
        ArgumentNullException.ThrowIfNull(identity);
        // Validate guards independently of replay, before comparing semantic input.
        if (expectedRevision is null or < 0 || maxAttempts < 1 ||
            (decisionSlotId is not null && string.IsNullOrWhiteSpace(decisionSlotId)))
        {
            return new(current, CommandOperationConflict.InvalidConcurrency);
        }
        if (input.ValueKind != JsonValueKind.Object)
        {
            return new(current, CommandOperationConflict.OperationConflict);
        }
        string hash = CoarseIdempotencyCanonicalizer.HashCanonicalJson(input);
        if (current is not null)
        {
            bool equivalent = current.Identity.TenantId == identity.TenantId &&
                current.Identity.OperationId == identity.OperationId && current.Identity.CommandType == identity.CommandType && current.Identity.ActorId == identity.ActorId &&
                current.Identity.OperationClass == identity.OperationClass &&
                current.Identity.EquivalenceVersion == identity.EquivalenceVersion &&
                current.Identity.ResourceIds.SequenceEqual(identity.ResourceIds, StringComparer.Ordinal) &&
                current.Identity.PredecessorOperationId == identity.PredecessorOperationId &&
                current.CanonicalInputHash == hash && current.Concurrency.ExpectedRevision == expectedRevision &&
                current.Concurrency.DecisionSlotId == decisionSlotId;
            return equivalent ? new(current, IsReplay: true) : new(current, CommandOperationConflict.OperationConflict);
        }
        if (decisionSlotId is not null && occupiedDecision is not null &&
            occupiedDecision.Identity.TenantId == identity.TenantId && occupiedDecision.Concurrency.DecisionSlotId == decisionSlotId)
        {
            return new(null, CommandOperationConflict.DecisionConflict);
        }
        CommandOperationTransition received = new(0, CommandExecutionState.Received, CommandProjectionState.ProjectionPending,
            identity.ActorId, identity.CreatedAt, ChatBotMessageCodes.CommandAccepted, identity.CorrelationId);
        return new(new(identity, hash, new(expectedRevision, decisionSlotId), Array.AsReadOnly(new[] { received }), MaxAttempts: maxAttempts));
    }

    /// <summary>Evaluates and folds an execution transition at the expected canonical revision.</summary>
    public static CommandOperationResult Transition(CommandOperationState current, long? expectedRevision,
        CommandExecutionState target, string actorId, DateTimeOffset serverUtc, string reasonCode, string correlationId,
        long? committedRevision = null, bool preEffectFailure = false)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (expectedRevision is null or < 0)
        {
            return new(current, CommandOperationConflict.InvalidConcurrency);
        }
        if (expectedRevision != current.Revision)
        {
            return new(current, CommandOperationConflict.RevisionConflict);
        }
        return Apply(current, new(current.Revision + 1, target, current.ProjectionState, actorId,
            serverUtc.ToUniversalTime(), reasonCode, correlationId, committedRevision, preEffectFailure));
    }

    /// <summary>Folds projection progress without changing or reopening a committed execution outcome.</summary>
    public static CommandOperationResult Project(CommandOperationState current, long? expectedRevision,
        CommandProjectionState projection, string actorId, DateTimeOffset serverUtc, string reasonCode, string correlationId)
        => expectedRevision is null or < 0
            ? new(current, CommandOperationConflict.InvalidConcurrency)
            : expectedRevision != current.Revision ? new(current, CommandOperationConflict.RevisionConflict)
            : Apply(current, new(current.Revision + 1, current.ExecutionState, projection, actorId,
                serverUtc.ToUniversalTime(), reasonCode, correlationId, current.CommittedRevision));

    /// <summary>Replays one declared event; invalid or incompatible events leave the fold unchanged.</summary>
    public static CommandOperationResult Apply(CommandOperationState current, CommandOperationTransition transition)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(transition);
        if (current.Transitions.Contains(transition))
        {
            return new(current, IsReplay: true);
        }
        if (transition.Revision != current.Revision + 1)
        {
            return new(current, CommandOperationConflict.RevisionConflict);
        }
        bool projectionChange = transition.State == current.ExecutionState &&
            current.ExecutionState == CommandExecutionState.Committed &&
            transition.ProjectionState != current.ProjectionState &&
            (transition.ProjectionState == CommandProjectionState.Projected ||
                (current.ProjectionState == CommandProjectionState.ProjectionPending && transition.ProjectionState == CommandProjectionState.ProjectionLagging)) &&
            current.ProjectionState != CommandProjectionState.Projected &&
            transition.CommittedRevision == current.CommittedRevision;
        bool executionChange = !current.IsTerminal && transition.ProjectionState == current.ProjectionState &&
            ((current.ExecutionState == CommandExecutionState.Received && transition.State == CommandExecutionState.Admitted) ||
            (current.ExecutionState == CommandExecutionState.Admitted && transition.State is CommandExecutionState.Committed or CommandExecutionState.Rejected or CommandExecutionState.Failed));
        bool commitValid = transition.State == CommandExecutionState.Committed
            ? transition.CommittedRevision is >= 0 &&
                (projectionChange || (current.Concurrency.ExpectedRevision < long.MaxValue && transition.CommittedRevision == current.Concurrency.ExpectedRevision + 1))
            : transition.CommittedRevision is null;
        if ((!projectionChange && !executionChange) || !commitValid || !Enum.IsDefined(transition.State) ||
            !Enum.IsDefined(transition.ProjectionState) || !CommandOperationMetadataValidation.IsValidActorId(transition.ActorId) ||
            transition.OccurredAt.Offset != TimeSpan.Zero || transition.OccurredAt < current.Transitions[^1].OccurredAt ||
            !ChatBotIdentity.IsValidUlid(transition.CorrelationId) || !ChatBotMessageCatalog.IsKnown(transition.ReasonCode) ||
            transition.ReasonCode == ChatBotMessageCodes.GenericDenial ||
            (transition.PreEffectFailure && transition.State != CommandExecutionState.Failed))
        {
            return new(current, CommandOperationConflict.InvalidTransition);
        }
        return new(current with { Transitions = Array.AsReadOnly(current.Transitions.Append(transition).ToArray()) });
    }

    /// <summary>Creates a distinct predecessor-linked attempt only after an eligible failure before any effect.</summary>
    public static CommandOperationResult Retry(CommandOperationState previous, CommandOperationIdentity identity,
        JsonElement input, long? expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(identity);
        if (expectedRevision is null or < 0)
        {
            return new(previous, CommandOperationConflict.InvalidConcurrency);
        }
        if (!previous.RetryEligible || identity.CommandType != previous.Identity.CommandType ||
            identity.EquivalenceVersion != previous.Identity.EquivalenceVersion ||
            input.ValueKind != JsonValueKind.Object || CoarseIdempotencyCanonicalizer.HashCanonicalJson(input) != previous.CanonicalInputHash || identity.OperationId == previous.Identity.OperationId ||
            identity.PredecessorOperationId != previous.Identity.OperationId || identity.TenantId != previous.Identity.TenantId ||
            identity.ActorId != previous.Identity.ActorId || !identity.ResourceIds.SequenceEqual(previous.Identity.ResourceIds) ||
            identity.OperationClass != previous.Identity.OperationClass || identity.CreatedAt < previous.Transitions[^1].OccurredAt)
        {
            return new(previous, CommandOperationConflict.RetryNotEligible);
        }
        CommandOperationResult result = Receive(identity, input, expectedRevision, previous.Concurrency.DecisionSlotId, maxAttempts: previous.MaxAttempts);
        return result.Succeeded ? result with
        {
            State = result.State! with
            {
                Attempt = previous.Attempt + 1,
                PriorOutcome = new(previous.Identity.OperationId, previous.Identity.CorrelationId, previous.Identity.CreatedAt,
                    previous.ExecutionState, previous.Transitions[^1].ReasonCode, previous.CommittedRevision),
            },
        } : result;
    }
}
