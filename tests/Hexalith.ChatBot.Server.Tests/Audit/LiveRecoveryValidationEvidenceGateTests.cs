using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>Exhaustive fail-closed coverage for the externally scheduled live-recovery evidence gate.</summary>
public sealed class LiveRecoveryValidationEvidenceGateTests
{
    private const string RunId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CompleteFreshPositivelyCoveredThreeJobAttemptPasses()
    {
        LiveRecoveryValidationEvidenceGateDecision decision = Evaluate(CompleteAttempt());

        decision.IsStopShip.ShouldBeFalse();
        decision.StopShipReasons.ShouldBeEmpty();
        decision.TargetDeviationReasons.ShouldBeEmpty();
        decision.EvidenceCounts[LiveRecoveryValidationJobs.Continuity].ShouldBe(2);
        decision.EvidenceCounts[LiveRecoveryValidationJobs.ProjectionRebuild].ShouldBe(1);
        decision.EvidenceCounts[LiveRecoveryValidationJobs.ScopedOutage].ShouldBe(6);
    }

    [Fact]
    public void DisabledIncompleteFutureStaleAndMissingCoverageStatesFailClosed()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();

        Evaluate(complete with { Enabled = false }).StopShipReasons.ShouldContain("live_validation_disabled");
        Evaluate(complete with { LatestAttemptCompletedSuccessfully = false }).StopShipReasons
            .ShouldContain("latest_attempt_incomplete");
        Evaluate(complete with { CompletedAtUtc = Now + TimeSpan.FromTicks(1) }).StopShipReasons
            .ShouldContain("result_timestamp_in_future");
        Evaluate(complete with
        {
            StartedAtUtc = Now - TimeSpan.FromDays(2) - TimeSpan.FromMinutes(1),
            CompletedAtUtc = Now - TimeSpan.FromDays(2),
        }).StopShipReasons
            .ShouldContain("stale_result");

        RecoveryValidationEvidenceManifest[] missing = complete.Evidence
            .Where(manifest => manifest.Scenario != ScopedOutageDependencies.Identity)
            .ToArray();
        Evaluate(complete with { Evidence = missing }).StopShipReasons
            .ShouldContain($"{LiveRecoveryValidationJobs.ScopedOutage}:incomplete_scenario_set");

        RecoveryValidationEvidenceManifest[] zeroCoverage = complete.Evidence.ToArray();
        zeroCoverage[0] = zeroCoverage[0] with { Coverage = new Dictionary<string, int> { ["scenario"] = 0 } };
        // Zero coverage is recordable metadata but is never releasable: the gate is the single authority that
        // rejects it, so an unmeasurable report can still be written to disk with its real reason code intact.
        LiveRecoveryValidationEvidenceGateDecision zero = Evaluate(complete with { Evidence = zeroCoverage });
        zero.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:zero_coverage");
        zero.StopShipReasons.ShouldNotContain($"{LiveRecoveryValidationJobs.Continuity}:invalid_evidence");

        RecoveryValidationEvidenceManifest unknownJob = complete.Evidence[0] with
        {
            JobId = "unknown-job",
            ReportKind = "unknown-job",
        };
        Evaluate(complete with { Evidence = [.. complete.Evidence, unknownJob] }).StopShipReasons
            .ShouldContain("unknown_live_job:evidence_rejected");

