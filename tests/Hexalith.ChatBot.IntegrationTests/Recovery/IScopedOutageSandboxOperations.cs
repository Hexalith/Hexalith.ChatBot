using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Tier-3 operations used by the live scoped-outage driver without exposing arbitrary fault authority.</summary>
internal interface IScopedOutageSandboxOperations
{
    /// <summary>Gets the current UTC time.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Captures a non-zero affected-operation and independent-control checkpoint.</summary>
    ValueTask CheckpointAsync(string dependency, string tenantRef, string correlationId, CancellationToken cancellationToken);

    /// <summary>Faults one closed dependency.</summary>
    ValueTask FaultAsync(string dependency, string tenantRef, CancellationToken cancellationToken);

    /// <summary>Observes the fault through its dependency client or component path.</summary>
    ValueTask<ScopedOutageFaultObservation> ObserveFaultAsync(
        string dependency,
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Restores one closed dependency. Returns whether a cross-tenant effect was detected before restoration
    /// cleared the affected dependency's effect ledger — restoration must not be the last word on tenant isolation,
    /// since a leak that happened during the fault window would otherwise be erased before <see cref="VerifyRecoveryAsync"/>
    /// could observe it.
    /// </summary>
    ValueTask<bool> RestoreAsync(string dependency, string tenantRef, CancellationToken cancellationToken);

    /// <summary>Verifies affected and control end-state after restoration.</summary>
    ValueTask<ScopedOutageRecoveryEndState> VerifyRecoveryAsync(
        string dependency,
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken);

    /// <summary>Performs idempotent metadata-only cleanup; returns whether cleanup genuinely completed (not a hardcoded true).</summary>
    ValueTask<bool> CleanupAsync(string dependency, string tenantRef, CancellationToken cancellationToken);
}
