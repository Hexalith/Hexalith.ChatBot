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
/// <para>
/// <see cref="ForRelease"/> is the constructor a <b>REQUIRED/release</b> path must use: it fails fast when
/// <see cref="ExpectedDatasetVersion"/>, <see cref="MinimumDatasetVolume"/>,
/// <see cref="MaximumMeasurableRecoveryCeilingSeconds"/>, or <see cref="RequiredRepositoryCommit"/> is left unpinned,
/// because an unpinned anchor on a required gate run is a configuration mistake, not a legitimate choice. The record
/// constructor's own unpinned-by-default parameters remain available for unit tests that deliberately exercise the
/// unpinned branches.
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
/// The smallest configured baseline-corpus volume the release path accepts. Zero leaves it unpinned. Per-scenario
/// <see cref="RecoveryValidationEvidenceManifest.DatasetVolume"/> separately reports how much of that corpus a driver
/// actually exercised.
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
    double MaximumMeasurableRecoveryCeilingSeconds = 0)
{
    /// <summary>
    /// Builds the policy for a <b>REQUIRED/release</b> gate run, where an unpinned anchor is a configuration mistake
    /// rather than a legitimate "leave it unpinned" choice. The record constructor's own defaults
    /// (<see langword="null"/>/<c>0</c>, meaning "unpinned") stay as-is for unit tests that deliberately exercise the
    /// unpinned branches; this factory instead fails fast so a release path can never silently construct a policy
    /// that does not actually anchor dataset version, configured corpus volume, measurable ceiling, or repository commit.
    /// </summary>
    /// <param name="configuredProjectionDatasets">The dataset refs the rebuild job must cover.</param>
    /// <param name="targetDeviationsBlockRelease">Whether a measurable target miss blocks.</param>
    /// <param name="requiredDriverMode">The driver-mode token evidence must carry to count as a live run.</param>
    /// <param name="maximumEvidenceAge">How old evidence may be before it is stale.</param>
    /// <param name="expectedDatasetVersion">The dataset version the release path expects. Required (non-null/non-whitespace).</param>
    /// <param name="minimumDatasetVolume">The smallest configured baseline-corpus volume the release path accepts. Required to be positive.</param>
    /// <param name="maximumMeasurableRecoveryCeilingSeconds">The largest measurable-recovery ceiling the release path believes the lane can honour. Required to be finite and positive.</param>
    /// <param name="requiredRepositoryCommit">The commit the evidence must be attributed to. Required (non-null/non-whitespace).</param>
    /// <returns>A policy with every release-required anchor validated and pinned.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="expectedDatasetVersion"/> or <paramref name="requiredRepositoryCommit"/> is null or whitespace.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="minimumDatasetVolume"/> is not positive, or <paramref name="maximumMeasurableRecoveryCeilingSeconds"/>
    /// is not a finite positive number.
    /// </exception>
    public static LiveRecoveryValidationGatePolicy ForRelease(
        IReadOnlyList<string> configuredProjectionDatasets,
        bool targetDeviationsBlockRelease,
        string requiredDriverMode,
        TimeSpan maximumEvidenceAge,
        string? expectedDatasetVersion,
        int minimumDatasetVolume,
        double maximumMeasurableRecoveryCeilingSeconds,
        string? requiredRepositoryCommit)
    {
        if (string.IsNullOrWhiteSpace(expectedDatasetVersion))
        {
            throw new ArgumentException(
                "A release gate policy must pin an expected dataset version.",
                nameof(expectedDatasetVersion));
        }

        if (minimumDatasetVolume <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumDatasetVolume),
                minimumDatasetVolume,
                "A release gate policy must pin a positive minimum dataset volume.");
        }

        if (!double.IsFinite(maximumMeasurableRecoveryCeilingSeconds) || maximumMeasurableRecoveryCeilingSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumMeasurableRecoveryCeilingSeconds),
                maximumMeasurableRecoveryCeilingSeconds,
                "A release gate policy must pin a finite, positive maximum measurable recovery ceiling.");
        }

        if (string.IsNullOrWhiteSpace(requiredRepositoryCommit))
        {
            throw new ArgumentException(
                "A release gate policy must pin the repository commit the evidence is attributed to.",
                nameof(requiredRepositoryCommit));
        }

        return new LiveRecoveryValidationGatePolicy(
            configuredProjectionDatasets,
            targetDeviationsBlockRelease,
            requiredDriverMode,
            maximumEvidenceAge,
            expectedDatasetVersion,
            minimumDatasetVolume,
            requiredRepositoryCommit,
            maximumMeasurableRecoveryCeilingSeconds);
    }
}