        RecoveryValidationEvidenceManifest[] unknownVerdict = complete.Evidence.ToArray();
        unknownVerdict[0] = unknownVerdict[0] with { Verdict = "unknown-verdict" };
        Evaluate(complete with { Evidence = unknownVerdict }).StopShipReasons
            .ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:unknown_verdict");
    }

    [Fact]
    public void UnmeasurableStructuralCleanupAndUnalertedBreachReasonsRemainDistinct()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        int continuityIndex = Array.FindIndex(
            evidence,
            manifest => manifest.Scenario == ContinuityDrillScenarios.EventStoreOutage);
        evidence[continuityIndex] = evidence[continuityIndex] with
        {
            Verdict = ContinuityDrillVerdicts.Unmeasurable,
            Assertions = new Dictionary<string, bool>
            {
                ["cleanup-complete"] = false,
                ["data-loss-absent"] = false,
            },
        };

        LiveRecoveryValidationEvidenceGateDecision decision = Evaluate(complete with { Evidence = evidence });

        decision.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:unmeasurable");
        decision.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:structural_breach");
        decision.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:cleanup_incomplete");
        decision.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:unalerted_breach");
    }

    [Fact]
    public void PassingVerdictWithFailedCleanupAloneStopShipsAsCleanupIncomplete()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        int continuityIndex = Array.FindIndex(
            evidence,
            manifest => manifest.Scenario == ContinuityDrillScenarios.EventStoreOutage);
        Dictionary<string, bool> assertions = new(evidence[continuityIndex].Assertions, StringComparer.Ordinal)
        {
            ["cleanup-complete"] = false,
        };
        evidence[continuityIndex] = evidence[continuityIndex] with
        {
            Verdict = ContinuityDrillVerdicts.Met,
            Assertions = assertions,
        };

        LiveRecoveryValidationEvidenceGateDecision decision = Evaluate(complete with { Evidence = evidence });

        decision.IsStopShip.ShouldBeTrue();
        decision.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:cleanup_incomplete");
        decision.StopShipReasons.ShouldNotContain($"{LiveRecoveryValidationJobs.Continuity}:unmeasurable");
    }

    [Fact]
    public void TargetDeviationIsVisibleAndOnlyBlocksWhenTheApprovedPolicyRequiresIt()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        int continuityIndex = Array.FindIndex(
            evidence,
            manifest => manifest.Scenario == ContinuityDrillScenarios.EventStoreOutage);
        evidence[continuityIndex] = evidence[continuityIndex] with
        {
            Verdict = ContinuityDrillVerdicts.Missed,
            Deviations = [ContinuityDrillEvaluator.RpoExceededDeviation],
        };
        Dictionary<string, int> alerts = new(complete.AlertsDeliveredByJob, StringComparer.Ordinal)
        {
            [LiveRecoveryValidationJobs.Continuity] = 1,
        };

        LiveRecoveryValidationEvidenceGateDecision advisory = Evaluate(
            complete with { Evidence = evidence, AlertsDeliveredByJob = alerts },
            Policy() with { TargetDeviationsBlockRelease = false });
        advisory.IsStopShip.ShouldBeFalse();
        advisory.TargetDeviationReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:target_deviation");
        advisory.StopShipReasons.ShouldNotContain($"{LiveRecoveryValidationJobs.Continuity}:target_deviation");

        LiveRecoveryValidationEvidenceGateDecision blocking = Evaluate(
            complete with { Evidence = evidence, AlertsDeliveredByJob = alerts });
        blocking.IsStopShip.ShouldBeTrue();
        blocking.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:target_deviation");
    }

    [Fact]
    public void EvidenceProducedByANonLiveDriverIsRejected()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        evidence[0] = evidence[0] with { DriverMode = "scripted-fake" };

        Evaluate(complete with { Evidence = evidence }).StopShipReasons
            .ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:driver_mode_not_live");
    }

    [Fact]
    public void MissingRequiredMeasurementsAndNonCanonicalTargetsAreRejected()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();

        // A manifest may not simply omit the measurement its job exists to produce: without a required vocabulary a
        // continuity drill passed as `met` while publishing no RPO and no RTO at all.
        RecoveryValidationEvidenceManifest[] missingMeasurement = complete.Evidence.ToArray();
        missingMeasurement[0] = missingMeasurement[0] with
        {
            MeasurementsSeconds = new Dictionary<string, double> { ["rpo"] = 1 },
            AllowedTargetsSeconds = new Dictionary<string, double> { ["rpo"] = RecoveryTargets.MaxRpo.TotalSeconds },
        };
        Evaluate(complete with { Evidence = missingMeasurement }).StopShipReasons
            .ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:measurement_missing");

        // Nor may it declare the bar it is judged by. RecoveryTargets is the gate's number, not the run's.
        RecoveryValidationEvidenceManifest[] inflatedTarget = complete.Evidence.ToArray();
        inflatedTarget[0] = inflatedTarget[0] with
        {
            MeasurementsSeconds = new Dictionary<string, double> { ["rpo"] = 1, ["rto"] = 20_000 },
            AllowedTargetsSeconds = new Dictionary<string, double>
            {
                ["rpo"] = RecoveryTargets.MaxRpo.TotalSeconds,
                ["rto"] = 21_600,
            },
        };
        LiveRecoveryValidationEvidenceGateDecision inflated = Evaluate(complete with { Evidence = inflatedTarget });
        inflated.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:target_not_canonical");
        inflated.TargetDeviationReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:target_deviation");
    }

    [Fact]
    public void ATargetWiderThanTheLaneCanMeasureIsDisclosedWithoutBlockingRelease()
    {
        LiveRecoveryValidationEvidenceGateDecision decision = Evaluate(CompleteAttempt());

        decision.IsStopShip.ShouldBeFalse();
        decision.ClaimLimitationReasons
            .ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:rto:target_exceeds_measurable_ceiling");
    }

    [Fact]
    public void RepositoryCommitCoherenceIsCaseInsensitive()
    {
        // Two manifests naming the same commit in different case (e.g. from two different CI runners) must remain a
        // coherent attempt — only a genuinely different commit is incoherent.
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        evidence[0] = evidence[0] with { RepositoryCommit = evidence[0].RepositoryCommit.ToUpperInvariant() };

        Evaluate(complete with { Evidence = evidence }).StopShipReasons
            .ShouldNotContain("attempt_evidence_incoherent");
    }

    [Fact]
    public void EvidenceFromTwoDifferentSubjectsIsNotOneCoherentAttempt()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        evidence[0] = evidence[0] with { TenantRef = "replay-test:other-validation" };

        Evaluate(complete with { Evidence = evidence }).StopShipReasons.ShouldContain("attempt_evidence_incoherent");
    }

    [Fact]
    public void EvidenceWithADivergentDatasetRefIsNotOneCoherentAttempt()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        evidence[0] = evidence[0] with { DatasetRef = "other-baseline" };

        Evaluate(complete with { Evidence = evidence }).StopShipReasons.ShouldContain("attempt_evidence_incoherent");
    }

    [Fact]
    public void EvidenceWithADivergentDatasetVersionIsNotOneCoherentAttempt()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        evidence[0] = evidence[0] with { DatasetVersion = "v2" };

        Evaluate(complete with { Evidence = evidence }).StopShipReasons.ShouldContain("attempt_evidence_incoherent");
    }

    [Fact]
    public void EvidenceWithADivergentConfiguredDatasetVolumeIsNotOneCoherentAttempt()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        evidence[0] = evidence[0] with { ConfiguredDatasetVolume = evidence[0].ConfiguredDatasetVolume + 1 };

        Evaluate(complete with { Evidence = evidence }).StopShipReasons.ShouldContain("attempt_evidence_incoherent");
    }

    [Fact]
    public void EvidenceWithAGenuinelyDifferentRepositoryCommitIsNotOneCoherentAttempt()
    {
        // Complements RepositoryCommitCoherenceIsCaseInsensitive: that test proves a case-only difference does NOT
        // trip the check; this proves a real divergence does.
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        evidence[0] = evidence[0] with { RepositoryCommit = new string('a', 40) };

        Evaluate(complete with { Evidence = evidence }).StopShipReasons.ShouldContain("attempt_evidence_incoherent");
    }

    [Fact]
    public void AManifestWithANullRunIdYieldsAReasonCodeRatherThanAnException()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        evidence[0] = evidence[0] with { RunId = null! };

        Should.NotThrow(() => Evaluate(complete with { Evidence = evidence }))
            .StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:invalid_evidence");
    }

    [Fact]
    public void AManifestWithNullAssertionsYieldsAReasonCodeRatherThanAnException()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        evidence[0] = evidence[0] with { Assertions = null! };

        Should.NotThrow(() => Evaluate(complete with { Evidence = evidence }))
            .StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:invalid_evidence");
    }

    [Fact]
    public void AManifestInvalidOnlyViaANonDictionaryFieldIsExcludedFromJobGradingNotJustInvalidEvidence()
    {
        // AManifestWithNullAssertionsYieldsAReasonCodeRatherThanAnException nulls a dictionary, which the older
        // null-dictionary check inside EvaluateJob already excludes regardless of Validate(). This corrupts only a
        // non-dictionary field (RunId) with every dictionary intact and a safety assertion deliberately false, so
        // only the newer Validate()-based invalidManifests exclusion — not the null-dictionary check — can be
        // keeping it out of job-level grading.
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        int continuityIndex = Array.FindIndex(
            evidence,
            manifest => manifest.Scenario == ContinuityDrillScenarios.EventStoreOutage);
        Dictionary<string, bool> assertions = new(evidence[continuityIndex].Assertions, StringComparer.Ordinal)
        {
            ["fault-observed"] = false,
        };
        evidence[continuityIndex] = evidence[continuityIndex] with
        {
            RunId = "not-a-canonical-ulid",
            Assertions = assertions,
        };

        LiveRecoveryValidationEvidenceGateDecision decision = Evaluate(complete with { Evidence = evidence });

        decision.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:invalid_evidence");
        decision.StopShipReasons.ShouldNotContain($"{LiveRecoveryValidationJobs.Continuity}:fault-observed:assertion_failed");
    }

    [Fact]
    public void NegativeAlertDeliveryCountsAreUnreadableRatherThanUnalerted()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        Dictionary<string, int> alerts = new(complete.AlertsDeliveredByJob, StringComparer.Ordinal)
        {
            [LiveRecoveryValidationJobs.Continuity] = -1,
        };

        Evaluate(complete with { AlertsDeliveredByJob = alerts })
            .StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:attempt_alert_counts_unreadable");
        Evaluate(complete with { AlertsDeliveredByJob = alerts })
            .StopShipReasons.ShouldNotContain($"{LiveRecoveryValidationJobs.Continuity}:unalerted_breach");
    }

    [Fact]
    public void NegativeAlertDeliveryCountsAlsoStopShipAsUnalertedBreachWhenAlertsWereRequired()
    {
        // Deliberately both: the clamp only makes the delivered-vs-required comparison well-defined, it does not
        // vouch for delivery, so a job whose alert counts are unreadable AND which required an alert must still fail
        // closed on the delivery question, not just the readability one.
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        int continuityIndex = Array.FindIndex(
            evidence,
            manifest => manifest.Scenario == ContinuityDrillScenarios.EventStoreOutage);
        evidence[continuityIndex] = evidence[continuityIndex] with
        {
            Verdict = ContinuityDrillVerdicts.Unmeasurable,
        };
        Dictionary<string, int> alerts = new(complete.AlertsDeliveredByJob, StringComparer.Ordinal)
        {
            [LiveRecoveryValidationJobs.Continuity] = -1,
        };

        LiveRecoveryValidationEvidenceGateDecision decision = Evaluate(
            complete with { Evidence = evidence, AlertsDeliveredByJob = alerts });

        decision.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:attempt_alert_counts_unreadable");
        decision.StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:unalerted_breach");
    }

    [Fact]
    public void AMissingOrFailedSafetyAssertionFailsClosedForEveryJob()
    {
        // The gate consulted six of the twenty assertions the sink writes, so a driver regression that never injected
        // the fault produced `fault-observed: false` beside verdict `met` and passed with no reason code at all.
        foreach (string jobId in LiveRecoveryValidationJobs.All)
        {
            foreach (string assertion in LiveRecoveryValidationEvidenceGate.SafetyAssertionsFor(jobId))
            {
                Evaluate(AttemptWithAssertion(jobId, assertion, value: false))
                    .StopShipReasons.ShouldContain(
                        $"{jobId}:{assertion}:assertion_failed",
                        $"A false '{assertion}' must stop the release for job '{jobId}'.");

                Evaluate(AttemptWithAssertion(jobId, assertion, value: null))
                    .StopShipReasons.ShouldContain(
                        $"{jobId}:{assertion}:assertion_missing",
                        $"An omitted '{assertion}' must stop the release for job '{jobId}'.");
            }
        }
    }

    [Fact]
    public void AMeasurableTargetMissStaysADeviationRatherThanBecomingAStructuralBreach()
    {
        // Task 6 keeps rebuild duration and scope-recording latency in the deviation channel. Sweeping them into the
        // required-true set alongside the safety invariants would have collapsed a measurable miss into a breach.
        foreach ((string jobId, string assertion) in new[]
        {
            (LiveRecoveryValidationJobs.ProjectionRebuild, "duration-within-target"),
            (LiveRecoveryValidationJobs.ScopedOutage, "scope-recorded-within-target"),
        })
        {
            LiveRecoveryValidationEvidenceAttempt deviating = AttemptWithAssertion(jobId, assertion, value: false);

            // A deviation still requires its alert; without it the gate correctly stop-ships `unalerted_breach`, which
            // would mask what this test is actually asserting.
            LiveRecoveryValidationEvidenceGateDecision advisory = LiveRecoveryValidationEvidenceGate.Evaluate(
                deviating with
                {
                    AlertsDeliveredByJob = new Dictionary<string, int>(StringComparer.Ordinal) { [jobId] = 1 },
                },
                Policy() with { TargetDeviationsBlockRelease = false },
                Now);

            advisory.IsStopShip.ShouldBeFalse(string.Join(',', advisory.StopShipReasons));
            advisory.TargetDeviationReasons.ShouldContain($"{jobId}:target_deviation");
            advisory.StopShipReasons.ShouldNotContain($"{jobId}:{assertion}:assertion_failed");
        }
    }

    [Fact]
    public void PolicyAnchorsDatasetProvenanceCommitAndMeasurableCeiling()
    {
        // Cross-manifest coherence proved only that the manifests agreed with each other. A one-record dataset, an
        // inflated measurable ceiling, or evidence from an unrelated commit all stayed mutually coherent and passed.
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();

        Evaluate(complete, Policy() with { ExpectedDatasetVersion = "v2" })
            .StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:dataset_version_unexpected");
        Evaluate(complete, Policy() with { MinimumDatasetVolume = 7 })
            .StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:dataset_volume_below_minimum");
        Evaluate(complete, Policy() with { RequiredRepositoryCommit = new string('a', 40) })
            .StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:repository_commit_unexpected");
        Evaluate(complete, Policy() with { MaximumMeasurableRecoveryCeilingSeconds = 179 })
            .StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:measurable_ceiling_overstated");

        // Unpinned is still permitted, so a local diagnostic run is not forced to invent a commit.
        Evaluate(complete, Policy()).IsStopShip.ShouldBeFalse();
    }

    [Fact]
    public void DatasetVolumeReportsWhatEachScenarioActuallyExercises()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();

        RecoveryValidationEvidenceManifest[] overstatedContinuity = complete.Evidence.ToArray();
        int continuityIndex = Array.FindIndex(
            overstatedContinuity,
            static manifest => manifest.JobId == LiveRecoveryValidationJobs.Continuity);
        overstatedContinuity[continuityIndex] = overstatedContinuity[continuityIndex] with { DatasetVolume = 6 };
        Evaluate(complete with { Evidence = overstatedContinuity }).StopShipReasons
            .ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:dataset_volume_not_applicable");

        RecoveryValidationEvidenceManifest[] understatedRebuild = complete.Evidence.ToArray();
        int rebuildIndex = Array.FindIndex(
            understatedRebuild,
            static manifest => manifest.JobId == LiveRecoveryValidationJobs.ProjectionRebuild);
        understatedRebuild[rebuildIndex] = understatedRebuild[rebuildIndex] with { DatasetVolume = 0 };
        Evaluate(complete with { Evidence = understatedRebuild }).StopShipReasons
            .ShouldContain($"{LiveRecoveryValidationJobs.ProjectionRebuild}:dataset_volume_mismatch");
    }

    [Fact]
    public void AMeasurementKeyOutsideTheCanonicalVocabularyIsRejected()
    {
        LiveRecoveryValidationEvidenceAttempt attempt = MutateContinuity(manifest => manifest with
        {
            MeasurementsSeconds = new Dictionary<string, double>(manifest.MeasurementsSeconds, StringComparer.Ordinal)
            {
                ["rto-adjusted"] = 1,
            },
            AllowedTargetsSeconds = new Dictionary<string, double>(manifest.AllowedTargetsSeconds, StringComparer.Ordinal)
            {
                ["rto-adjusted"] = 99_999,
            },
        });

        Evaluate(attempt).StopShipReasons
            .ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:non_canonical_measurement_key");
    }

    [Fact]
    public void TheClaimLimitationCoversRecoveryDurationKeysOnlyNotRpoOrScopeRecordingLatency()
    {
        // RPO is a data-loss window and NFR41 latency is measured during the outage; neither is bounded by how long
        // restoration is allowed to take, so judging them against the restoration ceiling was a category error emitted
        // on every passing run.
        string[] limitations = [.. Evaluate(CompleteAttempt()).ClaimLimitationReasons];

        limitations.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:rto:target_exceeds_measurable_ceiling");
        limitations.ShouldContain($"{LiveRecoveryValidationJobs.ProjectionRebuild}:rebuild-duration:target_exceeds_measurable_ceiling");
        limitations.ShouldNotContain($"{LiveRecoveryValidationJobs.Continuity}:rpo:target_exceeds_measurable_ceiling");
        limitations.ShouldNotContain($"{LiveRecoveryValidationJobs.ScopedOutage}:scope-recording-latency:target_exceeds_measurable_ceiling");
    }

    [Fact]
    public void CanonicalTargetsAreAnchoredToTheProductRecoveryTargets()
    {
        // Pins CanonicalTargetsFor to RecoveryTargets. Every fixture above states the seconds as literals precisely so
        // this is the single place the two are tied together, rather than each proving the other.
        LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(LiveRecoveryValidationJobs.Continuity)["rpo"]
            .ShouldBe(RecoveryTargets.MaxRpo.TotalSeconds);
        LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(LiveRecoveryValidationJobs.Continuity)["rto"]
            .ShouldBe(RecoveryTargets.MaxRto.TotalSeconds);
        LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(LiveRecoveryValidationJobs.ProjectionRebuild)["rebuild-duration"]
            .ShouldBe(RecoveryTargets.MaxRto.TotalSeconds);
        LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(LiveRecoveryValidationJobs.ScopedOutage)["scope-recording-latency"]
            .ShouldBe(RecoveryTargets.MaxScopeRecordingLatency.TotalSeconds);
    }

    private static LiveRecoveryValidationEvidenceAttempt AttemptWithAssertion(string jobId, string assertion, bool? value)
        => MutateJob(jobId, manifest =>
        {
            Dictionary<string, bool> assertions = new(manifest.Assertions, StringComparer.Ordinal);
            if (value is { } present)
            {
                assertions[assertion] = present;
            }
            else
            {
                assertions.Remove(assertion);
            }

            return manifest with { Assertions = assertions };
        });

    private static LiveRecoveryValidationEvidenceAttempt MutateContinuity(
        Func<RecoveryValidationEvidenceManifest, RecoveryValidationEvidenceManifest> mutate)
        => MutateJob(LiveRecoveryValidationJobs.Continuity, mutate);

    private static LiveRecoveryValidationEvidenceAttempt MutateJob(
        string jobId,
        Func<RecoveryValidationEvidenceManifest, RecoveryValidationEvidenceManifest> mutate)
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        bool mutated = false;
        List<RecoveryValidationEvidenceManifest> evidence = [];
        foreach (RecoveryValidationEvidenceManifest manifest in complete.Evidence!)
        {
            if (!mutated && string.Equals(manifest.JobId, jobId, StringComparison.Ordinal))
            {
                evidence.Add(mutate(manifest));
                mutated = true;
                continue;
            }

            evidence.Add(manifest);
        }

        mutated.ShouldBeTrue($"No manifest was found for live recovery job '{jobId}'.");
        return complete with { Evidence = evidence };
    }

    private static LiveRecoveryValidationEvidenceGateDecision Evaluate(
        LiveRecoveryValidationEvidenceAttempt attempt,
        LiveRecoveryValidationGatePolicy? policy = null)
        => LiveRecoveryValidationEvidenceGate.Evaluate(attempt, policy ?? Policy(), Now);

    private static LiveRecoveryValidationGatePolicy Policy()
        => new(
            ConfiguredProjectionDatasets: ["recovery-baseline"],
            TargetDeviationsBlockRelease: true,
            RequiredDriverMode: RecoveryValidationEvidenceManifest.LiveDriverMode,
            MaximumEvidenceAge: TimeSpan.FromDays(1));

    private static LiveRecoveryValidationEvidenceAttempt CompleteAttempt()
    {
        List<RecoveryValidationEvidenceManifest> evidence =
        [
            Manifest(LiveRecoveryValidationJobs.Continuity, ContinuityDrillScenarios.EventStoreOutage),
            Manifest(LiveRecoveryValidationJobs.Continuity, ContinuityDrillScenarios.M365SubscriptionFailure),
            Manifest(LiveRecoveryValidationJobs.ProjectionRebuild, "recovery-baseline"),
        ];
        evidence.AddRange(ScopedOutageDependencies.All.Select(dependency =>
            Manifest(LiveRecoveryValidationJobs.ScopedOutage, dependency)));
        return new LiveRecoveryValidationEvidenceAttempt(
            Enabled: true,
            RunId,
            StartedAtUtc: Now - TimeSpan.FromMinutes(2),
            CompletedAtUtc: Now - TimeSpan.FromMinutes(1),
            LatestAttemptCompletedSuccessfully: true,
            Evidence: evidence,
            AlertsDeliveredByJob: new Dictionary<string, int>(StringComparer.Ordinal));
    }

    private static RecoveryValidationEvidenceManifest Manifest(string jobId, string scenario)
    {
        // Every assertion the sink writes, because the gate now requires the full per-job vocabulary: a manifest that
        // simply omits `tenant-isolation-preserved` must not pass merely because nothing looked for it.
        Dictionary<string, bool> assertions = LiveRecoveryValidationEvidenceGate
            .RequiredAssertionsFor(jobId)
            .ToDictionary(name => name, _ => true, StringComparer.Ordinal);
        string verdict;
        Dictionary<string, double> measurements = new(StringComparer.Ordinal);
        if (jobId == LiveRecoveryValidationJobs.Continuity)
        {
            verdict = ContinuityDrillVerdicts.Met;
            measurements["rpo"] = 0;
            measurements["rto"] = 28;
        }
        else if (jobId == LiveRecoveryValidationJobs.ProjectionRebuild)
        {
            verdict = ProjectionRebuildVerdicts.Equivalent;
            measurements["rebuild-duration"] = 0.01;
        }
        else
        {
            verdict = ScopedOutageDegradationVerdicts.Contained;
            measurements["scope-recording-latency"] = 0.001;
        }

        // Literal seconds, NOT LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(jobId). Building the fixture from
        // the same function the gate compares against made `target_not_canonical` unfalsifiable: had CanonicalTargetsFor
        // drifted from RecoveryTargets to 21_600 s, every one of these tests still passed. These values pin the product
        // targets independently, so a drift in either direction fails here.
        Dictionary<string, double> targets = jobId switch
        {
            LiveRecoveryValidationJobs.Continuity => new(StringComparer.Ordinal) { ["rpo"] = 900, ["rto"] = 14_400 },
            LiveRecoveryValidationJobs.ProjectionRebuild => new(StringComparer.Ordinal) { ["rebuild-duration"] = 14_400 },
            _ => new(StringComparer.Ordinal) { ["scope-recording-latency"] = 300 },
        };

        return new RecoveryValidationEvidenceManifest
        {
            RunId = RunId,
            ScenarioId = ChatBotCorrelationId.New().Value,
            StartedAtUtc = Now - TimeSpan.FromMinutes(2),
            EndedAtUtc = Now - TimeSpan.FromMinutes(1),
            RepositoryCommit = "1493ff8f2f7e031bc386a2d379d95649744fe7ee",
            AppHostVersion = "chatbot-apphost-v1",
            AspireVersion = "13.4.6",
            DaprVersion = "1.18.4",
            TopologyVersion = "aspire-single-replica-recovery-v1",
            ConfigurationVersion = "live-recovery-v1",
            TenantRef = "replay-test:recovery-validation",
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            ConfiguredDatasetVolume = 6,
            DatasetVolume = jobId == LiveRecoveryValidationJobs.ProjectionRebuild ? 1 : 0,
            DriverMode = RecoveryValidationEvidenceManifest.LiveDriverMode,
            JobId = jobId,
            Scenario = scenario,
            InjectedFaultAction = $"fault:{scenario}",
            RestoreAction = $"restore:{scenario}",
            CleanupAction = $"cleanup:{scenario}",
            ExpectedScope = "tenant",
            ObservedScope = "tenant",
            ReportKind = jobId,
            Verdict = verdict,
            ReasonCode = "validation_completed",
            MeasurementsSeconds = measurements,
            AllowedTargetsSeconds = targets,
            Assertions = assertions,
            Coverage = new Dictionary<string, int> { ["scenario"] = 1 },
            Deviations = [],
            ResidualIds = ["RV-PROD-CONTROL"],
            MeasurableRecoveryCeilingSeconds = 180,
            ArtifactLocators = new Dictionary<string, string>
            {
                ["test-output"] = "artifact:live-recovery-validation-evidence/results.trx",
                ["reports"] = "artifact:live-recovery-validation-evidence/reports",
            },
        };
    }
}
