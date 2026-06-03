using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Redaction;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Direct coverage for the Story 9.11 (AC4, NFR56/A10) <see cref="AuditEnvelopeFactory.ContinuityDrillTargetMissed"/>
/// pre-commit, metadata-only breach envelope — the A10 recalibration evidence written before the operator alert. Pins the
/// fixed command/decision/state-transition/outcome tokens, the pre-commit phase + metadata-only redaction stage, the
/// Worker surface origin, the null replay-run id (the breach record is itself production), and the bounded safe ref list:
/// integer-second durations (never raw <see cref="TimeSpan"/>), boolean flags, one ref per deviation token, and the safe
/// follow-up locator. The coordinator test only checks phase/command/tenant; this fixture verifies the envelope's full
/// metadata-only contract that the leakage scan otherwise only smoke-serializes.
/// </summary>
public sealed class AuditEnvelopeFactoryContinuityDrillTests
{
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string Scenario = ContinuityDrillScenarios.M365SubscriptionFailure;
    private static readonly DateTimeOffset Timestamp = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MissedDrillEnvelopePinsMetadataOnlyTokensAndIntegerSecondDurations()
    {
        // A drill that breached every dimension exercises all deviation refs, both durations, and both boolean flags.
        ContinuityDrillReport report = new(
            "replay-test:continuity-drill",
            Scenario,
            Timestamp,
            Timestamp + TimeSpan.FromHours(5),
            MeasuredRpo: TimeSpan.FromMinutes(20),
            MeasuredRto: TimeSpan.FromHours(5),
            DataLossDetected: true,
            ContinuityDrillVerdicts.Missed,
            Deviations:
            [
                ContinuityDrillEvaluator.RpoExceededDeviation,
                ContinuityDrillEvaluator.RtoExceededDeviation,
                ContinuityDrillEvaluator.DataLossDeviation,
            ],
            RecalibrationFlag: true,
            FollowUpActionRef: $"continuity-recalibration:{Scenario}",
            Correlation,
            ContinuityDrillReport.DrillCompletedReasonCode);

        AuditEnvelope envelope = AuditEnvelopeFactory.ContinuityDrillTargetMissed(report, Correlation, Timestamp);

        // Fixed envelope shape (mirrors DerivedStoreIsolationBreach): system actor, pre-commit, metadata-only, Worker.
        envelope.TenantId.ShouldBe("replay-test:continuity-drill");
        envelope.ActorId.ShouldBe("continuity-drill");
        envelope.ActorType.ShouldBe("system");
        envelope.CommandName.ShouldBe("ContinuityDrillTargetMissed");
        envelope.ResourceId.ShouldBe("continuity-drill");
        envelope.Decision.ShouldBe("alert");
        envelope.ReasonCode.ShouldBe(ContinuityDrillReport.DrillCompletedReasonCode);
        envelope.CorrelationId.ShouldBe(Correlation);
        envelope.StateTransition.ShouldBe("Recovered->TargetMissed");
        envelope.Outcome.ShouldBe("continuity_drill_target_missed");
        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.RedactionDecision.ShouldBe(CoarseUserFacingRedactionStage.MetadataOnlyDecision);
        envelope.SurfaceOrigin.ShouldBe(ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
        envelope.ReplayRunId.ShouldBeNull(); // the system breach record is itself production

        // Bounded safe refs — durations are integer seconds (1200 / 18000), flags are bool tokens, never raw TimeSpan.
        envelope.SourceEvidenceRefs.ShouldContain($"correlation:{Correlation}");
        envelope.SourceEvidenceRefs.ShouldContain("admin-operation:continuity-drill");
        envelope.SourceEvidenceRefs.ShouldContain($"continuity-drill-scenario:{Scenario}");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-verdict:missed");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-reason:continuity_drill_completed");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-rpo-seconds:1200");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-rto-seconds:18000");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-data-loss:True");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-recalibration:True");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-deviation:rpo_exceeded");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-deviation:rto_exceeded");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-deviation:data_loss_detected");
        envelope.SourceEvidenceRefs.ShouldContain($"continuity-drill-follow-up:continuity-recalibration:{Scenario}");

        // Every ref is a single space-free safe token (NFR2/NFR42) — no raw content can hide here.
        envelope.SourceEvidenceRefs.ShouldAllBe(static r => !r.Contains(' ', StringComparison.Ordinal));
        foreach (string banned in new[] { "secret", "password", "bearer", "@", ".txt", ".json" })
        {
            envelope.SourceEvidenceRefs.ShouldAllBe(r => !r.Contains(banned, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void UnmeasurableDrillEnvelopeCarriesIncompleteDeviationAndZeroSecondDurations()
    {
        // The fail-safe breach: a drill that could not complete. Durations fold to 0 seconds, the single incomplete
        // deviation surfaces, and the unmeasurable reason code rides the envelope (never a fabricated met).
        ContinuityDrillReport report = ContinuityDrillReport.Unmeasurable(
            "replay-test:continuity-drill",
            ContinuityDrillScenarios.EventStoreOutage,
            Correlation,
            Timestamp,
            Timestamp);

        AuditEnvelope envelope = AuditEnvelopeFactory.ContinuityDrillTargetMissed(report, Correlation, Timestamp);

        envelope.ReasonCode.ShouldBe(ContinuityDrillReport.DrillUnmeasurableReasonCode);
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-verdict:unmeasurable");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-reason:continuity_drill_unmeasurable");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-rpo-seconds:0");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-rto-seconds:0");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-data-loss:False");
        envelope.SourceEvidenceRefs.ShouldContain("continuity-drill-recalibration:True");
        envelope.SourceEvidenceRefs.ShouldContain($"continuity-drill-deviation:{ContinuityDrillReport.IncompleteDeviation}");
    }
}
