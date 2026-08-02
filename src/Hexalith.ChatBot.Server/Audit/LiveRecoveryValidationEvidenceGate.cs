namespace Hexalith.ChatBot.Server.Audit;

/// <summary>Evaluates fresh, complete, positively covered live evidence without changing canonical report verdicts.</summary>
internal static class LiveRecoveryValidationEvidenceGate
{
    /// <summary>
    /// The measurement keys whose targets the harness <b>restoration budget</b> actually bounds, and therefore the only
    /// ones for which a target above <see cref="RecoveryValidationEvidenceManifest.MeasurableRecoveryCeilingSeconds"/>
    /// is a real limit on what the pass may be cited for.
    /// <para>
    /// Both are recovery-duration targets: the lane cancels a recovery that outruns its restoration budget, so a slower
    /// one converts to <c>unmeasurable</c> rather than <c>missed</c>. RPO is a data-loss window and NFR41
    /// scope-recording latency is measured <i>during</i> the outage — neither is bounded by how long restoration is
    /// allowed to take, so comparing them against the restoration ceiling emitted a category error on every passing run.
    /// </para>
    /// </summary>
    private static readonly IReadOnlySet<string> RestorationBoundedMeasurementKeys =
        new HashSet<string>(StringComparer.Ordinal) { "rto", "rebuild-duration" };


