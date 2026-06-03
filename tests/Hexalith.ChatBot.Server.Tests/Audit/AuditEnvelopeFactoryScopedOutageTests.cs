using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Redaction;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Direct coverage for the Story 9.13 (AC4, NFR58/NFR59/NFR41) <see cref="AuditEnvelopeFactory.ScopedOutageDegradationBreach"/>
/// pre-commit, metadata-only breach envelope — the validation evidence written before the operator alert. Pins the fixed
/// command/decision/state-transition/outcome tokens, the pre-commit phase + metadata-only redaction stage, the Worker
/// surface origin, the null replay-run id (the breach record is itself production), and the bounded safe ref list:
/// integer-second scope-recording latency (never raw <see cref="TimeSpan"/>), boolean flags, the dependency/scope/verdict
/// tokens, one ref per deviation token, and the safe first-breach locator. Mirrors
/// <see cref="AuditEnvelopeFactoryProjectionRebuildTests"/>.
/// </summary>
public sealed class AuditEnvelopeFactoryScopedOutageTests
{
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TestTenant = "replay-test:scoped-outage";
    private static readonly DateTimeOffset Timestamp = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BreachedOverTargetEnvelopePinsMetadataOnlyTokensAndIntegerSecondLatency()
    {
        // A breached (cross-tenant leakage + scope escape) + late-recording validation exercises the deviation refs, the
        // integer-second latency, both flags, the dependency/scope/verdict refs, and the safe first-breach locator.
        ScopedOutageDegradationReport report = new(
            TestTenant,
            ScopedOutageDependencies.Graph,
            ScopedOutageScopes.Mailbox,
            ScopedOutageScopes.Tenant,
            Timestamp,
            Timestamp + TimeSpan.FromMinutes(7),
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

        AuditEnvelope envelope = AuditEnvelopeFactory.ScopedOutageDegradationBreach(report, Correlation, Timestamp);

        // Fixed envelope shape (mirrors ProjectionRebuildValidationFailed): system actor, pre-commit, metadata-only, Worker.
        envelope.TenantId.ShouldBe(TestTenant);
        envelope.ActorId.ShouldBe("scoped-outage-validation");
        envelope.ActorType.ShouldBe("system");
        envelope.CommandName.ShouldBe("ScopedOutageDegradationBreach");
        envelope.ResourceId.ShouldBe("scoped-outage");
        envelope.Decision.ShouldBe("alert");
        envelope.ReasonCode.ShouldBe(ScopedOutageDegradationReport.ValidationCompletedReasonCode);
        envelope.CorrelationId.ShouldBe(Correlation);
        envelope.StateTransition.ShouldBe("Degraded->ValidationBreached");
        envelope.Outcome.ShouldBe("scoped_outage_degradation_breach");
        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.RedactionDecision.ShouldBe(CoarseUserFacingRedactionStage.MetadataOnlyDecision);
        envelope.SurfaceOrigin.ShouldBe(ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigin.Worker));
        envelope.ReplayRunId.ShouldBeNull(); // the system breach record is itself production

        // Bounded safe refs — the latency is integer seconds (7m = 420), flags are bool tokens, never raw TimeSpan.
        envelope.SourceEvidenceRefs.ShouldContain($"correlation:{Correlation}");
        envelope.SourceEvidenceRefs.ShouldContain("admin-operation:scoped-outage-validation");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-dependency:graph");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-expected-scope:mailbox");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-observed-scope:tenant");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-verdict:breached");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-reason:scoped_outage_validation_completed");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-recording-seconds:420");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-recording-within-target:False");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-deviation:cross_tenant_leakage");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-deviation:scope_escape");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-deviation:scope_recording_exceeded");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-first-breach:scope:tenant|deviation:cross_tenant_leakage");

        // Every ref is a single space-free safe token (NFR2/NFR42) — no raw content can hide here.
        envelope.SourceEvidenceRefs.ShouldAllBe(static r => !r.Contains(' ', StringComparison.Ordinal));
        foreach (string banned in new[] { "secret", "password", "bearer", "@", ".txt", ".json" })
        {
            envelope.SourceEvidenceRefs.ShouldAllBe(r => !r.Contains(banned, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void UnmeasurableEnvelopeCarriesIncompleteDeviationZeroSecondLatencyAndNoFirstBreachRef()
    {
        // The fail-safe breach: a validation that could not complete. The latency folds to 0 seconds, the single
        // incomplete deviation surfaces, the unmeasurable reason code rides the envelope (never a fabricated contained),
        // and there is no first-breach ref (the locator is null).
        ScopedOutageDegradationReport report = ScopedOutageDegradationReport.Unmeasurable(
            TestTenant,
            ScopedOutageDependencies.Identity,
            Correlation,
            Timestamp,
            Timestamp);

        AuditEnvelope envelope = AuditEnvelopeFactory.ScopedOutageDegradationBreach(report, Correlation, Timestamp);

        envelope.ReasonCode.ShouldBe(ScopedOutageDegradationReport.ValidationUnmeasurableReasonCode);
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-dependency:identity");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-verdict:unmeasurable");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-reason:scoped_outage_validation_unmeasurable");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-recording-seconds:0");
        envelope.SourceEvidenceRefs.ShouldContain("scoped-outage-recording-within-target:False");
        envelope.SourceEvidenceRefs.ShouldContain($"scoped-outage-deviation:{ScopedOutageDegradationReport.IncompleteDeviation}");
        envelope.SourceEvidenceRefs.ShouldNotContain(r => r.StartsWith("scoped-outage-first-breach:", StringComparison.Ordinal));
    }
}
