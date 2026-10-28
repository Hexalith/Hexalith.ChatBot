using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>Immutable metadata retained from an operation audit entry for localized presentation.</summary>
/// <param name="Phase">The audit phase.</param>
/// <param name="Decision">The stable decision code.</param>
/// <param name="Outcome">The stable outcome code.</param>
/// <param name="SurfaceOrigin">The originating surface.</param>
/// <param name="CorrelationId">The opaque correlation identity.</param>
public sealed record OperationAuditEntryModel(
    AuditHistoryPhase Phase,
    string Decision,
    string Outcome,
    SurfaceOrigin SurfaceOrigin,
    string CorrelationId);
