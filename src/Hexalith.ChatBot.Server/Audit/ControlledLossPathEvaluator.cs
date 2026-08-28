namespace Hexalith.ChatBot.Server.Audit;

/// <summary>Pure evaluator for authoritative durable-bound controlled-loss observations.</summary>
internal static class ControlledLossPathEvaluator
{
    /// <summary>The retained EventStore bounds were malformed, unordered, or non-authoritative.</summary>
    public const string InvalidDurableBoundsDeviation = "invalid_durable_bounds";

    /// <summary>The measured loss window was zero or negative.</summary>
    public const string NonPositiveRpoDeviation = "rpo_not_positive";

    /// <summary>The valid measured loss window exceeded the canonical RPO target.</summary>
    public const string RpoExceededDeviation = "rpo_exceeded";

    /// <summary>The pre-fault EventStore commit was not retained.</summary>
    public const string PreFaultCommitMissingDeviation = "pre_fault_commit_missing";

    /// <summary>The sandbox did not prove deliberate rejection of the candidate.</summary>
    public const string CandidateNotRejectedDeviation = "candidate_not_rejected";

    /// <summary>The deliberately rejected candidate was found in durable state.</summary>
    public const string CandidateCommittedDeviation = "candidate_committed";

    /// <summary>The post-recovery EventStore commit was not retained.</summary>
    public const string PostRecoveryCommitMissingDeviation = "post_recovery_commit_missing";

    /// <summary>The control tenant changed during the controlled-loss scenario.</summary>
    public const string TenantIsolationDeviation = "tenant_isolation_breached";

    /// <summary>An unauthorized mutation was observed during the controlled-loss scenario.</summary>
    public const string UnauthorizedMutationDeviation = "unauthorized_mutation_detected";

    /// <summary>Sandbox state could not be cleaned after the controlled-loss scenario.</summary>
    public const string CleanupIncompleteDeviation = "cleanup_incomplete";

    /// <summary>Computes RPO exclusively from persisted EventStore commit timestamps.</summary>
    public static TimeSpan MeasureRpo(ControlledLossPathMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        return measurement.PostRecoveryCommittedAtUtc - measurement.PreFaultCommittedAtUtc;
    }

    /// <summary>Returns stable deviations for invalid bounds, target misses, and structural safety failures.</summary>
    public static IReadOnlyList<string> Deviations(ControlledLossPathMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        List<string> deviations = [];
        bool boundsValid = BoundsAreValid(measurement);
        if (!boundsValid)
        {
            deviations.Add(InvalidDurableBoundsDeviation);
        }
        else
        {
            TimeSpan rpo = MeasureRpo(measurement);
            if (rpo <= TimeSpan.Zero)
            {
                deviations.Add(NonPositiveRpoDeviation);
            }
            else if (rpo > RecoveryTargets.MaxRpo)
            {
                deviations.Add(RpoExceededDeviation);
            }
        }

        if (!measurement.PreFaultRetained)
        {
            deviations.Add(PreFaultCommitMissingDeviation);
        }

        if (!measurement.CandidateRejected)
        {
            deviations.Add(CandidateNotRejectedDeviation);
        }

        if (!measurement.CandidateAbsent)
        {
            deviations.Add(CandidateCommittedDeviation);
        }

        if (!measurement.PostRecoveryRetained)
        {
            deviations.Add(PostRecoveryCommitMissingDeviation);
        }

        if (!measurement.TenantIsolationPreserved)
        {
            deviations.Add(TenantIsolationDeviation);
        }

        if (!measurement.UnauthorizedMutationAbsent)
        {
            deviations.Add(UnauthorizedMutationDeviation);
        }

        if (!measurement.CleanupComplete)
        {
            deviations.Add(CleanupIncompleteDeviation);
        }

        return deviations;
    }

    /// <summary>Returns met only for a valid positive RPO at or below the canonical target with all safety facts true.</summary>
    public static string Evaluate(ControlledLossPathMeasurement measurement)
    {
        IReadOnlyList<string> deviations = Deviations(measurement);
        if (deviations.Any(static deviation => deviation is not RpoExceededDeviation))
        {
            return ControlledLossPathVerdicts.Unmeasurable;
        }

        return deviations.Contains(RpoExceededDeviation, StringComparer.Ordinal)
            ? ControlledLossPathVerdicts.Missed
            : ControlledLossPathVerdicts.Met;
    }

    /// <summary>
    /// Validates each clock domain independently, positive sequences, and canonical distinct ULID identities.
    /// EventStore commit bounds are ordered against each other because they share the persistence clock; sandbox
    /// candidate time and runner start/end time are never ordered against EventStore.
    /// </summary>
    public static bool BoundsAreValid(ControlledLossPathMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        string[] identities =
        [
            measurement.PreFaultRetainedRef,
            measurement.PreFaultEventRef,
            measurement.RejectedCandidateRef,
            measurement.PostRecoveryRetainedRef,
            measurement.PostRecoveryEventRef,
        ];
        return identities.All(RecoveryValidationEvidenceManifest.IsCanonicalUlid) &&
            identities.Distinct(StringComparer.Ordinal).Count() == identities.Length &&
            measurement.PreFaultSequence > 0 &&
            measurement.PostRecoverySequence > 0 &&
            measurement.StartedAtUtc.Offset == TimeSpan.Zero &&
            measurement.EndedAtUtc.Offset == TimeSpan.Zero &&
            measurement.StartedAtUtc <= measurement.EndedAtUtc &&
            measurement.PreFaultCommittedAtUtc.Offset == TimeSpan.Zero &&
            measurement.RejectedAtUtc.Offset == TimeSpan.Zero &&
            measurement.PostRecoveryCommittedAtUtc.Offset == TimeSpan.Zero &&
            measurement.PreFaultCommittedAtUtc <= measurement.PostRecoveryCommittedAtUtc;
    }
}
