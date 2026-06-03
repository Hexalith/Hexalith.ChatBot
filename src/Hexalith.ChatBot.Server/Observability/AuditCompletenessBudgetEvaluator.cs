using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Pure, deterministic, fail-safe mapping from an already-computed <see cref="AuditCompletenessMeasurement"/> to the
/// coarse <see cref="ErrorBudgetBurnState"/> (Story 9.2, AC2/NFR50a). It mirrors <see cref="ErrorBudgetBurnEvaluator"/>
/// exactly: it does no IO, no clock, no percentile/count math — it maps an <b>already-computed fraction</b>. The
/// reconstruction, the projection diff, the replay exclusion, and the windowing all happen upstream in
/// <see cref="AuditCompletenessMeasurer"/>; this evaluator only thresholds the result.
/// <list type="bullet">
///   <item><see cref="ErrorBudgetBurnState.WithinBudget"/> — measurable and fraction ≥ 99.5%;</item>
///   <item><see cref="ErrorBudgetBurnState.Exhausted"/> — measurable and fraction &lt; 99.5% (a P1 breach);</item>
///   <item><see cref="ErrorBudgetBurnState.Unknown"/> — unmeasurable (chain/projection unavailable or threw), NEVER a fabricated within-budget.</item>
/// </list>
/// Note there is no <see cref="ErrorBudgetBurnState.Approaching"/> band: NFR50a is a single hard 99.5% threshold that
/// either holds or trips a P1, so the mapping is intentionally two-state-plus-unknown.
/// </summary>
internal static class AuditCompletenessBudgetEvaluator
{
    /// <summary>Maps a completeness measurement to its coarse, fail-safe error-budget burn state.</summary>
    public static ErrorBudgetBurnState FromMeasurement(AuditCompletenessMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        if (!measurement.IsMeasurable)
        {
            return ErrorBudgetBurnState.Unknown;
        }

        return measurement.Fraction >= AuditCompletenessMeasurement.CompletenessTargetFraction
            ? ErrorBudgetBurnState.WithinBudget
            : ErrorBudgetBurnState.Exhausted;
    }
}
