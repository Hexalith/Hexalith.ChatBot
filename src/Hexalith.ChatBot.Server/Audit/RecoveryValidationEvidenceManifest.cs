namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Metadata-only execution-provenance manifest surrounding one canonical recovery report. The manifest never replaces
/// report verdict semantics and must contain only bounded tokens, numeric measurements, booleans, and artifact locators.
/// </summary>
internal sealed record RecoveryValidationEvidenceManifest
{
    /// <summary>
    /// The only <see cref="DriverMode"/> the release evidence gate accepts as a live run. A scripted fake produces a
    /// manifest that is structurally identical in every other field, so this token is the sole discriminator.
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

        if (DatasetVolume <= 0)
        {
            errors.Add("Dataset volume must be positive.");
        }

        if (!IsFiniteNonNegative(MeasurementsSeconds) || !IsFiniteNonNegative(AllowedTargetsSeconds))
        {
            errors.Add("Measurements and allowed targets must use safe keys and finite non-negative seconds.");
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
}
