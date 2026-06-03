namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The pure, deterministic scoped-outage degradation verdict function (Story 9.13, AC1/AC2/AC3). Given a measured
/// <see cref="ScopedOutageDegradationMeasurement"/> it returns a <see cref="ScopedOutageDegradationVerdicts"/> token by
/// folding the three NFR59 isolation assertions (cross-tenant leakage, unauthorized mutation, silent data loss), the
/// NFR58 scope-containment check (observed scope within the expected narrowest scope), and the NFR17/NFR13 recovery
/// checks (in-flight items resume recoverable, no duplicate side effects). No clock, no IO — re-running over the same
/// inputs yields the same verdict (mirroring <see cref="ContinuityDrillEvaluator"/> / the pure verifiers behind the
/// 9.4/9.5 isolation probes).
/// <para>
/// The evaluator is binary <c>contained</c>/<c>breached</c> over <b>available</b> measurements; the <c>unmeasurable</c>
/// verdict for a validation that could not complete is produced by the coordinator via
/// <see cref="ScopedOutageDegradationReport.Unmeasurable"/> (fail-safe), never here. The late-scope-recording dimension
/// is folded by the coordinator into the report's <c>ScopeRecordedWithinTarget</c> boolean (a monitoring-latency miss),
/// <b>not</b> the verdict — the verdict stays binary contained/breached over the serious assertions.
/// </para>
/// </summary>
internal static class ScopedOutageDegradationEvaluator
{
    /// <summary>The deviation token recorded when a foreign-tenant artifact survived the outage (NFR59).</summary>
    public const string CrossTenantLeakageDeviation = "cross_tenant_leakage";

    /// <summary>The deviation token recorded when the outage caused an unauthorized state mutation (NFR59).</summary>
    public const string UnauthorizedMutationDeviation = "unauthorized_mutation";

    /// <summary>The deviation token recorded when the outage caused silent data loss (NFR59).</summary>
    public const string SilentDataLossDeviation = "silent_data_loss";

    /// <summary>The deviation token recorded when the observed degradation scope escaped the expected narrowest scope (NFR58).</summary>
    public const string ScopeEscapeDeviation = "scope_escape";

    /// <summary>The deviation token recorded when an in-flight item did not resume from a visible recoverable state on recovery (NFR17).</summary>
    public const string InflightNotRecoverableDeviation = "inflight_not_recoverable";

    /// <summary>The deviation token recorded when recovery produced a duplicate side effect (NFR13).</summary>
    public const string DuplicateSideEffectDeviation = "duplicate_side_effect";

    /// <summary>The deviation token recorded when the detection→scope-recording latency exceeded <see cref="RecoveryTargets.MaxScopeRecordingLatency"/> (NFR41).</summary>
    public const string ScopeRecordingExceededDeviation = "scope_recording_exceeded";

    /// <summary>
    /// Returns <see cref="ScopedOutageDegradationVerdicts.Breached"/> iff <b>any</b> serious assertion failed
    /// (cross-tenant leakage detected, unauthorized mutation detected, silent data loss detected, the observed scope
    /// escaped the expected scope, an in-flight item did not resume recoverable, <b>or</b> a duplicate side effect
    /// occurred); otherwise <see cref="ScopedOutageDegradationVerdicts.Contained"/>. Pure — no clock, no IO.
    /// </summary>
    public static string Evaluate(ScopedOutageDegradationMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        return AnySeriousAssertionFailed(measurement)
            ? ScopedOutageDegradationVerdicts.Breached
            : ScopedOutageDegradationVerdicts.Contained;
    }

    /// <summary>
    /// Returns a safe bounded locator (<see cref="AuditMetadata.SafeOptionalToken"/>-guarded) for the first failed
    /// assertion in the stable deviation order, or <see langword="null"/> when the validation is
    /// <see cref="ScopedOutageDegradationVerdicts.Contained"/>. Deterministic across runs.
    /// </summary>
    public static string? FirstBreachLocator(ScopedOutageDegradationMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        string? firstToken = SeriousDeviations(measurement) is [string first, ..] ? first : null;
        if (firstToken is null)
        {
            return null;
        }

        return AuditMetadata.SafeOptionalToken($"scope:{measurement.ObservedScope}|deviation:{firstToken}");
    }

    /// <summary>
    /// Returns the bounded deviation tokens in stable order (<see cref="CrossTenantLeakageDeviation"/>,
    /// <see cref="UnauthorizedMutationDeviation"/>, <see cref="SilentDataLossDeviation"/>,
    /// <see cref="ScopeEscapeDeviation"/>, <see cref="InflightNotRecoverableDeviation"/>,
    /// <see cref="DuplicateSideEffectDeviation"/>, then <see cref="ScopeRecordingExceededDeviation"/> when
    /// <paramref name="scopeRecordedWithinTarget"/> is <see langword="false"/>). Empty when <c>contained</c> and within
    /// the recording target.
    /// </summary>
    public static IReadOnlyList<string> Deviations(ScopedOutageDegradationMeasurement measurement, bool scopeRecordedWithinTarget)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        List<string> deviations = SeriousDeviations(measurement);
        if (!scopeRecordedWithinTarget)
        {
            deviations.Add(ScopeRecordingExceededDeviation);
        }

        return deviations;
    }

    private static List<string> SeriousDeviations(ScopedOutageDegradationMeasurement measurement)
    {
        List<string> deviations = [];
        if (measurement.CrossTenantLeakageDetected)
        {
            deviations.Add(CrossTenantLeakageDeviation);
        }

        if (measurement.UnauthorizedMutationDetected)
        {
            deviations.Add(UnauthorizedMutationDeviation);
        }

        if (measurement.SilentDataLossDetected)
        {
            deviations.Add(SilentDataLossDeviation);
        }

        if (!string.Equals(measurement.ObservedScope, measurement.ExpectedScope, StringComparison.Ordinal))
        {
            deviations.Add(ScopeEscapeDeviation);
        }

        if (!measurement.InflightItemsRecoverable)
        {
            deviations.Add(InflightNotRecoverableDeviation);
        }

        if (measurement.DuplicateSideEffectDetected)
        {
            deviations.Add(DuplicateSideEffectDeviation);
        }

        return deviations;
    }

    private static bool AnySeriousAssertionFailed(ScopedOutageDegradationMeasurement measurement)
        => measurement.CrossTenantLeakageDetected
            || measurement.UnauthorizedMutationDetected
            || measurement.SilentDataLossDetected
            || !string.Equals(measurement.ObservedScope, measurement.ExpectedScope, StringComparison.Ordinal)
            || !measurement.InflightItemsRecoverable
            || measurement.DuplicateSideEffectDetected;
}
