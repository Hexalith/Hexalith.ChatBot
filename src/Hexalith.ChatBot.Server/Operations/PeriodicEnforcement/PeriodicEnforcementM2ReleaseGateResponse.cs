using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Operations.PeriodicEnforcement;

/// <summary>
/// The M2 release-gate payload. <see cref="IsStopShip"/> drives an HTTP 503 so a release job can gate on the status
/// code alone.
/// </summary>
internal sealed record PeriodicEnforcementM2ReleaseGateResponse(
    bool M2SweepsEnabled,
    IReadOnlyDictionary<string, int> EvaluatorFailureCounts,
    IReadOnlyDictionary<string, PeriodicEnforcementM2SweepHealth> M2SweepStatuses,
    IReadOnlyList<string> StopShipReasons,
    bool IsStopShip)
{
    public static PeriodicEnforcementM2ReleaseGateResponse From(
        PeriodicEnforcementRunStatus status,
        bool m2SweepsEnabled,
        DateTimeOffset evaluatedAtUtc,
        TimeSpan maximumResultAge)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (maximumResultAge < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumResultAge), maximumResultAge, "The maximum result age must be non-negative.");
        }

        Dictionary<string, PeriodicEnforcementM2SweepHealth> sweeps = new(StringComparer.Ordinal);
        List<string> reasons = [];
        if (!m2SweepsEnabled)
        {
            reasons.Add("m2_sweeps_disabled");
        }

        foreach (string jobName in M2SweepJobs.All)
        {
            _ = status.M2SweepStatuses.TryGetValue(jobName, out PeriodicEnforcementM2SweepStatus? sweep);
            sweeps[jobName] = new PeriodicEnforcementM2SweepHealth(
                sweep?.LastRanAtUtc,
                sweep?.LastSucceededAtUtc,
                sweep?.LastBreaches is > 0,
                sweep?.LastCoverage is > 0);

            if (sweep?.LastBreaches is null || sweep.LastSucceededAtUtc is null)
            {
                reasons.Add($"{jobName}:never_completed");
            }
            else if (!sweep.LastAttemptCompletedSuccessfully)
            {
                reasons.Add($"{jobName}:latest_attempt_incomplete");
            }
            else if (evaluatedAtUtc - sweep.LastSucceededAtUtc.Value < TimeSpan.Zero)
            {
                reasons.Add($"{jobName}:result_timestamp_in_future");
            }
            else if (evaluatedAtUtc - sweep.LastSucceededAtUtc.Value > maximumResultAge)
            {
                reasons.Add($"{jobName}:stale_result");
            }
            else if (sweep.LastBreaches > 0)
            {
                reasons.Add($"{jobName}:breaches_detected");
            }
            else if (sweep.LastCoverage is null or 0 && !IsCoverageStructurallyUnavailable(jobName, sweep))
            {
                reasons.Add($"{jobName}:zero_coverage");
            }
        }

        return new PeriodicEnforcementM2ReleaseGateResponse(
            m2SweepsEnabled,
            status.EvaluatorFailureCounts,
            sweeps,
            reasons,
            reasons.Count > 0);
    }

    private static bool IsCoverageStructurallyUnavailable(string jobName, PeriodicEnforcementM2SweepStatus sweep)
        => string.Equals(jobName, M2SweepJobs.DerivedStoreIsolationProbe, StringComparison.Ordinal) &&
            sweep.LastPopulation == 1;
}
