namespace Hexalith.ChatBot.Server.Audit;

/// <summary>Observed execution facts threaded from a live driver into its retained evidence manifest.</summary>
internal sealed record RecoveryValidationExecutionAssertions(
    bool CleanupComplete,
    bool FaultObserved,
    bool RecoveryObserved,
    bool IndependentControlSucceeded,
    bool TenantIsolationPreserved,
    bool UnauthorizedMutationAbsent,
    bool StateReconstructable,
    bool ImmutableSourceOnly,
    bool MailboxReingestionAbsent);
