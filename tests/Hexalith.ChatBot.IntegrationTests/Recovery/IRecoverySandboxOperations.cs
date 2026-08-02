namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Granular operations used by the Tier-3 continuity runner. The live implementation performs Aspire commands and
/// real HTTP/Worker observations; this seam exists only in the integration-test assembly for deterministic tests.
/// </summary>
internal interface IRecoverySandboxOperations
{
    DateTimeOffset UtcNow { get; }

    ValueTask<RecoveryOperationCheckpoint> SeedCommittedOperationAsync(string tenantRef, string correlationId, CancellationToken cancellationToken);
    ValueTask StopEventStoreAsync(CancellationToken cancellationToken);
    ValueTask<RecoveryFaultObservation> ObserveEventStoreFaultAsync(string tenantRef, string correlationId, CancellationToken cancellationToken);
    ValueTask StartEventStoreAsync(CancellationToken cancellationToken);
    ValueTask<DateTimeOffset> WaitForEventStoreRecoveryAsync(CancellationToken cancellationToken);
    ValueTask<RecoveryEventStoreEndState> ReadEventStoreEndStateAsync(RecoveryOperationCheckpoint checkpoint, CancellationToken cancellationToken);

    /// <summary>Verifies and erases scenario state; returns whether cleanup genuinely completed (not a hardcoded true).</summary>
    ValueTask<bool> CleanupEventStoreScenarioAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Establishes a witnessed committed-before-outage bound for the M365/subscription drill so loss-path RPO is not
    /// measured from harness wall-clock start.
    /// </summary>
    ValueTask<RecoveryOperationCheckpoint> CheckpointSubscriptionCommittedBoundAsync(
        string tenantRef,
        string correlationId,
        CancellationToken cancellationToken);

    ValueTask ExpireSubscriptionAsync(string tenantRef, CancellationToken cancellationToken);
    ValueTask<RecoveryFaultObservation> ObserveSubscriptionFaultAsync(string tenantRef, string correlationId, CancellationToken cancellationToken);
    ValueTask RestoreSubscriptionAsync(string tenantRef, CancellationToken cancellationToken);
    ValueTask<RecoverySubscriptionEndState> ReconcileSubscriptionAsync(string tenantRef, string correlationId, CancellationToken cancellationToken);

    /// <summary>Verifies and erases scenario state; returns whether cleanup genuinely completed (not a hardcoded true).</summary>
    ValueTask<bool> CleanupSubscriptionScenarioAsync(string tenantRef, CancellationToken cancellationToken);
}
