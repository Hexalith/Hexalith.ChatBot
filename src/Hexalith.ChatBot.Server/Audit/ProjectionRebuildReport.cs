namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The metadata-only NFR57 validation-evidence artifact for one projection-rebuild validation run (Story 9.12, AC1/AC4).
/// Modeled on <see cref="ContinuityDrillReport"/> / <see cref="AuditCompletenessMeasurement"/>: a sealed record of safe
/// bounded tokens, the measured duration, counts, and bounded reason codes — never raw email content, recipient PII,
/// subject, body, prompts, payloads, or vector/embedding values. This report <b>is</b> the NFR57 validation evidence
/// (dataset + duration-vs-4-hr-target + diff result).
/// <para>
/// Fail-safe (Epic 8/9 no-fabrication doctrine): a validation that <b>cannot complete</b> is recorded via
/// <see cref="Unmeasurable"/> (verdict <see cref="ProjectionRebuildVerdicts.Unmeasurable"/>), never a fabricated
/// <see cref="ProjectionRebuildVerdicts.Equivalent"/>.
/// </para>
/// <para>
/// <b>Three distinct breach dimensions.</b> <see cref="IsDivergent"/> is the serious determinism breach (NFR49a evidence
/// reproducibility / invariant #11). <see cref="IsBreach"/> folds all three fail-closed dimensions — a divergence, an
/// unmeasurable validation, <b>or</b> a duration overrun (<see cref="DurationWithinTarget"/> false) — so any of them
/// fail-closed-audits-then-alerts. A deterministic-but-slow rebuild stays <c>equivalent</c> with
/// <see cref="DurationWithinTarget"/> false (a recovery-time miss, not a determinism failure).
/// </para>
/// </summary>
internal sealed record ProjectionRebuildReport(
    string TenantRef,
    string DatasetRef,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    TimeSpan MeasuredRebuildDuration,
    bool DurationWithinTarget,
    string Verdict,
    int ResourcesCompared,
    IReadOnlyList<string> Deviations,
    string? FirstDivergingResourceLocator,
    string ProjectionSchemaVersion,
    string CorrelationId,
    string ReasonCode,
    RecoveryValidationExecutionAssertions? ExecutionAssertions = null)
{
    /// <summary>Reason code for a validation that completed and produced a measured verdict (equivalent or divergent).</summary>
    public const string ValidationCompletedReasonCode = "projection_rebuild_completed";

    /// <summary>Reason code for a validation that could not complete (driver threw, rebuild never finished) — the fail-safe breach.</summary>
    public const string ValidationUnmeasurableReasonCode = "projection_rebuild_unmeasurable";

    /// <summary>The bounded deviation token recorded for a validation that could not complete.</summary>
    public const string IncompleteDeviation = "rebuild_incomplete";

    /// <summary>
    /// The bounded deviation token recorded when the rebuild itself completed but its evidence could not be retained,
    /// so a sink outage is not reported as a rebuild that could not run.
    /// </summary>
    public const string EvidenceRetentionFailedDeviation = "rebuild_evidence_retention_failed";

    /// <summary>True when the rebuild was non-deterministic (verdict <c>divergent</c>) — the serious NFR49a/invariant-#11 breach.</summary>
    public bool IsDivergent => string.Equals(Verdict, ProjectionRebuildVerdicts.Divergent, StringComparison.Ordinal);

    /// <summary>True when the validation must fail-closed-audit-then-alert: a divergence, an unmeasurable validation, <b>or</b> a duration overrun.</summary>
    public bool IsBreach => !string.Equals(Verdict, ProjectionRebuildVerdicts.Equivalent, StringComparison.Ordinal) || !DurationWithinTarget;

    /// <summary>
    /// Builds the fail-safe unmeasurable report for a validation that could not complete (a production-tenant target, a
    /// throwing driver, or unavailable snapshots/durations). Verdict <see cref="ProjectionRebuildVerdicts.Unmeasurable"/>,
    /// <see cref="DurationWithinTarget"/> false, a single <see cref="IncompleteDeviation"/>, zero resources compared, no
    /// locator, and the <see cref="ValidationUnmeasurableReasonCode"/> — never a fabricated <c>equivalent</c>.
    /// </summary>
    public static ProjectionRebuildReport Unmeasurable(
        string tenantRef,
        string datasetRef,
        string correlationId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc,
        string projectionSchemaVersion)
        => new(
            tenantRef,
            datasetRef,
            startedAtUtc,
            endedAtUtc,
            MeasuredRebuildDuration: TimeSpan.Zero,
            DurationWithinTarget: false,
            ProjectionRebuildVerdicts.Unmeasurable,
            ResourcesCompared: 0,
            Deviations: [IncompleteDeviation],
            FirstDivergingResourceLocator: null,
            projectionSchemaVersion,
            correlationId,
            ValidationUnmeasurableReasonCode);
}

/// <summary>
/// The measured result the <see cref="IProjectionRebuildDriver"/> seam returns to the coordinator: the wall-clock bounds,
/// the measured rebuild duration, the pre-rebuild + rebuilt structural snapshots, and the two stamped projection schema
/// versions. The pure <see cref="ProjectionRebuildEquivalenceEvaluator"/> folds the snapshots + schema versions into a
/// verdict; the coordinator compares <see cref="MeasuredDuration"/> against <see cref="RecoveryTargets.MaxRto"/>.
/// </summary>
internal sealed record ProjectionRebuildMeasurement(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    TimeSpan MeasuredDuration,
    IReadOnlyList<ProjectionResourceDigest> PreRebuildSnapshot,
    IReadOnlyList<ProjectionResourceDigest> RebuiltSnapshot,
    string PreRebuildSchemaVersion,
    string RebuiltSchemaVersion,
    RecoveryValidationExecutionAssertions? ExecutionAssertions = null);

/// <summary>
/// The structured result of a projection-rebuild validation sweep across every baseline dataset (Story 9.12, AC4). A
/// CI/release gate asserts against the dimension it cares about — e.g. <c>Divergent == 0 &amp;&amp; Unmeasurable == 0</c>
/// ⇒ the rebuilds are deterministic and produced evidence — while a <see cref="DurationExceeded"/> is a recovery-time
/// recalibration signal kept distinct from a determinism failure. Mirrors <see cref="ContinuityDrillOutcome"/>.
/// </summary>
internal sealed record ProjectionRebuildOutcome(
    int TenantsValidated,
    int Equivalent,
    int Divergent,
    int DurationExceeded,
    int Unmeasurable,
    int Alerted);
