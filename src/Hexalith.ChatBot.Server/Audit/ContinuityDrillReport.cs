namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The metadata-only A10 recalibration evidence artifact for one continuity-drill scenario run (Story 9.11, AC3/AC4).
/// Modeled on <see cref="AuditCompletenessMeasurement"/>: a sealed record of safe bounded tokens, durations, counts, and
/// bounded reason codes — never raw item content, recipient PII, prompts, or payloads. This report <b>is</b> the A10
/// recalibration evidence.
/// <para>
/// Fail-safe (Epic 8/9 no-fabrication doctrine): a drill that <b>cannot complete</b> is recorded via
/// <see cref="Unmeasurable"/> (verdict <see cref="ContinuityDrillVerdicts.Unmeasurable"/>), never a fabricated
/// <see cref="ContinuityDrillVerdicts.Met"/>. <see cref="IsBreach"/> folds the fail-closed doctrine — a miss <b>or</b>
/// an unmeasurable drill both fail-closed-audit-then-alert — while <see cref="IsMiss"/> distinguishes the honest
/// recalibration signal (NOT stop-ship) from the fail-safe breach.
/// </para>
/// </summary>
internal sealed record ContinuityDrillReport(
    string TenantRef,
    string Scenario,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    TimeSpan MeasuredRpo,
    TimeSpan MeasuredRto,
    bool DataLossDetected,
    string Verdict,
    IReadOnlyList<string> Deviations,
    bool RecalibrationFlag,
    string? FollowUpActionRef,
    string CorrelationId,
    string ReasonCode)
{
    /// <summary>Reason code for a drill that completed and produced a measured verdict (met or missed).</summary>
    public const string DrillCompletedReasonCode = "continuity_drill_completed";

    /// <summary>Reason code for a drill that could not complete (runner threw, recovery never finished) — the fail-safe breach.</summary>
    public const string DrillUnmeasurableReasonCode = "continuity_drill_unmeasurable";

    /// <summary>The bounded deviation token recorded for a drill that could not complete.</summary>
    public const string IncompleteDeviation = "continuity_drill_incomplete";

    /// <summary>True when the drill produced an honest <c>missed</c> verdict — a recorded deviation flagging A10 recalibration (NOT stop-ship).</summary>
    public bool IsMiss => string.Equals(Verdict, ContinuityDrillVerdicts.Missed, StringComparison.Ordinal);

    /// <summary>True when the drill did not meet target (a miss <b>or</b> an unmeasurable drill) — fail-closed-audit-then-alert.</summary>
    public bool IsBreach => !string.Equals(Verdict, ContinuityDrillVerdicts.Met, StringComparison.Ordinal);

    /// <summary>
    /// Builds the fail-safe unmeasurable report for a drill that could not complete (unknown/production scenario, a
    /// throwing runner, or unavailable durations). Verdict <see cref="ContinuityDrillVerdicts.Unmeasurable"/>,
    /// <see cref="RecalibrationFlag"/> true, a single <see cref="IncompleteDeviation"/>, and the
    /// <see cref="DrillUnmeasurableReasonCode"/> — never a fabricated <c>met</c>.
    /// </summary>
    public static ContinuityDrillReport Unmeasurable(
        string tenantRef,
        string scenario,
        string correlationId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset endedAtUtc)
        => new(
            tenantRef,
            scenario,
            startedAtUtc,
            endedAtUtc,
            MeasuredRpo: TimeSpan.Zero,
            MeasuredRto: TimeSpan.Zero,
            DataLossDetected: false,
            ContinuityDrillVerdicts.Unmeasurable,
            Deviations: [IncompleteDeviation],
            RecalibrationFlag: true,
            FollowUpActionRef: $"continuity-recalibration:{scenario}",
            correlationId,
            DrillUnmeasurableReasonCode);
}

/// <summary>
/// The structured result of a continuity-drill sweep across every <see cref="ContinuityDrillScenarios"/> scenario
/// (Story 9.11, AC4). A CI/release gate asserts against the dimension it cares about — e.g.
/// <c>Unmeasurable == 0</c> ⇒ the drills ran and produced evidence (the fail-safe breach is an unmeasurable drill),
/// distinct from <c>Missed == 0</c> ⇒ every target met. Mirrors <c>DerivedStoreIsolationProbeOutcome</c>.
/// </summary>
internal sealed record ContinuityDrillOutcome(int ScenariosRun, int Met, int Missed, int Unmeasurable, int Alerted);

/// <summary>
/// The measured result the <see cref="IContinuityDrillScenarioRunner"/> seam returns from running one recovery scenario:
/// the wall-clock bounds and the measured RPO/RTO plus the data-loss check. The pure
/// <see cref="ContinuityDrillEvaluator"/> folds these into a verdict.
/// </summary>
internal sealed record ContinuityDrillMeasurement(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndedAtUtc,
    TimeSpan MeasuredRpo,
    TimeSpan MeasuredRto,
    bool DataLossDetected);
