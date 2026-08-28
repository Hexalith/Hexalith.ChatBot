namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Metadata-only execution-provenance manifest surrounding one canonical recovery report. The manifest never replaces
/// report verdict semantics and must contain only bounded tokens, numeric measurements, booleans, and artifact locators.
/// </summary>
internal sealed record RecoveryValidationEvidenceManifest
{
    /// <summary>
    /// The only <see cref="DriverMode"/> the release evidence gate accepts as a live run.
    /// <para>
    /// This token is <b>declared by the writing sink, not derived from the driver that produced the report</b>, so it
    /// is not by itself proof of a live run: it rejects a manifest that names a non-live mode, and nothing more. The
    /// claim that it is "the sole discriminator" against a scripted fake was false — the anti-fake weight is carried by
    /// <see cref="LiveRecoveryValidationGatePolicy.RequiredRepositoryCommit"/>,
    /// <see cref="LiveRecoveryValidationGatePolicy.ExpectedDatasetVersion"/> and
    /// <see cref="LiveRecoveryValidationGatePolicy.MinimumDatasetVolume"/>, which the release path supplies and the run
    /// cannot choose.
    /// </para>
    /// </summary>
    public const string LiveDriverMode = "aspire-tier3-live";

    public required string RunId { get; init; }
    public required string ScenarioId { get; init; }
    public required DateTimeOffset StartedAtUtc { get; init; }
    public required DateTimeOffset EndedAtUtc { get; init; }
    public required string RepositoryCommit { get; init; }
    public required string AppHostVersion { get; init; }
    public required string AspireVersion { get; init; }
    public required string DaprVersion { get; init; }
    public required string TopologyVersion { get; init; }
    public required string ConfigurationVersion { get; init; }
    public required string TenantRef { get; init; }
    public required string DatasetRef { get; init; }
    public required string DatasetVersion { get; init; }

    /// <summary>Gets the number of records in the configured baseline corpus.</summary>
    public required int ConfiguredDatasetVolume { get; init; }

    /// <summary>
    /// Gets the number of configured dataset records or resources this scenario actually exercised. Scenarios that do
    /// not consult the baseline corpus report zero; projection rebuild reports the resources it compared.
    /// </summary>
    public required int DatasetVolume { get; init; }
    public required string DriverMode { get; init; }
    public required string JobId { get; init; }
    public required string Scenario { get; init; }
    public required string InjectedFaultAction { get; init; }
    public required string RestoreAction { get; init; }
    public required string CleanupAction { get; init; }
    public required string ExpectedScope { get; init; }
    public required string ObservedScope { get; init; }
    public required string ReportKind { get; init; }
    public required string Verdict { get; init; }
    public required string ReasonCode { get; init; }
    public required IReadOnlyDictionary<string, double> MeasurementsSeconds { get; init; }
    public required IReadOnlyDictionary<string, double> AllowedTargetsSeconds { get; init; }
    public required IReadOnlyDictionary<string, bool> Assertions { get; init; }
    public required IReadOnlyDictionary<string, int> Coverage { get; init; }
    public required IReadOnlyList<string> Deviations { get; init; }
    public required IReadOnlyList<string> ResidualIds { get; init; }
    public required IReadOnlyDictionary<string, string> ArtifactLocators { get; init; }

    /// <summary>Gets the independently read baseline projection schema stamp for projection evidence.</summary>
    public string? ProjectionPreRebuildSchemaVersion { get; init; }

    /// <summary>Gets the rebuilt projection schema stamp for projection evidence.</summary>
    public string? ProjectionRebuiltSchemaVersion { get; init; }

    /// <summary>Gets the retained metadata-only baseline resource digests.</summary>
    public IReadOnlyList<ProjectionResourceDigest>? ProjectionPreRebuildDigests { get; init; }

    /// <summary>Gets the retained metadata-only rebuilt resource digests.</summary>
    public IReadOnlyList<ProjectionResourceDigest>? ProjectionRebuiltDigests { get; init; }

    /// <summary>Gets the number of source-email resources compared.</summary>
    public int ProjectionSourceResourceCount { get; init; }

    /// <summary>Gets the number of unique governed resources compared.</summary>
    public int ProjectionGovernedResourceCount { get; init; }

    /// <summary>Gets the number of WORM records replayed.</summary>
    public int ProjectionWormRecordCount { get; init; }

    /// <summary>Gets the number of grouped WORM operations replayed.</summary>
    public int ProjectionWormOperationCount { get; init; }

