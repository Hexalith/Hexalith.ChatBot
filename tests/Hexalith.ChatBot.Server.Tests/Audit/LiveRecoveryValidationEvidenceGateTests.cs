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
    public void EvidenceFromTwoDifferentSubjectsIsNotOneCoherentAttempt()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        evidence[0] = evidence[0] with { TenantRef = "replay-test:other-validation" };

        Evaluate(complete with { Evidence = evidence }).StopShipReasons.ShouldContain("attempt_evidence_incoherent");
    }

    [Fact]
    public void AManifestWithNullMembersYieldsAReasonCodeRatherThanAnException()
    {
        LiveRecoveryValidationEvidenceAttempt complete = CompleteAttempt();
        RecoveryValidationEvidenceManifest[] evidence = complete.Evidence.ToArray();
        evidence[0] = evidence[0] with { RunId = null!, Assertions = null! };

        Should.NotThrow(() => Evaluate(complete with { Evidence = evidence }))
            .StopShipReasons.ShouldContain($"{LiveRecoveryValidationJobs.Continuity}:invalid_evidence");
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
        Dictionary<string, bool> assertions = new(StringComparer.Ordinal)
        {
            ["cleanup-complete"] = true,
        };
        string verdict;
        Dictionary<string, double> measurements = new(StringComparer.Ordinal);
        if (jobId == LiveRecoveryValidationJobs.Continuity)
        {
            assertions["data-loss-absent"] = true;
            verdict = ContinuityDrillVerdicts.Met;
            measurements["rpo"] = 0;
            measurements["rto"] = 28;
        }
        else if (jobId == LiveRecoveryValidationJobs.ProjectionRebuild)
        {
            assertions["duration-within-target"] = true;
            assertions["structurally-equivalent"] = true;
            verdict = ProjectionRebuildVerdicts.Equivalent;
            measurements["rebuild-duration"] = 0.01;
        }
        else
        {
            assertions["scope-contained"] = true;
            assertions["scope-recorded-within-target"] = true;
            verdict = ScopedOutageDegradationVerdicts.Contained;
            measurements["scope-recording-latency"] = 0.001;
        }

        // The gate owns the targets; a manifest that declares its own is rejected. Mirror the canonical values here so
        // the fixture represents a well-formed run rather than a run that set its own bar.
        Dictionary<string, double> targets = new(
            LiveRecoveryValidationEvidenceGate.CanonicalTargetsFor(jobId),
            StringComparer.Ordinal);

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
            DatasetVolume = 6,
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
                ["test-output"] = "artifact:live-recovery/results.trx",
                ["reports"] = "artifact:live-recovery/reports",
                ["logs"] = "artifact:live-recovery/logs",
                ["traces"] = "artifact:live-recovery/traces",
                ["metrics"] = "artifact:live-recovery/metrics",
                ["state-end-state"] = "artifact:live-recovery/state-end-state",
            },
        };
    }
}
