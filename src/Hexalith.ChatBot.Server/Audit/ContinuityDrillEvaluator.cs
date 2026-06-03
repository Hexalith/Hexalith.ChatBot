namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The pure, deterministic continuity-drill verdict function (Story 9.11, AC1/AC4). Given the measured RPO/RTO and the
/// data-loss check, it returns a <see cref="ContinuityDrillVerdicts"/> token by comparing against the single-source
/// <see cref="RecoveryTargets"/>. No clock, no IO — re-running over the same inputs yields the same verdict (mirroring
/// the pure verifiers behind the 9.4/9.5 isolation probes).
/// <para>
/// The evaluator is binary <c>met</c>/<c>missed</c> over <b>available</b> measurements; the <c>unmeasurable</c> verdict
/// for a drill that could not complete is produced by the coordinator via
/// <see cref="ContinuityDrillReport.Unmeasurable"/> (fail-safe), never here.
/// </para>
/// </summary>
internal static class ContinuityDrillEvaluator
{
    /// <summary>The deviation token recorded when the measured RPO exceeds <see cref="RecoveryTargets.MaxRpo"/>.</summary>
    public const string RpoExceededDeviation = "rpo_exceeded";

    /// <summary>The deviation token recorded when the measured RTO exceeds <see cref="RecoveryTargets.MaxRto"/>.</summary>
    public const string RtoExceededDeviation = "rto_exceeded";

    /// <summary>The deviation token recorded when the data-loss check detected a committed-before-outage operation missing after recovery.</summary>
    public const string DataLossDeviation = "data_loss_detected";

    /// <summary>
    /// Returns <see cref="ContinuityDrillVerdicts.Met"/> iff the measured RPO is within
    /// <see cref="RecoveryTargets.MaxRpo"/>, the measured RTO is within <see cref="RecoveryTargets.MaxRto"/>, and no
    /// data loss was detected; otherwise <see cref="ContinuityDrillVerdicts.Missed"/>. Boundary equality
    /// (<c>== target</c>) is within target (met).
    /// </summary>
    public static string Evaluate(TimeSpan measuredRpo, TimeSpan measuredRto, bool dataLossDetected)
        => measuredRpo <= RecoveryTargets.MaxRpo && measuredRto <= RecoveryTargets.MaxRto && !dataLossDetected
            ? ContinuityDrillVerdicts.Met
            : ContinuityDrillVerdicts.Missed;

    /// <summary>
    /// Returns the bounded deviation tokens for the measured inputs, in a stable order
    /// (<see cref="RpoExceededDeviation"/>, <see cref="RtoExceededDeviation"/>, <see cref="DataLossDeviation"/>). Empty
    /// when the drill is <see cref="ContinuityDrillVerdicts.Met"/>.
    /// </summary>
    public static IReadOnlyList<string> Deviations(TimeSpan measuredRpo, TimeSpan measuredRto, bool dataLossDetected)
    {
        List<string> deviations = [];
        if (measuredRpo > RecoveryTargets.MaxRpo)
        {
            deviations.Add(RpoExceededDeviation);
        }

        if (measuredRto > RecoveryTargets.MaxRto)
        {
            deviations.Add(RtoExceededDeviation);
        }

        if (dataLossDetected)
        {
            deviations.Add(DataLossDeviation);
        }

        return deviations;
    }
}
