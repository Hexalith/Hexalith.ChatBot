using Hexalith.ChatBot.Server.Gateway.Redaction;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The end-state an operation rebuilds to from the chain alone (Story 9.2, AC1). It is the structural, metadata-only
/// result the AC2 measurer diffs against the live projection — never raw item content. <see cref="ResourceId"/> is the
/// projection lookup key (the governed aggregate id the chain claims reached this state) and
/// <see cref="ProjectionRedactionState"/> is the structural token the projection must agree on.
/// </summary>
internal sealed record ReconstructedOperationState(
    string ResourceId,
    string Decision,
    string ReasonCode,
    string PolicySnapshotId,
    string StateTransition,
    string Outcome,
    string ProjectionRedactionState,
    string ResultingStateToken);