    /// <summary>Gets the canonical snapshot-fingerprint algorithm/version token.</summary>
    public string? ProjectionFingerprintAlgorithmVersion { get; init; }

    /// <summary>Gets the canonical baseline snapshot fingerprint.</summary>
    public string? ProjectionPreRebuildFingerprint { get; init; }

    /// <summary>Gets the canonical rebuilt snapshot fingerprint.</summary>
    public string? ProjectionRebuiltFingerprint { get; init; }

    /// <summary>Gets the retained aggregate identity immediately before the controlled rejection.</summary>
    public string? PreFaultRetainedRef { get; init; }

    /// <summary>Gets the persisted EventStore event identity immediately before the controlled rejection.</summary>
    public string? PreFaultEventRef { get; init; }

    /// <summary>Gets the persisted EventStore sequence immediately before the controlled rejection.</summary>
    public long? PreFaultSequence { get; init; }

    /// <summary>Gets the authoritative persisted EventStore timestamp immediately before the controlled rejection.</summary>
    public DateTimeOffset? PreFaultCommittedAtUtc { get; init; }

    /// <summary>Gets the deliberately rejected aggregate candidate identity.</summary>
    public string? RejectedCandidateRef { get; init; }

    /// <summary>Gets when the sandbox exposed the safe candidate identity before rejecting the dependency call.</summary>
    public DateTimeOffset? RejectedAtUtc { get; init; }

    /// <summary>Gets the retained aggregate identity committed after restoration.</summary>
    public string? PostRecoveryRetainedRef { get; init; }

    /// <summary>Gets the persisted EventStore event identity committed after restoration.</summary>
    public string? PostRecoveryEventRef { get; init; }

    /// <summary>Gets the persisted EventStore sequence committed after restoration.</summary>
    public long? PostRecoverySequence { get; init; }

    /// <summary>Gets the authoritative persisted EventStore timestamp after restoration.</summary>
    public DateTimeOffset? PostRecoveryCommittedAtUtc { get; init; }

    /// <summary>
    /// Gets the longest recovery this lane could have measured, in seconds — the harness restoration budget.
    /// <para>
    /// Load-bearing for honest reading of <see cref="AllowedTargetsSeconds"/>: when this ceiling is shorter than an
    /// allowed target, a recovery slower than the ceiling converts to <c>unmeasurable</c> and can never be reported as
    /// a miss of that target. A run therefore demonstrates recovery within THIS budget, not within the target, and the
    /// manifest must say so rather than leaving the reader to infer a stronger claim than the evidence supports.
    /// </para>
    /// </summary>
    public double MeasurableRecoveryCeilingSeconds { get; init; }

    /// <summary>Validates structural completeness, UTC/ULID provenance, positive coverage, and metadata sanitization.</summary>
    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        // Every member below is `required`, but a manifest reaches the gate through JSON deserialization from a CI
        // artifact, where any of them can arrive null. A fail-closed gate must answer with a reason code, not an NRE.
        if (MeasurementsSeconds is null || AllowedTargetsSeconds is null || Assertions is null || Coverage is null ||
            Deviations is null || ResidualIds is null || ArtifactLocators is null)
        {
            errors.Add("Manifest is structurally incomplete.");
            return errors;
        }

        if (!IsCanonicalUlid(RunId))
        {
            errors.Add($"{nameof(RunId)} must be a canonical ULID.");
        }

        if (!IsCanonicalUlid(ScenarioId))
        {
            errors.Add($"{nameof(ScenarioId)} must be a canonical ULID.");
        }

        if (StartedAtUtc.Offset != TimeSpan.Zero || EndedAtUtc.Offset != TimeSpan.Zero || EndedAtUtc < StartedAtUtc)
        {
            errors.Add("UTC start/end bounds must be ordered and use offset zero.");
        }

        if (!IsCommit(RepositoryCommit))
        {
            errors.Add($"{nameof(RepositoryCommit)} must be a full hexadecimal commit identifier.");
        }

        // NaN fails every comparison, so a bare `<= 0` test lets NaN — and +infinity — through. This is the one value
        // that bounds what a passing run may be cited as evidence for, so it must be a real number.
        if (!double.IsFinite(MeasurableRecoveryCeilingSeconds) || MeasurableRecoveryCeilingSeconds <= 0)
        {
            errors.Add($"{nameof(MeasurableRecoveryCeilingSeconds)} must be positive and finite so a target claim can be bounded.");
        }

