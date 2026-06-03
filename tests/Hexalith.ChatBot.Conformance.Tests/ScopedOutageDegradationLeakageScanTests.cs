using System.Text.Json;

using Hexalith.ChatBot.Conformance.Tests.Harness;
using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 9.13 (AC2, NFR2/NFR42/NFR59) no-leak floor: the scoped-outage degradation report, the sweep outcome, and the
/// <c>ScopedOutageDegradationBreach</c> breach envelope are metadata-only by construction (safe bounded tokens, the
/// integer latency, booleans, counts). Serializing them and routing the rendered JSON through the shared cross-tenant
/// leakage scanner must surface no foreign-tenant (or any other corpus-class) sentinel. Mirrors
/// <see cref="ProjectionRebuildLeakageScanTests"/>. The validation tenant token is the neutral, non-sentinel
/// <c>tenant-scoped-outage</c> (NOT the Story 1.12 corpus <c>tenant-alpha</c>/<c>tenant-beta</c> sentinels).
/// </summary>
public sealed class ScopedOutageDegradationLeakageScanTests
{
    private const string ScopedOutageTenant = "tenant-scoped-outage";
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public void ScopedOutageDegradationSerializationCarriesNoCrossTenantSentinel()
    {
        DateTimeOffset started = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);
        DateTimeOffset ended = started + TimeSpan.FromMinutes(7);

        // A breached + late-recording report exercises every populated field: deviations, a first-breach locator, the flags.
        ScopedOutageDegradationReport report = new(
            ScopedOutageTenant,
            ScopedOutageDependencies.Graph,
            ScopedOutageScopes.Mailbox,
            ScopedOutageScopes.Tenant,
            started,
            ended,
            ScopeRecordingLatency: TimeSpan.FromMinutes(7),
            ScopeRecordedWithinTarget: false,
            ScopedOutageDegradationVerdicts.Breached,
            Deviations:
            [
                ScopedOutageDegradationEvaluator.CrossTenantLeakageDeviation,
                ScopedOutageDegradationEvaluator.ScopeEscapeDeviation,
                ScopedOutageDegradationEvaluator.ScopeRecordingExceededDeviation,
            ],
            FirstBreachLocator: $"scope:{ScopedOutageScopes.Tenant}|deviation:{ScopedOutageDegradationEvaluator.CrossTenantLeakageDeviation}",
            Correlation,
            ScopedOutageDegradationReport.ValidationCompletedReasonCode);

        ScopedOutageDegradationOutcome outcome = new(ScenariosValidated: 6, Contained: 4, Breached: 1, ScopeRecordingExceeded: 1, Unmeasurable: 1, Alerted: 3);

        AuditEnvelope envelope = AuditEnvelopeFactory.ScopedOutageDegradationBreach(report, Correlation, ended);

        string rendered = JsonSerializer.Serialize(
            new { report, outcome, envelope },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Should.NotThrow(() =>
            CrossTenantLeakageScanner.ScanAll("scoped-outage", "tenant-scoped-outage", rendered));
    }
}
