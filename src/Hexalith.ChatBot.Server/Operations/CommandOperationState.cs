using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>The immutable fold of canonical operation events; this model adds no persistence path.</summary>
/// <param name="Identity">The immutable original metadata.</param>
/// <param name="CanonicalInputHash">The canonical command-input fingerprint.</param>
/// <param name="Concurrency">The immutable owner revision guard and decision slot.</param>
/// <param name="Transitions">The ordered immutable event sequence.</param>
/// <param name="Attempt">The one-based attempt number.</param>
/// <param name="MaxAttempts">The attempt ceiling; runtime policy approval remains required.</param>
/// <param name="PriorOutcome">The retained predecessor outcome for a retry.</param>
internal sealed record CommandOperationState(
    CommandOperationIdentity Identity,
    string CanonicalInputHash,
    CommandOperationConcurrency Concurrency,
    IReadOnlyList<CommandOperationTransition> Transitions,
    int Attempt = 1,
    int MaxAttempts = 1,
    CommandOperationPriorOutcome? PriorOutcome = null)
{
    /// <summary>Gets the folded canonical revision.</summary>
    public long Revision => Transitions[^1].Revision;
    /// <summary>Gets the folded execution state.</summary>
    public CommandExecutionState ExecutionState => Transitions[^1].State;
    /// <summary>Gets the separate folded projection state.</summary>
    public CommandProjectionState ProjectionState => Transitions[^1].ProjectionState;
    /// <summary>Gets the authoritative committed revision, never inferred from SDK admission.</summary>
    public long? CommittedRevision => Transitions[^1].CommittedRevision;
    /// <summary>Gets whether the original attempt has a retained terminal outcome.</summary>
    public bool IsTerminal => ExecutionState is CommandExecutionState.Committed or CommandExecutionState.Rejected or CommandExecutionState.Failed;
    /// <summary>Gets retry eligibility in this pure contract model. Runtime policy approval is still required.</summary>
    public bool RetryEligible => ExecutionState == CommandExecutionState.Failed && Transitions[^1].PreEffectFailure && CommittedRevision is null && Attempt < MaxAttempts;
}