        string[] tokens =
        [
            AppHostVersion,
            AspireVersion,
            DaprVersion,
            TopologyVersion,
            ConfigurationVersion,
            TenantRef,
            DatasetRef,
            DatasetVersion,
            DriverMode,
            JobId,
            Scenario,
            InjectedFaultAction,
            RestoreAction,
            CleanupAction,
            ExpectedScope,
            ObservedScope,
            ReportKind,
            Verdict,
            ReasonCode,
        ];
        if (tokens.Any(static token => !AuditMetadata.IsSafeStableIdentifier(token)) ||
            !ReplayTenantPolicy.IsTestTenant(TenantRef) ||
            Deviations.Any(static token => !AuditMetadata.IsSafeStableIdentifier(token)) ||
            ResidualIds.Any(static token => !AuditMetadata.IsSafeStableIdentifier(token)))
        {
            errors.Add("Manifest metadata contains an unsafe or sensitive value.");
        }

        if (ConfiguredDatasetVolume <= 0)
        {
            errors.Add("Configured dataset volume must be positive.");
        }

        if (DatasetVolume < 0)
        {
            errors.Add("Exercised dataset volume must not be negative.");
        }

        if (!IsFiniteNonNegative(MeasurementsSeconds) || !IsFiniteNonNegative(AllowedTargetsSeconds))
        {
            errors.Add("Measurements and allowed targets must use safe keys and finite non-negative seconds.");
        }

        if (string.Equals(JobId, LiveRecoveryValidationJobs.ControlledLossPath, StringComparison.Ordinal))
        {
            if (!HasValidControlledLossBounds())
            {
                errors.Add("Controlled-loss durable commit bounds are incomplete or invalid.");
            }

            if (!HasConsistentControlledLossReason())
            {
                errors.Add("Controlled-loss verdict and reason code are inconsistent.");
            }
        }
        else if (HasAnyControlledLossBound())
        {
            errors.Add("Controlled-loss durable commit bounds are not allowed on another report kind.");
        }

        IReadOnlyList<string> projectionErrors = ValidateProjectionEvidence();
        if (projectionErrors.Count > 0)
        {
            errors.Add("Projection snapshot evidence is invalid: " + string.Join(", ", projectionErrors));
        }

        if (Assertions.Count == 0 || Assertions.Keys.Any(static token => !AuditMetadata.IsSafeStableIdentifier(token)))
        {
            errors.Add("Assertions must be non-empty metadata-only tokens.");
        }

        // Coverage must be a safe, non-negative metadata token here. Whether coverage is high enough to RELEASE is a
        // gate decision (`{job}:zero_coverage`), not a metadata-validity decision: rejecting zero here made an
        // unmeasurable report — the one outcome this lane exists to capture — impossible to persist at all, because
        // ProjectionRebuildReport.Unmeasurable reports zero resources compared.
        if (Coverage.Count == 0 || Coverage.Any(static entry => !AuditMetadata.IsSafeStableIdentifier(entry.Key) || entry.Value < 0))
        {
            errors.Add("Every coverage dimension must be a safe token with non-negative coverage.");
        }

        // Only artifacts the lane actually produces may be REQUIRED. Requiring logs/traces/metrics/state-end-state
        // did not make them exist — it only guaranteed every manifest carried syntactically valid links to nothing.
        // Additional kinds remain allowed and are still safety-checked below when present.
        string[] requiredArtifactKinds = ["test-output", "reports"];
        if (requiredArtifactKinds.Any(kind => !ArtifactLocators.TryGetValue(kind, out string? locator) || !IsSafeArtifactLocator(locator)) ||
            ArtifactLocators.Keys.Any(static token => !AuditMetadata.IsSafeStableIdentifier(token)) ||
            ArtifactLocators.Values.Any(static locator => !IsSafeArtifactLocator(locator)))
        {
            errors.Add("Required raw artifact locators are missing or unsafe.");
        }

