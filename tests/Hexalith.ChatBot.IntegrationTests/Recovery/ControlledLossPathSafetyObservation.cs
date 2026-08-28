namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Post-restore retained-state, isolation, and unauthorized-mutation observations.</summary>
internal sealed record ControlledLossPathSafetyObservation(
    bool PreFaultRetained,
    bool CandidateAbsent,
    bool PostRecoveryRetained,
    bool TenantIsolationPreserved,
    bool UnauthorizedMutationAbsent);
