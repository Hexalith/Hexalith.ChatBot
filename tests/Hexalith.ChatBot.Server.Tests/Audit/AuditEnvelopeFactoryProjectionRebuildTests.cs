using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Redaction;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Direct coverage for the Story 9.12 (AC4, NFR57/NFR49a) <see cref="AuditEnvelopeFactory.ProjectionRebuildValidationFailed"/>
/// pre-commit, metadata-only breach envelope — the validation evidence written before the operator alert. Pins the fixed
/// command/decision/state-transition/outcome tokens, the pre-commit phase + metadata-only redaction stage, the Worker
/// surface origin, the null replay-run id (the breach record is itself production), and the bounded safe ref list:
/// integer-second durations (never raw <see cref="TimeSpan"/>), boolean flags, the resources-compared/schema-version
/// values, one ref per deviation token, and the safe first-diverging locator. The coordinator test only checks
/// phase/command/tenant; this fixture verifies the envelope's full metadata-only contract that the leakage scan otherwise
/// only smoke-serializes. Mirrors <see cref="AuditEnvelopeFactoryContinuityDrillTests"/>.
/// </summary>
public sealed class AuditEnvelopeFactoryProjectionRebuildTests
{
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TestTenant = "replay-test:projection-rebuild";
    private const string SchemaVersion = "chatbot.governed-operation-view.v1";
    private static readonly DateTimeOffset Timestamp = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DivergentOverTargetEnvelopePinsMetadataOnlyTokensAndIntegerSecondDuration()
    {
        // A divergent + over-target validation exercises both deviation refs, the integer-second duration, both flags,
        // the resources-compared/schema-version refs, and the safe first-diverging locator.
        ProjectionRebuildReport report = new(
            TestTenant,
            "baseline-dataset-1",
            Timestamp,
            Timestamp + RecoveryTargets.MaxRto + TimeSpan.FromMinutes(30),
            MeasuredRebuildDuration: RecoveryTargets.MaxRto + TimeSpan.FromMinutes(30),
            DurationWithinTarget: false,
            ProjectionRebuildVerdicts.Divergent,
            ResourcesCompared: 3,
            Deviations:
            [
                ProjectionRebuildEquivalenceEvaluator.DivergedDeviation,
                ProjectionRebuildEquivalenceEvaluator.DurationExceededDeviation,
            ],
            FirstDivergingResourceLocator: "resource:resource-b",
            ProjectionSchemaVersion: SchemaVersion,
            Correlation,
            ProjectionRebuildReport.ValidationCompletedReasonCode);

        AuditEnvelope envelope = AuditEnvelopeFactory.ProjectionRebuildValidationFailed(report, Correlation, Timestamp);

        // Fixed envelope shape (mirrors ContinuityDrillTargetMissed): system actor, pre-commit, metadata-only, Worker.
        envelope.TenantId.ShouldBe(TestTenant);
        envelope.ActorId.ShouldBe("projection-rebuild-validation");
        envelope.ActorType.ShouldBe("system");
        envelope.CommandName.ShouldBe("ProjectionRebuildValidationFailed");
        envelope.ResourceId.ShouldBe("projection-rebuild");
        envelope.Decision.ShouldBe("alert");
        envelope.ReasonCode.ShouldBe(ProjectionRebuildReport.ValidationCompletedReasonCode);
        envelope.CorrelationId.ShouldBe(Correlation);
        envelope.StateTransition.ShouldBe("Rebuilt->ValidationFailed");
        envelope.Outcome.ShouldBe("projection_rebuild_validation_failed");
        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.RedactionDecision.ShouldBe(CoarseUserFacingRedactionStage.MetadataOnlyDecision);
        envelope.SurfaceOrigin.ShouldBe(ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
        envelope.ReplayRunId.ShouldBeNull(); // the system breach record is itself production

        // Bounded safe refs — the duration is integer seconds (4h30m = 16200), flags are bool tokens, never raw TimeSpan.
        envelope.SourceEvidenceRefs.ShouldContain($"correlation:{Correlation}");
        envelope.SourceEvidenceRefs.ShouldContain("admin-operation:projection-rebuild-validation");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-dataset:baseline-dataset-1");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-verdict:divergent");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-reason:projection_rebuild_completed");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-duration-seconds:16200");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-within-target:False");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-resources-compared:3");
        envelope.SourceEvidenceRefs.ShouldContain($"projection-rebuild-schema-version:{SchemaVersion}");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-deviation:projection_diverged");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-deviation:rebuild_duration_exceeded");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-first-diverging:resource:resource-b");

        // Every ref is a single space-free safe token (NFR2/NFR42) — no raw content can hide here.
        envelope.SourceEvidenceRefs.ShouldAllBe(static r => !r.Contains(' ', StringComparison.Ordinal));
        foreach (string banned in new[] { "secret", "password", "bearer", "@", ".txt", ".json" })
        {
            envelope.SourceEvidenceRefs.ShouldAllBe(r => !r.Contains(banned, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void UnmeasurableEnvelopeCarriesIncompleteDeviationZeroSecondDurationAndNoFirstDivergingRef()
    {
        // The fail-safe breach: a validation that could not complete. The duration folds to 0 seconds, the single
        // incomplete deviation surfaces, the unmeasurable reason code rides the envelope (never a fabricated equivalent),
        // and there is no first-diverging ref (the locator is null).
        ProjectionRebuildReport report = ProjectionRebuildReport.Unmeasurable(
            TestTenant,
            "baseline-dataset-1",
            Correlation,
            Timestamp,
            Timestamp,
            SchemaVersion);

        AuditEnvelope envelope = AuditEnvelopeFactory.ProjectionRebuildValidationFailed(report, Correlation, Timestamp);

        envelope.ReasonCode.ShouldBe(ProjectionRebuildReport.ValidationUnmeasurableReasonCode);
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-verdict:unmeasurable");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-reason:projection_rebuild_unmeasurable");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-duration-seconds:0");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-within-target:False");
        envelope.SourceEvidenceRefs.ShouldContain("projection-rebuild-resources-compared:0");
        envelope.SourceEvidenceRefs.ShouldContain($"projection-rebuild-deviation:{ProjectionRebuildReport.IncompleteDeviation}");
        envelope.SourceEvidenceRefs.ShouldNotContain(r => r.StartsWith("projection-rebuild-first-diverging:", StringComparison.Ordinal));
    }
}