        return errors;
    }

    /// <summary>Returns stable projection-specific fail-closed evidence reason codes without throwing.</summary>
    internal IReadOnlyList<string> ValidateProjectionEvidence()
    {
        bool projectionJob = string.Equals(JobId, LiveRecoveryValidationJobs.ProjectionRebuild, StringComparison.Ordinal);
        if (!projectionJob)
        {
            return HasAnyProjectionEvidence() ? ["projection_evidence_not_applicable"] : [];
        }

        List<string> reasons = [];
        bool unmeasurable = string.Equals(Verdict, ProjectionRebuildVerdicts.Unmeasurable, StringComparison.Ordinal);
        IReadOnlyList<ProjectionResourceDigest?> pre = ProjectionPreRebuildDigests ?? [];
        IReadOnlyList<ProjectionResourceDigest?> rebuilt = ProjectionRebuiltDigests ?? [];
        if (unmeasurable)
        {
            if (ProjectionPreRebuildSchemaVersion is not null || ProjectionRebuiltSchemaVersion is not null ||
                pre.Count != 0 || rebuilt.Count != 0 || ProjectionSourceResourceCount != 0 ||
                ProjectionGovernedResourceCount != 0 || ProjectionWormRecordCount != 0 ||
                ProjectionWormOperationCount != 0 || ProjectionPreRebuildFingerprint is not null ||
                ProjectionRebuiltFingerprint is not null || ProjectionFingerprintAlgorithmVersion is not null)
            {
                reasons.Add("projection_unmeasurable_evidence_not_empty");
            }

            if (!string.Equals(ReasonCode, ProjectionRebuildReport.ValidationUnmeasurableReasonCode, StringComparison.Ordinal))
            {
                reasons.Add("projection_verdict_reason_mismatch");
            }

            return reasons;
        }

        if (pre.Count > ProjectionSnapshotFingerprint.MaximumResources || rebuilt.Count > ProjectionSnapshotFingerprint.MaximumResources)
        {
            reasons.Add("projection_snapshot_oversized");
        }

        if (ProjectionPreRebuildDigests is null || ProjectionRebuiltDigests is null || pre.Count == 0 || rebuilt.Count == 0 ||
            pre.Any(static digest => digest is null) || rebuilt.Any(static digest => digest is null) ||
            !IsCanonicalSnapshot(pre) || !IsCanonicalSnapshot(rebuilt))
        {
            reasons.Add("projection_snapshot_malformed");
            return reasons;
        }

        if (!string.Equals(ProjectionFingerprintAlgorithmVersion, ProjectionSnapshotFingerprint.AlgorithmVersion, StringComparison.Ordinal) ||
            !ProjectionSnapshotFingerprint.IsCanonicalSha256(ProjectionPreRebuildFingerprint) ||
            !ProjectionSnapshotFingerprint.IsCanonicalSha256(ProjectionRebuiltFingerprint))
        {
            reasons.Add("projection_fingerprint_algorithm_invalid");
        }
        else
        {
            try
            {
                if (!string.Equals(
                        ProjectionSnapshotFingerprint.Compute([.. pre.OfType<ProjectionResourceDigest>()]),
                        ProjectionPreRebuildFingerprint,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        ProjectionSnapshotFingerprint.Compute([.. rebuilt.OfType<ProjectionResourceDigest>()]),
                        ProjectionRebuiltFingerprint,
                        StringComparison.Ordinal))
                {
                    reasons.Add("projection_fingerprint_mismatch");
                }
            }
            catch (Exception)
            {
                reasons.Add("projection_snapshot_malformed");
            }
        }

        int expectedResources = ProjectionSourceResourceCount + ProjectionGovernedResourceCount;
        if (ProjectionSourceResourceCount <= 0 || ProjectionGovernedResourceCount <= 0 ||
            ProjectionWormRecordCount <= 0 || ProjectionWormOperationCount <= 0 ||
            ProjectionWormOperationCount > ProjectionWormRecordCount || expectedResources != pre.Count ||
            expectedResources != rebuilt.Count || DatasetVolume != expectedResources ||
            Coverage is null || !Coverage.TryGetValue("scenario", out int coverage) || coverage != expectedResources)
        {
            reasons.Add("projection_resource_count_mismatch");
        }

        bool fingerprintsEqual = string.Equals(
            ProjectionPreRebuildFingerprint,
            ProjectionRebuiltFingerprint,
            StringComparison.Ordinal);
        bool schemasEqual = string.Equals(
            ProjectionPreRebuildSchemaVersion,
            ProjectionRebuiltSchemaVersion,
            StringComparison.Ordinal);
        bool evidenceEquivalent = fingerprintsEqual && schemasEqual;
        bool verdictConsistent = (Verdict, ReasonCode) switch
        {
            (ProjectionRebuildVerdicts.Equivalent, ProjectionRebuildReport.ValidationCompletedReasonCode) => evidenceEquivalent,
            (ProjectionRebuildVerdicts.Divergent, ProjectionRebuildReport.ValidationCompletedReasonCode) => !evidenceEquivalent,
            _ => false,
        };
        if (!AuditMetadata.IsSafeStableIdentifier(ProjectionPreRebuildSchemaVersion) ||
            !AuditMetadata.IsSafeStableIdentifier(ProjectionRebuiltSchemaVersion) || !verdictConsistent)
        {
            reasons.Add("projection_verdict_reason_mismatch");
        }

        return [.. reasons.Distinct(StringComparer.Ordinal)];
    }

    /// <summary>Returns whether a locator is a query-free CI artifact URI containing no sensitive marker.</summary>
    public static bool IsSafeArtifactLocator(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2_048 ||
            value.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            !Uri.TryCreate(value, UriKind.Absolute, out Uri? locator))
        {
            return false;
        }

        return string.Equals(locator.Scheme, "artifact", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(locator.Query) &&
            string.IsNullOrEmpty(locator.Fragment) &&
            string.IsNullOrEmpty(locator.UserInfo);
    }

    internal static bool IsCanonicalUlid(string? value)
    {
        const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        return value is { Length: 26 } && value[0] <= '7' && value.All(alphabet.Contains);
    }

    private static bool IsCommit(string? value)
        => value?.Length is 40 or 64 && value.All(static character => char.IsAsciiHexDigit(character));

    private static bool IsFiniteNonNegative(IReadOnlyDictionary<string, double> values)
        => values.Count > 0 && values.All(static entry =>
            AuditMetadata.IsSafeStableIdentifier(entry.Key) &&
            double.IsFinite(entry.Value) &&
            entry.Value >= 0);

    private bool HasAnyControlledLossBound()
        => PreFaultRetainedRef is not null || PreFaultEventRef is not null || PreFaultSequence is not null ||
            PreFaultCommittedAtUtc is not null || RejectedCandidateRef is not null || RejectedAtUtc is not null ||
            PostRecoveryRetainedRef is not null || PostRecoveryEventRef is not null || PostRecoverySequence is not null ||
            PostRecoveryCommittedAtUtc is not null;

    private bool HasAnyProjectionEvidence()
        => ProjectionPreRebuildSchemaVersion is not null || ProjectionRebuiltSchemaVersion is not null ||
            ProjectionPreRebuildDigests is not null || ProjectionRebuiltDigests is not null ||
            ProjectionSourceResourceCount != 0 || ProjectionGovernedResourceCount != 0 ||
            ProjectionWormRecordCount != 0 || ProjectionWormOperationCount != 0 ||
            ProjectionFingerprintAlgorithmVersion is not null || ProjectionPreRebuildFingerprint is not null ||
            ProjectionRebuiltFingerprint is not null;

    private static bool IsCanonicalSnapshot(IReadOnlyList<ProjectionResourceDigest?> snapshot)
    {
        HashSet<string> resourceIds = new(StringComparer.Ordinal);
        foreach (ProjectionResourceDigest? digest in snapshot)
        {
            if (digest is null || !AuditMetadata.IsSafeStableIdentifier(digest.ResourceId) ||
                !ProjectionSnapshotFingerprint.IsCanonicalSha256(digest.StructuralStateToken) ||
                !resourceIds.Add(digest.ResourceId))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasValidControlledLossBounds()
    {
        string[] identities =
        [
            PreFaultRetainedRef ?? string.Empty,
            PreFaultEventRef ?? string.Empty,
            RejectedCandidateRef ?? string.Empty,
            PostRecoveryRetainedRef ?? string.Empty,
            PostRecoveryEventRef ?? string.Empty,
        ];
        return identities.All(IsCanonicalUlid) &&
            identities.Distinct(StringComparer.Ordinal).Count() == identities.Length &&
            PreFaultSequence > 0 &&
            PostRecoverySequence > 0 &&
            PreFaultCommittedAtUtc is { Offset: var preOffset } pre && preOffset == TimeSpan.Zero &&
            RejectedAtUtc is { Offset: var rejectedOffset } && rejectedOffset == TimeSpan.Zero &&
            PostRecoveryCommittedAtUtc is { Offset: var postOffset } post && postOffset == TimeSpan.Zero &&
            StartedAtUtc <= EndedAtUtc &&
            pre <= post;
    }

    private bool HasConsistentControlledLossReason()
        => (Verdict, ReasonCode) switch
        {
            (ControlledLossPathVerdicts.Met, ControlledLossPathReport.CompletedReasonCode) => true,
            (ControlledLossPathVerdicts.Missed, ControlledLossPathReport.TargetMissedReasonCode) => true,
            (ControlledLossPathVerdicts.Unmeasurable, ControlledLossPathReport.UnmeasurableReasonCode) => true,
            _ => false,
        };
}
