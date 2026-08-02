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
/// <param name="ConfiguredProjectionDatasets">The dataset refs the rebuild job must cover, from the release path.</param>
/// <param name="TargetDeviationsBlockRelease">Whether a measurable target miss blocks, decided by the release path.</param>
/// <param name="RequiredDriverMode">The driver-mode token evidence must carry to count as a live run.</param>
/// <param name="MaximumEvidenceAge">How old evidence may be before it is stale.</param>
/// <param name="ExpectedDatasetVersion">
/// The dataset version the release path expects, or <see langword="null"/> to leave it unpinned. Pinning the dataset
/// <i>name</i> alone left the run free to declare how much it had exercised: shrinking the baseline to one record and
/// declaring a matching volume kept every manifest mutually coherent and passed the gate.
/// </param>
/// <param name="MinimumDatasetVolume">
/// The smallest dataset volume the release path accepts as meaningful coverage. Zero leaves it unpinned.
/// </param>
/// <param name="RequiredRepositoryCommit">
/// The commit the evidence must be attributed to, or <see langword="null"/> to leave it unpinned. Cross-manifest
/// coherence proved only that the manifests agreed with each other, not that they described the released tree.
/// </param>
/// <param name="MaximumMeasurableRecoveryCeilingSeconds">
/// The largest measurable-recovery ceiling the release path believes the lane can honour, or zero to leave it
/// unpinned. Without it a manifest could declare an inflated ceiling and suppress the claim-limitation disclosure.
/// </param>
internal sealed record LiveRecoveryValidationGatePolicy(
    IReadOnlyList<string> ConfiguredProjectionDatasets,
    bool TargetDeviationsBlockRelease,
    string RequiredDriverMode,
    TimeSpan MaximumEvidenceAge,
    string? ExpectedDatasetVersion = null,
    int MinimumDatasetVolume = 0,
    string? RequiredRepositoryCommit = null,
    double MaximumMeasurableRecoveryCeilingSeconds = 0);
