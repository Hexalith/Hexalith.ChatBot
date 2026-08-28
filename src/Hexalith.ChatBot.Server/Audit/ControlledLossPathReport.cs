namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Retained metadata-only evidence for a deliberately rejected subscription notification surrounded by two
/// authoritative EventStore commits. It is separate from ordinary no-loss continuity evidence by design.
/// </summary>
internal sealed record ControlledLossPathReport(
    string TenantRef,
    string Scenario,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    TimeSpan MeasuredRpo,
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
    bool CleanupComplete,
    string Verdict,
    IReadOnlyList<string> Deviations,
    string CorrelationId,
    string ReasonCode)
{
    /// <summary>The single closed scenario retained by the controlled-loss evidence job.</summary>
    public const string SubscriptionNotificationRejectionScenario = "subscription-notification-rejection";

    /// <summary>The reason emitted for a fully valid, measured controlled-loss run.</summary>
    public const string CompletedReasonCode = "controlled_loss_path_completed";

    /// <summary>The reason emitted when valid authoritative bounds exceed the canonical RPO target.</summary>
    public const string TargetMissedReasonCode = "controlled_loss_path_target_missed";

    /// <summary>The reason emitted when authoritative bounds or safety invariants are invalid.</summary>
    public const string UnmeasurableReasonCode = "controlled_loss_path_unmeasurable";
}
