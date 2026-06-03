using System.Text.Json;

using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 9.12 (AC3, NFR2/NFR42/NFR59) no-leak floor: the projection-rebuild report, the per-resource digest, the sweep
/// outcome, and the <c>ProjectionRebuildValidationFailed</c> breach envelope are metadata-only by construction (safe
/// bounded tokens, the integer duration, booleans, counts). Serializing them and routing the rendered JSON through the
/// shared cross-tenant leakage scanner must surface no foreign-tenant (or any other corpus-class) sentinel. Mirrors
/// <c>ContinuityDrillLeakageScanTests</c>. The rebuild tenant token is the neutral, non-sentinel
/// <c>tenant-projection-rebuild</c> (NOT the Story 1.12 corpus <c>tenant-alpha</c>/<c>tenant-beta</c> sentinels).
/// </summary>
public sealed class ProjectionRebuildLeakageScanTests
{
    private const string RebuildTenant = "tenant-projection-rebuild";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public void ProjectionRebuildSerializationCarriesNoCrossTenantSentinel()
    {
        DateTimeOffset started = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);
        DateTimeOffset ended = started + RecoveryTargets.MaxRto + TimeSpan.FromMinutes(30);

        // A divergent + over-target report exercises every populated field: deviations, a first-diverging locator, the flags.
        ProjectionRebuildReport report = new(
            RebuildTenant,
            "baseline-dataset-1",
            started,
            ended,
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
            ProjectionSchemaVersion: "chatbot.governed-operation-view.v1",
            Correlation,
            ProjectionRebuildReport.ValidationCompletedReasonCode);

        ProjectionResourceDigest digest = ProjectionResourceDigest.Create("resource-b", "token-b");
        ProjectionRebuildOutcome outcome = new(TenantsValidated: 3, Equivalent: 1, Divergent: 1, DurationExceeded: 1, Unmeasurable: 1, Alerted: 2);

        AuditEnvelope envelope = AuditEnvelopeFactory.ProjectionRebuildValidationFailed(report, Correlation, ended);

        string rendered = JsonSerializer.Serialize(
            new { report, digest, outcome, envelope },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Should.NotThrow(() =>
            CrossTenantLeakageScanner.ScanAll("projection-rebuild", "tenant-projection-rebuild", rendered));
    }
}