    /// <summary>Returns a stop-ship decision for anything short of a valid latest three-job evidence attempt.</summary>
    public static LiveRecoveryValidationEvidenceGateDecision Evaluate(
        LiveRecoveryValidationEvidenceAttempt attempt,
        LiveRecoveryValidationGatePolicy policy,
        DateTimeOffset evaluatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.MaximumEvidenceAge <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                policy.MaximumEvidenceAge,
                "The maximum evidence age must be positive.");
        }

        List<string> stopShip = [];
        List<string> targetDeviations = [];
        List<string> claimLimitations = [];
        Dictionary<string, int> counts = new(StringComparer.Ordinal);

        // A gate that throws on a malformed artifact still fails closed, but it reports a stack trace instead of a
        // reason code. Manifests are deserialized from disk, where every reference member is nullable in practice.
        IReadOnlyList<RecoveryValidationEvidenceManifest> evidence = attempt.Evidence ?? [];
        if (attempt.Evidence is null || evidence.Any(static manifest => manifest is null))
        {
            stopShip.Add("attempt_evidence_unreadable");
            evidence = [.. evidence.Where(static manifest => manifest is not null)];
        }

        if (attempt.AlertsDeliveredByJob is null)
        {
            stopShip.Add("attempt_alert_counts_unreadable");
        }

        if (!attempt.Enabled)
        {
            stopShip.Add("live_validation_disabled");
        }

        if (!RecoveryValidationEvidenceManifest.IsCanonicalUlid(attempt.RunId) ||
            attempt.StartedAtUtc.Offset != TimeSpan.Zero ||
            evaluatedAtUtc.Offset != TimeSpan.Zero)
        {
            stopShip.Add("attempt_provenance_invalid");
        }

        if (!attempt.LatestAttemptCompletedSuccessfully || attempt.CompletedAtUtc is null)
        {
            stopShip.Add("latest_attempt_incomplete");
        }
        else if (attempt.CompletedAtUtc.Value.Offset != TimeSpan.Zero ||
            attempt.CompletedAtUtc.Value < attempt.StartedAtUtc)
        {
            stopShip.Add("attempt_bounds_invalid");
        }
        else if (attempt.CompletedAtUtc.Value > evaluatedAtUtc)
        {
            stopShip.Add("result_timestamp_in_future");
        }
        else if (evaluatedAtUtc - attempt.CompletedAtUtc.Value > policy.MaximumEvidenceAge)
        {
            stopShip.Add("stale_result");
        }

        IReadOnlyList<string> configuredDatasets = policy.ConfiguredProjectionDatasets ?? [];
        if (configuredDatasets.Count == 0 ||
            configuredDatasets.Any(static dataset => !AuditMetadata.IsSafeStableIdentifier(dataset)) ||
            configuredDatasets.Distinct(StringComparer.Ordinal).Count() != configuredDatasets.Count)
        {
            stopShip.Add($"{LiveRecoveryValidationJobs.ProjectionRebuild}:invalid_expected_scenarios");
        }

        foreach (RecoveryValidationEvidenceManifest manifest in evidence)
        {
            if (manifest.Validate().Count > 0)
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:invalid_evidence");
            }

            if (!string.Equals(manifest.RunId, attempt.RunId, StringComparison.Ordinal))
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:run_mismatch");
            }

            if (!string.Equals(manifest.JobId, manifest.ReportKind, StringComparison.Ordinal))
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:job_kind_mismatch");
            }

            if (!LiveRecoveryValidationJobs.All.Contains(manifest.JobId))
            {
                stopShip.Add("unknown_live_job:evidence_rejected");
            }

            // Rejects a manifest naming a non-live mode. Note this is NOT proof of a live run: the sink declares the
            // token rather than deriving it from the driver, so the anti-fake weight sits in the commit, dataset and
            // volume anchoring below, which the release path supplies and the run cannot choose.
            if (!string.Equals(manifest.DriverMode, policy.RequiredDriverMode, StringComparison.Ordinal))
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:driver_mode_not_live");
            }

            // Cross-manifest coherence proves the manifests agree with each other, not that they describe the subject
            // the release path expects. Without these the run still declared how much it had exercised and which tree
            // it came from: a one-record dataset, or evidence from an unrelated commit, satisfied the gate.
            if (policy.ExpectedDatasetVersion is { } expectedDatasetVersion &&
                !string.Equals(manifest.DatasetVersion, expectedDatasetVersion, StringComparison.Ordinal))
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:dataset_version_unexpected");
            }

            if (policy.MinimumDatasetVolume > 0 && manifest.DatasetVolume < policy.MinimumDatasetVolume)
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:dataset_volume_below_minimum");
            }

            if (policy.RequiredRepositoryCommit is { } requiredCommit &&
                !string.Equals(manifest.RepositoryCommit, requiredCommit, StringComparison.OrdinalIgnoreCase))
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:repository_commit_unexpected");
            }

            // An inflated ceiling silences the claim-limitation channel, so the release path bounds what the run may
            // claim it was able to measure.
            if (policy.MaximumMeasurableRecoveryCeilingSeconds > 0 &&
                manifest.MeasurableRecoveryCeilingSeconds > policy.MaximumMeasurableRecoveryCeilingSeconds)
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:measurable_ceiling_overstated");
            }

            // A matching RunId is not proof of freshness. Without bounding each manifest against both the attempt
            // window and the evaluation clock, evidence carried over from an earlier or clock-skewed run is accepted.
            if (manifest.StartedAtUtc.Offset != TimeSpan.Zero ||
                manifest.EndedAtUtc.Offset != TimeSpan.Zero ||
                manifest.EndedAtUtc < manifest.StartedAtUtc)
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:manifest_bounds_invalid");
            }
            else if (manifest.EndedAtUtc > evaluatedAtUtc)
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:manifest_timestamp_in_future");
            }
            else if (evaluatedAtUtc - manifest.EndedAtUtc > policy.MaximumEvidenceAge)
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:manifest_stale");
            }

            if (manifest.StartedAtUtc < attempt.StartedAtUtc ||
                (attempt.CompletedAtUtc is { } attemptCompletedAtUtc && manifest.EndedAtUtc > attemptCompletedAtUtc))
            {
                stopShip.Add($"{SafeJob(manifest.JobId)}:manifest_outside_attempt_window");
            }
        }

        // A shared RunId binds manifests to one attempt but not to one subject. Without this an attempt assembled from
        // a directory holding two tenants', datasets', or builds' evidence passes as a single coherent run.
        if (evidence.Count > 1 && (
            evidence.Select(static manifest => manifest.TenantRef).Distinct(StringComparer.Ordinal).Count() > 1 ||
            evidence.Select(static manifest => manifest.DatasetRef).Distinct(StringComparer.Ordinal).Count() > 1 ||
            evidence.Select(static manifest => manifest.DatasetVersion).Distinct(StringComparer.Ordinal).Count() > 1 ||
            evidence.Select(static manifest => manifest.DatasetVolume).Distinct().Count() > 1 ||
            evidence.Select(static manifest => manifest.RepositoryCommit).Distinct(StringComparer.Ordinal).Count() > 1))
        {
            stopShip.Add("attempt_evidence_incoherent");
        }

        EvaluateJob(
            attempt,
            evidence,
            LiveRecoveryValidationJobs.Continuity,
            ContinuityDrillScenarios.All,
            stopShip,
            targetDeviations,
            claimLimitations,
            counts);
        EvaluateJob(
            attempt,
            evidence,
            LiveRecoveryValidationJobs.ProjectionRebuild,
            configuredDatasets.ToHashSet(StringComparer.Ordinal),
            stopShip,
            targetDeviations,
            claimLimitations,
            counts);
        EvaluateJob(
            attempt,
            evidence,
            LiveRecoveryValidationJobs.ScopedOutage,
            ScopedOutageDependencies.All,
            stopShip,
            targetDeviations,
            claimLimitations,
            counts);

        if (policy.TargetDeviationsBlockRelease)
        {
            stopShip.AddRange(targetDeviations);
        }

        string[] distinctStopShip = stopShip.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        string[] distinctTargets = targetDeviations.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        string[] distinctLimitations = claimLimitations.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return new LiveRecoveryValidationEvidenceGateDecision(
            distinctStopShip.Length > 0,
            distinctStopShip,
            distinctTargets,
            distinctLimitations,
            counts);
    }

    /// <summary>
    /// Returns the measurement keys a job must publish, mapped to the canonical product target each is judged against.
    /// <para>
    /// The gate owns these numbers. Reading the allowed target out of the manifest instead means the run declares the
    /// bar it is measured by, and a manifest carrying <c>rto = 20000</c> against a self-declared allowance of
    /// <c>21600</c> passes although <see cref="RecoveryTargets.MaxRto"/> is <c>14400</c>.
    /// </para>
    /// </summary>
    internal static IReadOnlyDictionary<string, double> CanonicalTargetsFor(string jobId)
        => jobId switch
        {
            LiveRecoveryValidationJobs.Continuity => new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["rpo"] = RecoveryTargets.MaxRpo.TotalSeconds,
                ["rto"] = RecoveryTargets.MaxRto.TotalSeconds,
            },
            LiveRecoveryValidationJobs.ProjectionRebuild => new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["rebuild-duration"] = RecoveryTargets.MaxRto.TotalSeconds,
            },
            LiveRecoveryValidationJobs.ScopedOutage => new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["scope-recording-latency"] = RecoveryTargets.MaxScopeRecordingLatency.TotalSeconds,
            },
            _ => new Dictionary<string, double>(StringComparer.Ordinal),
        };

    /// <summary>
    /// Returns every assertion name a job's manifest must carry. Absence is stop-ship: a manifest that simply omits
    /// <c>tenant-isolation-preserved</c> must not pass because nothing looked for it.
    /// </summary>
    internal static IReadOnlyList<string> RequiredAssertionsFor(string jobId)
        => jobId switch
        {
            LiveRecoveryValidationJobs.Continuity =>
            [
                "cleanup-complete", "data-loss-absent", "fault-observed", "recovery-observed",
                "state-reconstructable", "tenant-isolation-preserved", "unauthorized-mutation-absent", "measurable",
            ],
            LiveRecoveryValidationJobs.ProjectionRebuild =>
            [
                "cleanup-complete", "duration-within-target", "immutable-source-only",
                "mailbox-reingestion-absent", "structurally-equivalent", "tenant-isolation-preserved",
            ],
            LiveRecoveryValidationJobs.ScopedOutage =>
            [
                "cleanup-complete", "fault-observed", "recovery-observed", "independent-control-succeeded",
                "control-tenant-isolated", "scope-recorded-within-target", "scope-contained",
                "cross-tenant-leakage-absent", "unauthorized-mutation-absent", "silent-data-loss-absent",
                "inflight-items-recoverable", "duplicate-side-effect-absent",
            ],
            _ => [],
        };

    /// <summary>
    /// Returns the subset of <see cref="RequiredAssertionsFor"/> whose falsity is a stop-ship structural or safety
    /// breach. Deliberately excludes <c>duration-within-target</c> and <c>scope-recorded-within-target</c>, which are
    /// measurable target misses and stay in the deviation channel per Task 6; <c>cleanup-complete</c>, which keeps its
    /// own <c>{job}:cleanup_incomplete</c> code; and <c>data-loss-absent</c>/<c>structurally-equivalent</c>/
    /// <c>scope-contained</c>, which are already routed through <see cref="IsStructuralBreach"/>.
    /// </summary>
    internal static IReadOnlyList<string> SafetyAssertionsFor(string jobId)
        => jobId switch
        {
            LiveRecoveryValidationJobs.Continuity =>
            [
                "fault-observed", "recovery-observed", "state-reconstructable",
                "tenant-isolation-preserved", "unauthorized-mutation-absent", "measurable",
            ],
            LiveRecoveryValidationJobs.ProjectionRebuild =>
            [
                "immutable-source-only", "mailbox-reingestion-absent", "tenant-isolation-preserved",
            ],
            LiveRecoveryValidationJobs.ScopedOutage =>
            [
                "fault-observed", "recovery-observed", "independent-control-succeeded", "control-tenant-isolated",
                "cross-tenant-leakage-absent", "unauthorized-mutation-absent", "silent-data-loss-absent",
                "inflight-items-recoverable", "duplicate-side-effect-absent",
            ],
            _ => [],
        };

    private static void EvaluateJob(
        LiveRecoveryValidationEvidenceAttempt attempt,
        IReadOnlyList<RecoveryValidationEvidenceManifest> allEvidence,
        string jobId,
        IReadOnlySet<string> expectedScenarios,
        List<string> stopShip,
        List<string> targetDeviations,
        List<string> claimLimitations,
        Dictionary<string, int> counts)
    {
        RecoveryValidationEvidenceManifest[] evidence = allEvidence
            .Where(manifest => string.Equals(manifest.JobId, jobId, StringComparison.Ordinal))
            .ToArray();
        counts[jobId] = evidence.Length;
        if (evidence.Length == 0)
        {
            stopShip.Add($"{jobId}:missing_evidence");
            return;
        }

        HashSet<string> actualScenarios = evidence.Select(static manifest => manifest.Scenario).ToHashSet(StringComparer.Ordinal);
        if (evidence.Length != expectedScenarios.Count || !actualScenarios.SetEquals(expectedScenarios))
        {
            stopShip.Add($"{jobId}:incomplete_scenario_set");
        }

        IReadOnlyDictionary<string, double> canonicalTargets = CanonicalTargetsFor(jobId);
        int alertsRequired = 0;
        foreach (RecoveryValidationEvidenceManifest manifest in evidence)
        {
            // Already stop-shipped as `{job}:invalid_evidence` by the caller. Every check below dereferences a
            // dictionary that a structurally incomplete manifest may not carry, so a fail-closed gate must not
            // continue into them and turn a reason code into a NullReferenceException.
            if (manifest.MeasurementsSeconds is null || manifest.AllowedTargetsSeconds is null ||
                manifest.Assertions is null || manifest.Coverage is null)
            {
                continue;
            }

            if (manifest.Coverage.Count == 0 || manifest.Coverage.Values.Any(static value => value <= 0))
            {
                stopShip.Add($"{jobId}:zero_coverage");
            }

            if (!Assertion(manifest, "cleanup-complete"))
            {
                stopShip.Add($"{jobId}:cleanup_incomplete");
            }

            // Assertion NAMES already fail closed on absence (Assertion() returns false for a missing key), but
            // measurement keys did not: a continuity manifest publishing only an unrelated key passed with verdict
            // `met` and no RPO or RTO recorded anywhere. The required vocabulary closes that.
            foreach ((string key, double canonicalTarget) in canonicalTargets)
            {
                if (!manifest.MeasurementsSeconds.ContainsKey(key))
                {
                    stopShip.Add($"{jobId}:measurement_missing");
                    continue;
                }

                if (!manifest.AllowedTargetsSeconds.TryGetValue(key, out double declaredTarget) ||
                    Math.Abs(declaredTarget - canonicalTarget) > 0.000_001)
                {
                    stopShip.Add($"{jobId}:target_not_canonical");
                }

                // Not a breach and not a deviation: the run met the target it could measure. It is a limit on what the
                // pass may be cited as evidence for, and it travels with the decision so nobody has to infer it.
                //
                // Scoped to the keys the restoration budget genuinely bounds. The projection-rebuild job is measured
                // against the same 4-hour target inside the same bounded lane, so restricting this to `rto` let an
                // NFR57 pass be cited for a target the run could never have missed; widening it to EVERY key was the
                // opposite error, judging RPO and scope-recording latency against a budget that does not bound them.
                if (RestorationBoundedMeasurementKeys.Contains(key) &&
                    canonicalTarget > manifest.MeasurableRecoveryCeilingSeconds)
                {
                    claimLimitations.Add($"{jobId}:{key}:target_exceeds_measurable_ceiling");
                }
            }

            if (manifest.MeasurementsSeconds.Keys.Any(key => !manifest.AllowedTargetsSeconds.ContainsKey(key)))
            {
                stopShip.Add($"{jobId}:measurement_target_missing");
            }

            // A key outside the canonical vocabulary carries an allowance nothing anchors, so the run would be
            // declaring its own bar for it. The canonical set is closed; anything else is rejected rather than ignored.
            if (manifest.MeasurementsSeconds.Keys.Any(key => !canonicalTargets.ContainsKey(key)))
            {
                stopShip.Add($"{jobId}:non_canonical_measurement_key");
            }

            // Assertion NAMES fail closed on absence only if something reads them. The gate previously consulted six
            // of the twenty the sink writes, so `fault-observed: false` beside verdict `met` passed with no reason
            // code — a driver regression that never injected the fault was indistinguishable from a clean drill.
            //
            // Every name below is structural or an NFR57/NFR59 safety invariant, never a measurable target miss:
            // Task 6 defines the deviation bucket exhaustively as RPO/RTO, rebuild duration and scope-recording
            // latency, and all three are measurement keys handled above. `duration-within-target` and
            // `scope-recorded-within-target` therefore stay in the deviation channel via IsTargetDeviation, and
            // `data-loss-absent`/`structurally-equivalent`/`scope-contained` stay in IsStructuralBreach.
            foreach (string assertionName in RequiredAssertionsFor(jobId))
            {
                if (!manifest.Assertions.ContainsKey(assertionName))
                {
                    stopShip.Add($"{jobId}:{assertionName}:assertion_missing");
                }
            }

            foreach (string assertionName in SafetyAssertionsFor(jobId))
            {
                if (manifest.Assertions.TryGetValue(assertionName, out bool passed) && !passed)
                {
                    stopShip.Add($"{jobId}:{assertionName}:assertion_failed");
                }
            }

            bool unmeasurable = IsUnmeasurable(jobId, manifest.Verdict);
            bool structuralBreach = IsStructuralBreach(jobId, manifest);
            bool targetDeviation = IsTargetDeviation(jobId, manifest) || ExceedsCanonicalTarget(manifest, canonicalTargets);
            if (!IsKnownVerdict(jobId, manifest.Verdict))
            {
                stopShip.Add($"{jobId}:unknown_verdict");
            }

            if (unmeasurable)
            {
                stopShip.Add($"{jobId}:unmeasurable");
            }

            if (structuralBreach)
            {
                stopShip.Add($"{jobId}:structural_breach");
            }

            if (targetDeviation)
            {
                targetDeviations.Add($"{jobId}:target_deviation");
            }

            if (unmeasurable || structuralBreach || targetDeviation)
            {
                alertsRequired++;
            }
        }

        int alertsDelivered = attempt.AlertsDeliveredByJob?.GetValueOrDefault(jobId) ?? 0;
        if (alertsDelivered < alertsRequired)
        {
            stopShip.Add($"{jobId}:unalerted_breach");
        }
    }

    private static bool IsUnmeasurable(string jobId, string verdict)
        => jobId switch
        {
            LiveRecoveryValidationJobs.Continuity => string.Equals(verdict, ContinuityDrillVerdicts.Unmeasurable, StringComparison.Ordinal),
            LiveRecoveryValidationJobs.ProjectionRebuild => string.Equals(verdict, ProjectionRebuildVerdicts.Unmeasurable, StringComparison.Ordinal),
            LiveRecoveryValidationJobs.ScopedOutage => string.Equals(verdict, ScopedOutageDegradationVerdicts.Unmeasurable, StringComparison.Ordinal),
            _ => true,
        };

    private static bool IsKnownVerdict(string jobId, string verdict)
        => jobId switch
        {
            LiveRecoveryValidationJobs.Continuity => verdict is
                ContinuityDrillVerdicts.Met or ContinuityDrillVerdicts.Missed or ContinuityDrillVerdicts.Unmeasurable,
            LiveRecoveryValidationJobs.ProjectionRebuild => verdict is
                ProjectionRebuildVerdicts.Equivalent or ProjectionRebuildVerdicts.Divergent or ProjectionRebuildVerdicts.Unmeasurable,
            LiveRecoveryValidationJobs.ScopedOutage => verdict is
                ScopedOutageDegradationVerdicts.Contained or ScopedOutageDegradationVerdicts.Breached or ScopedOutageDegradationVerdicts.Unmeasurable,
            _ => false,
        };

    private static bool IsStructuralBreach(string jobId, RecoveryValidationEvidenceManifest manifest)
        => jobId switch
        {
            LiveRecoveryValidationJobs.Continuity => !Assertion(manifest, "data-loss-absent"),
            LiveRecoveryValidationJobs.ProjectionRebuild =>
                string.Equals(manifest.Verdict, ProjectionRebuildVerdicts.Divergent, StringComparison.Ordinal) ||
                !Assertion(manifest, "structurally-equivalent"),
            LiveRecoveryValidationJobs.ScopedOutage =>
                string.Equals(manifest.Verdict, ScopedOutageDegradationVerdicts.Breached, StringComparison.Ordinal) ||
                !Assertion(manifest, "scope-contained"),
            _ => true,
        };

    private static bool IsTargetDeviation(string jobId, RecoveryValidationEvidenceManifest manifest)
        => jobId switch
        {
            LiveRecoveryValidationJobs.Continuity =>
                string.Equals(manifest.Verdict, ContinuityDrillVerdicts.Missed, StringComparison.Ordinal) &&
                Assertion(manifest, "data-loss-absent"),
            LiveRecoveryValidationJobs.ProjectionRebuild => !Assertion(manifest, "duration-within-target"),
            LiveRecoveryValidationJobs.ScopedOutage => !Assertion(manifest, "scope-recorded-within-target"),
            _ => false,
        };

    /// <summary>
    /// Returns whether any measured value exceeded the <b>canonical</b> product target for its key. This is a
    /// measurable target deviation, never a structural or unmeasurable breach, so it stays in the deviation channel.
    /// </summary>
    private static bool ExceedsCanonicalTarget(
        RecoveryValidationEvidenceManifest manifest,
        IReadOnlyDictionary<string, double> canonicalTargets)
        => manifest.MeasurementsSeconds.Any(measurement =>
            canonicalTargets.TryGetValue(measurement.Key, out double allowed)
                ? measurement.Value > allowed
                : manifest.AllowedTargetsSeconds.TryGetValue(measurement.Key, out double declared) &&
                    measurement.Value > declared);

    private static bool Assertion(RecoveryValidationEvidenceManifest manifest, string name)
        => manifest.Assertions.TryGetValue(name, out bool passed) && passed;

    private static string SafeJob(string jobId)
        => LiveRecoveryValidationJobs.All.Contains(jobId) ? jobId : "unknown_live_job";
}
