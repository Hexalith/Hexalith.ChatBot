namespace Hexalith.ChatBot.Server.Audit;

/// <summary>Metadata-only observations used to evaluate one controlled subscription-notification loss path.</summary>
internal sealed record ControlledLossPathMeasurement(
    string TenantRef,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    string PreFaultRetainedRef,
    string PreFaultEventRef,
    long PreFaultSequence,
    DateTimeOffset PreFaultCommittedAtUtc,
    string RejectedCandidateRef,
    DateTimeOffset RejectedAtUtc,
    string PostRecoveryRetainedRef,
    string PostRecoveryEventRef,
    long PostRecoverySequence,
    DateTimeOffset PostRecoveryCommittedAtUtc,
    bool PreFaultRetained,
    bool CandidateRejected,
    bool CandidateAbsent,
    bool PostRecoveryRetained,
    bool TenantIsolationPreserved,
    bool UnauthorizedMutationAbsent,
    bool CleanupComplete);
