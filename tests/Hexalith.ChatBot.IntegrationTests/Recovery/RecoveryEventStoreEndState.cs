namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Post-recovery EventStore reconstruction and isolation assertions.</summary>
internal sealed record RecoveryEventStoreEndState(
    DateTimeOffset RecoveredAtUtc,
    int ReconstructableCommittedCount,
    bool TenantIsolationPreserved,
    bool UnauthorizedMutationAbsent);
