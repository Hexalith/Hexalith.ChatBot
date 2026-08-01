namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The metadata-only NFR58/NFR59 validation-evidence artifact for one scoped-outage degradation validation run (Story
/// 9.13, AC1/AC2/AC4). Modeled on <see cref="ProjectionRebuildReport"/> / <see cref="ContinuityDrillReport"/>: a sealed
/// record of safe bounded tokens, the integer-second scope-recording latency, booleans, and bounded reason codes — never
/// raw email content, recipient PII, subject, body, prompts, payloads, foreign-tenant identity, or vector/embedding
/// values. This report <b>is</b> the per-scenario NFR58/NFR59 evidence (dependency + expected/observed scope + the three
/// assertion outcomes + verdict).
/// <para>
/// Fail-safe (Epic 8/9 no-fabrication doctrine): a validation that <b>cannot complete</b> is recorded via
/// <see cref="Unmeasurable"/> (verdict <see cref="ScopedOutageDegradationVerdicts.Unmeasurable"/>), never a fabricated
/// <see cref="ScopedOutageDegradationVerdicts.Contained"/>.
/// </para>
/// <para>
/// <b>Three distinct breach dimensions.</b> <see cref="IsScopeBreach"/> is the serious NFR58/NFR59 isolation/scope/
/// recovery breach (cross-tenant leakage, unauthorized mutation, silent data loss, scope escape, non-recoverable
/// in-flight, or duplicate side effect). <see cref="IsBreach"/> folds all three fail-closed dimensions — a serious
/// breach, an unmeasurable validation, <b>or</b> a late scope recording (<see cref="ScopeRecordedWithinTarget"/> false)
/// — so any of them fail-closed-audits-then-alerts. A contained-but-slow degradation stays <c>contained</c> with
/// <see cref="ScopeRecordedWithinTarget"/> false (a monitoring-latency miss, not an isolation failure).
/// </para>
/// </summary>
internal sealed record ScopedOutageDegradationReport(
    string TenantRef,
    string Dependency,
    string ExpectedScope,
    string ObservedScope,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    TimeSpan ScopeRecordingLatency,
    bool ScopeRecordedWithinTarget,
    string Verdict,
    IReadOnlyList<string> Deviations,
    string? FirstBreachLocator,
    string CorrelationId,
    string ReasonCode,
    RecoveryValidationExecutionAssertions? ExecutionAssertions = null)
{
    /// <summary>Reason code for a validation that completed and produced a measured verdict (contained or breached).</summary>
    public const string ValidationCompletedReasonCode = "scoped_outage_validation_completed";

    /// <summary>Reason code for a validation that could not complete (driver threw, outage exercise never finished) — the fail-safe breach.</summary>
    public const string ValidationUnmeasurableReasonCode = "scoped_outage_validation_unmeasurable";

    /// <summary>The bounded deviation token recorded for a validation that could not complete.</summary>
    public const string IncompleteDeviation = "scoped_outage_incomplete";

    /// <summary>
    /// The bounded deviation token recorded when the scenario itself completed but its evidence could not be retained,
    /// so a sink outage is not reported as an outage exercise that could not run.
    /// </summary>
    public const string EvidenceRetentionFailedDeviation = "scoped_outage_evidence_retention_failed";

    /// <summary>True when the validation produced a <c>breached</c> verdict — the serious NFR58/NFR59 isolation/scope/recovery breach.</summary>
    public bool IsScopeBreach => string.Equals(Verdict, ScopedOutageDegradationVerdicts.Breached, StringComparison.Ordinal);

    /// <summary>True when the validation must fail-closed-audit-then-alert: a serious breach, an unmeasurable validation, <b>or</b> a late scope recording.</summary>
    public bool IsBreach => !string.Equals(Verdict, ScopedOutageDegradationVerdicts.Contained, StringComparison.Ordinal) || !ScopeRecordedWithinTarget;

    /// <summary>
    /// Builds the fail-safe unmeasurable report for a validation that could not complete (a production-tenant target, an
    /// unknown dependency, a throwing driver, or unavailable assertion results). Verdict
    /// <see cref="ScopedOutageDegradationVerdicts.Unmeasurable"/>, <see cref="ScopeRecordedWithinTarget"/> false, a single
    /// <see cref="IncompleteDeviation"/>, the scopes folded to the safe <see cref="ScopedOutageScopes.Tenant"/> token, no
    /// locator, and the <see cref="ValidationUnmeasurableReasonCode"/> — never a fabricated <c>contained</c>.
    /// </summary>
    public static ScopedOutageDegradationReport Unmeasurable(
        string tenantRef,
        string dependency,
        string correlationId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc)
        => new(
            tenantRef,
            dependency,
            ExpectedScope: ScopedOutageScopes.Tenant,
            ObservedScope: ScopedOutageScopes.Tenant,
            startedAtUtc,
            endedAtUtc,
            ScopeRecordingLatency: TimeSpan.Zero,
            ScopeRecordedWithinTarget: false,
            ScopedOutageDegradationVerdicts.Unmeasurable,
            Deviations: [IncompleteDeviation],
            FirstBreachLocator: null,
            correlationId,
            ValidationUnmeasurableReasonCode);
}

/// <summary>
/// The measured result the <see cref="IScopedOutageInjectionDriver"/> seam returns to the coordinator: the expected vs
/// observed degradation scope, the three NFR59 isolation assertions, the NFR17/NFR13 recovery checks, the measured
/// NFR41 detection→scope-recording latency, and the wall-clock bounds. The pure
/// <see cref="ScopedOutageDegradationEvaluator"/> folds the assertions + scopes into a verdict; the coordinator compares
/// <see cref="ScopeRecordingLatency"/> against <see cref="RecoveryTargets.MaxScopeRecordingLatency"/>.
/// <para>
/// <see cref="InflightItemsRecoverable"/> is the <b>positive</b> NFR17 assertion — the evaluator records
/// <c>inflight_not_recoverable</c> when it is <see langword="false"/>; an <see cref="ObservedScope"/> outside the
/// <see cref="ExpectedScope"/> is the NFR58 scope escape.
/// </para>
/// </summary>
internal sealed record ScopedOutageDegradationMeasurement(
    string ExpectedScope,
    string ObservedScope,
    bool CrossTenantLeakageDetected,
    bool UnauthorizedMutationDetected,
    bool SilentDataLossDetected,
    bool InflightItemsRecoverable,
    bool DuplicateSideEffectDetected,
    TimeSpan ScopeRecordingLatency,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    RecoveryValidationExecutionAssertions? ExecutionAssertions = null);

/// <summary>
/// The structured result of a scoped-outage degradation validation sweep across every
/// <see cref="ScopedOutageDependencies"/> scenario (Story 9.13, AC4). A CI/release gate asserts against the dimension it
/// cares about — e.g. <c>Breached == 0 &amp;&amp; Unmeasurable == 0</c> ⇒ every dependency outage degraded only its scope
/// and produced evidence — while a <see cref="ScopeRecordingExceeded"/> is a monitoring-latency recalibration signal kept
/// distinct from an isolation breach. Mirrors <see cref="ProjectionRebuildOutcome"/> / <see cref="ContinuityDrillOutcome"/>.
/// </summary>
internal sealed record ScopedOutageDegradationOutcome(
    int ScenariosValidated,
    int Contained,
    int Breached,
    int ScopeRecordingExceeded,
    int Unmeasurable,
    int Alerted);
