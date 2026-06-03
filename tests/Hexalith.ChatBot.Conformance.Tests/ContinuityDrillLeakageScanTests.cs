using System.Text.Json;

using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 9.11 (AC2, NFR2/NFR42/NFR59) no-leak floor: the continuity-drill report, the sweep outcome, and the
/// <c>ContinuityDrillTargetMissed</c> breach envelope are metadata-only by construction (safe bounded tokens, integer
/// durations, booleans). Serializing them and routing the rendered JSON through the shared cross-tenant leakage scanner
/// must surface no foreign-tenant (or any other corpus-class) sentinel. Mirrors <c>DeletionErasureLeakageScanTests</c>.
/// The drill tenant token is the neutral, non-sentinel <c>tenant-continuity-drill</c> (NOT the Story 1.12 corpus
/// <c>tenant-alpha</c>/<c>tenant-beta</c> sentinels).
/// </summary>
public sealed class ContinuityDrillLeakageScanTests
{
    private const string DrillTenant = "tenant-continuity-drill";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public void ContinuityDrillSerializationCarriesNoCrossTenantSentinel()
    {
        DateTimeOffset started = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);
        DateTimeOffset ended = started + TimeSpan.FromHours(5);

        // A missed drill exercises every populated field: deviations, a follow-up ref, the recalibration flag.
        ContinuityDrillReport report = new(
            DrillTenant,
            ContinuityDrillScenarios.M365SubscriptionFailure,
            started,
            ended,
            MeasuredRpo: RecoveryTargets.MaxRpo + TimeSpan.FromMinutes(20),
            MeasuredRto: RecoveryTargets.MaxRto + TimeSpan.FromHours(1),
            DataLossDetected: true,
            ContinuityDrillVerdicts.Missed,
            Deviations:
            [
                ContinuityDrillEvaluator.RpoExceededDeviation,
                ContinuityDrillEvaluator.RtoExceededDeviation,
                ContinuityDrillEvaluator.DataLossDeviation,
            ],
            RecalibrationFlag: true,
            FollowUpActionRef: $"continuity-recalibration:{ContinuityDrillScenarios.M365SubscriptionFailure}",
            Correlation,
            ContinuityDrillReport.DrillCompletedReasonCode);

        ContinuityDrillOutcome outcome = new(ScenariosRun: 2, Met: 0, Missed: 1, Unmeasurable: 1, Alerted: 2);

        AuditEnvelope envelope = AuditEnvelopeFactory.ContinuityDrillTargetMissed(report, Correlation, ended);

        string rendered = JsonSerializer.Serialize(
            new { report, outcome, envelope },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Should.NotThrow(() =>
            CrossTenantLeakageScanner.ScanAll("continuity-drill", "tenant-continuity-drill", rendered));
    }
}
