using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>Story 12.15 Task 2 metadata-only evidence-manifest validation tests.</summary>
public sealed class RecoveryValidationEvidenceManifestTests
{
    private static readonly DateTimeOffset Started = new(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CompleteMetadataOnlyManifestIsValid()
    {
        RecoveryValidationEvidenceManifest manifest = ValidManifest();

        manifest.Validate().ShouldBeEmpty();
    }

    [Fact]
    public void ManifestRejectsInvalidUlidZeroCoverageAndSensitiveMetadata()
    {
        (ValidManifest() with { RunId = "not-a-ulid" }).Validate().ShouldContain(error => error.Contains(nameof(RecoveryValidationEvidenceManifest.RunId), StringComparison.Ordinal));
        (ValidManifest() with { Coverage = new Dictionary<string, int> { ["scenario"] = -1 } }).Validate().ShouldContain(error => error.Contains("coverage", StringComparison.OrdinalIgnoreCase));

        // Zero coverage stays RECORDABLE so an unmeasurable report can be persisted at all; the release gate is what
        // rejects it (see LiveRecoveryValidationEvidenceGateTests zero_coverage coverage).
        (ValidManifest() with { Coverage = new Dictionary<string, int> { ["scenario"] = 0 } }).Validate().ShouldBeEmpty();
        (ValidManifest() with { Deviations = ["password=do-not-retain"] }).Validate().ShouldContain(error => error.Contains("metadata", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ManifestRejectsNonUtcOrReversedBoundsAndMissingRawArtifactLinks()
    {
        (ValidManifest() with { StartedAtUtc = Started.ToOffset(TimeSpan.FromHours(2)) }).Validate().ShouldNotBeEmpty();
        (ValidManifest() with { EndedAtUtc = Started - TimeSpan.FromSeconds(1) }).Validate().ShouldNotBeEmpty();
        (ValidManifest() with { ArtifactLocators = new Dictionary<string, string>() }).Validate().ShouldContain(error => error.Contains("artifact", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The ceiling bounds what a target claim can mean: a recovery slower than it converts to unmeasurable and can
    /// never be reported as a target miss. An unset or non-positive ceiling would let a manifest present a "met"
    /// verdict against a 4-hour target with nothing recording that the lane could not have observed a 4-hour recovery.
    /// </summary>
    [Fact]
    public void ManifestRequiresAPositiveMeasurableRecoveryCeiling()
    {
        (ValidManifest() with { MeasurableRecoveryCeilingSeconds = 0 })
            .Validate()
            .ShouldContain(error => error.Contains("MeasurableRecoveryCeilingSeconds", StringComparison.Ordinal));
        (ValidManifest() with { MeasurableRecoveryCeilingSeconds = -1 })
            .Validate()
            .ShouldContain(error => error.Contains("MeasurableRecoveryCeilingSeconds", StringComparison.Ordinal));
        (ValidManifest() with { MeasurableRecoveryCeilingSeconds = 180 }).Validate().ShouldBeEmpty();

        // The non-finite cases are the actual defect this check was added for: `NaN <= 0` is false, so a NaN or
        // infinite ceiling passed a bounds test written only against zero and negatives. Covering 0/-1/180 alone left
        // the finite check free to be deleted with every assertion still green.
        foreach (double nonFinite in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            (ValidManifest() with { MeasurableRecoveryCeilingSeconds = nonFinite })
                .Validate()
                .ShouldContain(
                    error => error.Contains("MeasurableRecoveryCeilingSeconds", StringComparison.Ordinal),
                    $"A ceiling of {nonFinite} must be rejected.");
        }
    }

    [Fact]
    public void ControlledLossManifestRequiresCompleteOrderedUtcDistinctDurableBounds()
    {
        ControlledManifest().Validate().ShouldBeEmpty();
        (ControlledManifest() with { PreFaultCommittedAtUtc = null }).Validate().ShouldNotBeEmpty();
        (ControlledManifest() with { PostRecoverySequence = 0 }).Validate().ShouldNotBeEmpty();
        (ControlledManifest() with { RejectedCandidateRef = "not-a-ulid" }).Validate().ShouldNotBeEmpty();
        (ControlledManifest() with
        {
            PostRecoveryCommittedAtUtc = Started + TimeSpan.FromSeconds(9),
            RejectedAtUtc = Started + TimeSpan.FromSeconds(20),
        }).Validate().ShouldNotBeEmpty();
        (ControlledManifest() with
        {
            PostRecoveryEventRef = "01ARZ3NDEKTSV4RRFFQ69G5FAA",
        }).Validate().ShouldNotBeEmpty();
        (ControlledManifest() with
        {
            RejectedAtUtc = Started - TimeSpan.FromDays(1),
        }).Validate().ShouldBeEmpty("Sandbox and EventStore timestamps use independent authoritative clocks.");
    }

    [Fact]
    public void ControlledLossManifestRequiresVerdictReasonConsistency()
    {
        (ControlledManifest() with
        {
            Verdict = ControlledLossPathVerdicts.Missed,
            ReasonCode = ControlledLossPathReport.CompletedReasonCode,
        }).Validate().ShouldContain(error => error.Contains("reason code", StringComparison.Ordinal));

        (ControlledManifest() with
        {
            Verdict = ControlledLossPathVerdicts.Missed,
            ReasonCode = ControlledLossPathReport.TargetMissedReasonCode,
        }).Validate().ShouldBeEmpty();
    }

    [Fact]
    public void OrdinaryManifestCannotSmuggleControlledLossBounds()
    {
        (ValidManifest() with { RejectedCandidateRef = "01ARZ3NDEKTSV4RRFFQ69G5FAA" })
            .Validate()
            .ShouldContain(error => error.Contains("not allowed", StringComparison.Ordinal));
    }

    private static RecoveryValidationEvidenceManifest ValidManifest()
        => new()
        {
            RunId = "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ScenarioId = "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            StartedAtUtc = Started,
            EndedAtUtc = Started + TimeSpan.FromMinutes(1),
            RepositoryCommit = "1493ff8f2f7e031bc386a2d379d95649744fe7ee",
            AppHostVersion = "chatbot-apphost-v1",
            AspireVersion = "13.4.6",
            DaprVersion = "1.18.4",
            TopologyVersion = "single-replica-v1",
            ConfigurationVersion = "live-recovery-v1",
            TenantRef = "replay-test:recovery-validation",
            DatasetRef = "recovery-baseline",
            DatasetVersion = "v1",
            ConfiguredDatasetVolume = 6,
            DatasetVolume = 0,
            DriverMode = "aspire-tier3-live",
            JobId = LiveRecoveryValidationJobs.Continuity,
            Scenario = ContinuityDrillScenarios.EventStoreOutage,
            InjectedFaultAction = "stop:eventstore",
            RestoreAction = "start:eventstore",
            CleanupAction = "delete:recovery-partition-v1",
            ExpectedScope = "tenant",
            ObservedScope = "tenant",
            ReportKind = "continuity",
            Verdict = ContinuityDrillVerdicts.Met,
            ReasonCode = ContinuityDrillReport.DrillCompletedReasonCode,
            MeasurementsSeconds = new Dictionary<string, double> { ["rpo"] = 1, ["rto"] = 2 },
            AllowedTargetsSeconds = new Dictionary<string, double> { ["rpo"] = 900, ["rto"] = 14_400 },
            Assertions = new Dictionary<string, bool> { ["data-loss-absent"] = true },
            Coverage = new Dictionary<string, int> { ["committed-operations"] = 1 },
            Deviations = [],
            ResidualIds = ["RV-PROD-CONTROL"],
            MeasurableRecoveryCeilingSeconds = 180,
            ArtifactLocators = new Dictionary<string, string>
            {
                ["test-output"] = "artifact:live-recovery-validation-evidence/results.trx",
                ["reports"] = "artifact:live-recovery-validation-evidence/reports",
            },
        };

    private static RecoveryValidationEvidenceManifest ControlledManifest()
        => ValidManifest() with
        {
            JobId = LiveRecoveryValidationJobs.ControlledLossPath,
            ReportKind = LiveRecoveryValidationJobs.ControlledLossPath,
            Scenario = ControlledLossPathReport.SubscriptionNotificationRejectionScenario,
            Verdict = ControlledLossPathVerdicts.Met,
            ReasonCode = ControlledLossPathReport.CompletedReasonCode,
            MeasurementsSeconds = new Dictionary<string, double> { ["rpo"] = 20 },
            AllowedTargetsSeconds = new Dictionary<string, double> { ["rpo"] = 900 },
            PreFaultRetainedRef = "01ARZ3NDEKTSV4RRFFQ69G5FAA",
            PreFaultEventRef = "01ARZ3NDEKTSV4RRFFQ69G5FAB",
            PreFaultSequence = 1,
            PreFaultCommittedAtUtc = Started + TimeSpan.FromSeconds(10),
            RejectedCandidateRef = "01ARZ3NDEKTSV4RRFFQ69G5FAC",
            RejectedAtUtc = Started + TimeSpan.FromSeconds(20),
            PostRecoveryRetainedRef = "01ARZ3NDEKTSV4RRFFQ69G5FAD",
            PostRecoveryEventRef = "01ARZ3NDEKTSV4RRFFQ69G5FAE",
            PostRecoverySequence = 1,
            PostRecoveryCommittedAtUtc = Started + TimeSpan.FromSeconds(30),
        };
}
