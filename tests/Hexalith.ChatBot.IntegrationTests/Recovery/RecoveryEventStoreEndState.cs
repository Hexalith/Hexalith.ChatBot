namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Post-recovery EventStore reconstruction and isolation assertions.</summary>
internal sealed record RecoveryEventStoreEndState(
    int ReconstructableCommittedCount,
    bool TenantIsolationPreserved,
    bool UnauthorizedMutationAbsent);
