namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The release policy the live-recovery evidence gate is evaluated under, supplied by the release path rather than by
/// the run being judged.
/// <para>
/// Every member here was previously a field of <see cref="LiveRecoveryValidationEvidenceAttempt"/>, i.e. part of the
/// envelope the producing run writes about itself. A run that declares its own expected dataset set, its own
/// target-deviation blocking policy, and its own driver mode is not being gated — it is grading its own homework. The
/// gate now takes them from the configured release path instead.
/// </para>
/// </summary>
internal sealed record LiveRecoveryValidationGatePolicy(
    IReadOnlyList<string> ConfiguredProjectionDatasets,
    bool TargetDeviationsBlockRelease,
    string RequiredDriverMode,
    TimeSpan MaximumEvidenceAge);
