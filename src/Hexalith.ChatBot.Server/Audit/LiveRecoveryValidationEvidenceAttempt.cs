namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Metadata-only latest-attempt envelope consumed by the external live-recovery release evidence gate.
/// <para>
/// This record carries only what the run <b>observed</b>. The expected dataset set, the target-deviation blocking
/// policy, and the required driver mode deliberately live in <see cref="LiveRecoveryValidationGatePolicy"/> instead:
/// a threshold supplied by the run being judged is not a gate.
/// </para>
/// </summary>
internal sealed record LiveRecoveryValidationEvidenceAttempt(
    bool Enabled,
    string RunId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    bool LatestAttemptCompletedSuccessfully,
    IReadOnlyList<RecoveryValidationEvidenceManifest> Evidence,
    IReadOnlyDictionary<string, int> AlertsDeliveredByJob);
