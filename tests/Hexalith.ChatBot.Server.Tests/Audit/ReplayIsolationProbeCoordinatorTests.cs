using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.4 (AC3, FR95a) coverage for the nightly replay-isolation probe: the pure verifier's two complementary
/// assertions, and the coordinator's fail-closed audit-then-deliver discipline (modeled on the Story 9.1 chain
/// verifier). A clean store set is <c>Clean</c> with no alert; a production-tenant trace record carrying a replay run id
/// is a <c>Breach</c> with exactly one alert written audit-then-deliver; a production-tenant chain replay envelope is a
/// <c>Breach</c> (defense in depth); an enumeration that throws is <c>Unknown</c> (a breach, never a silent pass); the
/// sweep skips test tenants; and the outcome counts are accurate (the release-gate contract).
/// </summary>
public sealed class ReplayIsolationProbeCoordinatorTests
{
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string ProductionTenant = "tenant-alpha";
    private const string TestTenant = "replay-test:tenant-alpha";
    private static readonly DateTimeOffset Now = WormAuditTestData.FixedNow;

    // -- Pure verifier --------------------------------------------------------------------------------------------

    [Fact]
    public void VerifierIsCleanWhenNoProductionRecordCarriesAReplayMarker()
    {
        ReplayIsolationVerificationResult result = ReplayIsolationVerifier.Verify(
            ProductionTenant,
            [TraceRecord(ProductionTenant, "send-001", replayRunId: null)],
            [WormAuditTestData.Envelope(ProductionTenant)]);

        result.Status.ShouldBe(ReplayIsolationStatus.Clean);
        result.IsBreach.ShouldBeFalse();
        result.FirstOffenderLocator.ShouldBeNull();
    }

    [Fact]
    public void VerifierFlagsATraceRecordReplayMarkerAsABreach()
    {
        ReplayIsolationVerificationResult result = ReplayIsolationVerifier.Verify(
            ProductionTenant,
            [TraceRecord(ProductionTenant, "send-007", replayRunId: "replay-run-001")],
            []);

        result.Status.ShouldBe(ReplayIsolationStatus.Breach);
        result.ReasonCode.ShouldBe(ReplayIsolationVerificationResult.TraceBreachReasonCode);
        result.FirstOffenderLocator.ShouldBe("trace-send:send-007");
    }

    [Fact]
    public void VerifierFlagsAChainReplayEnvelopeAsABreachDefenseInDepth()
    {
        ReplayIsolationVerificationResult result = ReplayIsolationVerifier.Verify(
            ProductionTenant,
            [],
            [WormAuditTestData.Envelope(ProductionTenant) with { ReplayRunId = "replay-run-001" }]);

        result.Status.ShouldBe(ReplayIsolationStatus.Breach);
        result.ReasonCode.ShouldBe(ReplayIsolationVerificationResult.ChainBreachReasonCode);
        result.FirstOffenderLocator.ShouldBe("chain-seq:0");
    }

    [Fact]
    public void VerifierPrefersTheTraceLocatorWhenBothInvariantsAreViolated()
    {
        // The verifier checks the outbound-trace store first (the primary AC3 assertion), so when BOTH a trace record
        // AND a chain envelope are replay-marked the trace breach + its locator win — pinning the documented ordering so
        // the operator alert points at the primary evidence, not the defense-in-depth chain hit.
        ReplayIsolationVerificationResult result = ReplayIsolationVerifier.Verify(
            ProductionTenant,
            [TraceRecord(ProductionTenant, "send-007", replayRunId: "replay-run-001")],
            [WormAuditTestData.Envelope(ProductionTenant) with { ReplayRunId = "replay-run-001" }]);

        result.Status.ShouldBe(ReplayIsolationStatus.Breach);
        result.ReasonCode.ShouldBe(ReplayIsolationVerificationResult.TraceBreachReasonCode);
        result.FirstOffenderLocator.ShouldBe("trace-send:send-007");
    }

    // -- Coordinator ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task CleanStoreSetSweepsWithZeroBreachesAndNoAlert()
    {
        InMemoryOutboundTraceStore traceStore = new();
        await traceStore.RecordAsync(TraceRecord(ProductionTenant, "send-001", replayRunId: null), CancellationToken.None);
        InMemoryWormAuditStore wormStore = new();
        _ = await wormStore.AppendAsync(WormAuditTestData.Envelope(ProductionTenant), CancellationToken.None);
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ReplayIsolationProbeCoordinator coordinator = new(traceStore, wormStore, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ReplayIsolationProbeOutcome outcome = await coordinator.SweepAllProductionTenantsAsync(Correlation, CancellationToken.None);

        outcome.TenantsSwept.ShouldBe(1);
        outcome.Breaches.ShouldBe(0);
        outcome.Alerted.ShouldBe(0);
        alertSink.Alerts.ShouldBeEmpty();
        auditWriter.Envelopes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProductionTraceReplayRecordAuditsThenEmitsExactlyOneAlert()
    {
        InMemoryOutboundTraceStore traceStore = new();
        await traceStore.RecordAsync(TraceRecord(ProductionTenant, "send-007", replayRunId: "replay-run-001"), CancellationToken.None);
        InMemoryWormAuditStore wormStore = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ReplayIsolationProbeCoordinator coordinator = new(traceStore, wormStore, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ReplayIsolationVerificationResult result = await coordinator.VerifyTenantAndAlertAsync(ProductionTenant, Correlation, CancellationToken.None);

        result.Status.ShouldBe(ReplayIsolationStatus.Breach);

        // Audited pre-commit BEFORE the alert (audit-then-deliver), metadata-only.
        AuditEnvelope envelope = auditWriter.Envelopes.ShouldHaveSingleItem();
        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.CommandName.ShouldBe("ReplayIsolationBreach");
        envelope.ReplayRunId.ShouldBeNull(); // the system breach record is itself production

        OperatorAlert alert = alertSink.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.ReplayIsolationBreach);
        alert.TenantId.ShouldBe(ProductionTenant);
        alert.CorrelationId.ShouldBe(Correlation);
        alert.ReasonCode.ShouldBe(ReplayIsolationVerificationResult.TraceBreachReasonCode);
        alert.FirstBreakLocator.ShouldBe("trace-send:send-007");
    }

    [Fact]
    public async Task ProductionChainReplayEnvelopeIsABreachDefenseInDepth()
    {
        InMemoryOutboundTraceStore traceStore = new();
        InMemoryWormAuditStore wormStore = new();
        _ = await wormStore.AppendAsync(WormAuditTestData.Envelope(ProductionTenant) with { ReplayRunId = "replay-run-001" }, CancellationToken.None);
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ReplayIsolationProbeCoordinator coordinator = new(traceStore, wormStore, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ReplayIsolationVerificationResult result = await coordinator.VerifyTenantAndAlertAsync(ProductionTenant, Correlation, CancellationToken.None);

        result.Status.ShouldBe(ReplayIsolationStatus.Breach);
        result.ReasonCode.ShouldBe(ReplayIsolationVerificationResult.ChainBreachReasonCode);
        alertSink.Alerts.ShouldHaveSingleItem().Kind.ShouldBe(OperatorAlertKind.ReplayIsolationBreach);
    }

    [Fact]
    public async Task EnumerationThatThrowsFailsClosedToUnknownNoSilentPass()
    {
        ReplayIsolationProbeCoordinator coordinator = new(
            new ThrowingOutboundTraceStore(),
            new WormAuditTestData.ThrowingWormAuditStore(),
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            new WormAuditTestData.FixedClock(Now));

        ReplayIsolationVerificationResult result = await coordinator.VerifyTenantAndAlertAsync(ProductionTenant, Correlation, CancellationToken.None);

        result.Status.ShouldBe(ReplayIsolationStatus.Unknown);
        result.IsBreach.ShouldBeTrue();
        result.ReasonCode.ShouldBe(ReplayIsolationVerificationResult.SweepIncompleteReasonCode);
    }

    [Fact]
    public async Task SweepSkipsTestTenantsAndOnlyCountsProductionBreaches()
    {
        InMemoryOutboundTraceStore traceStore = new();
        // A test-tenant replay record is EXPECTED, never a breach — the sweep must skip it.
        await traceStore.RecordAsync(TraceRecord(TestTenant, "send-001", replayRunId: "replay-run-001"), CancellationToken.None);
        // A clean production tenant.
        await traceStore.RecordAsync(TraceRecord(ProductionTenant, "send-002", replayRunId: null), CancellationToken.None);
        InMemoryWormAuditStore wormStore = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ReplayIsolationProbeCoordinator coordinator = new(traceStore, wormStore, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ReplayIsolationProbeOutcome outcome = await coordinator.SweepAllProductionTenantsAsync(Correlation, CancellationToken.None);

        outcome.TenantsSwept.ShouldBe(1); // only the production tenant
        outcome.Breaches.ShouldBe(0);
        outcome.Alerted.ShouldBe(0);
        alertSink.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task SweepOutcomeCountsAreAccurateAcrossCleanAndBreachedProductionTenants()
    {
        InMemoryOutboundTraceStore traceStore = new();
        await traceStore.RecordAsync(TraceRecord("tenant-clean", "send-001", replayRunId: null), CancellationToken.None);
        await traceStore.RecordAsync(TraceRecord("tenant-breached", "send-002", replayRunId: "replay-run-001"), CancellationToken.None);
        InMemoryWormAuditStore wormStore = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        ReplayIsolationProbeCoordinator coordinator = new(traceStore, wormStore, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        ReplayIsolationProbeOutcome outcome = await coordinator.SweepAllProductionTenantsAsync(Correlation, CancellationToken.None);

        outcome.TenantsSwept.ShouldBe(2);
        outcome.Breaches.ShouldBe(1);
        outcome.Alerted.ShouldBe(1);
    }

    [Fact]
    public async Task FailClosedAuditSuppressesTheAlertButStillSurfacesTheBreach()
    {
        InMemoryOutboundTraceStore traceStore = new();
        await traceStore.RecordAsync(TraceRecord(ProductionTenant, "send-007", replayRunId: "replay-run-001"), CancellationToken.None);
        InMemoryOperatorAlertSink alertSink = new();
        ReplayIsolationProbeCoordinator coordinator = new(
            traceStore,
            new InMemoryWormAuditStore(),
            new WormAuditTestData.UnavailableAuditWriter(),
            alertSink,
            new WormAuditTestData.FixedClock(Now));

        ReplayIsolationVerificationResult result = await coordinator.VerifyTenantAndAlertAsync(ProductionTenant, Correlation, CancellationToken.None);

        result.IsBreach.ShouldBeTrue();
        alertSink.Alerts.ShouldBeEmpty(); // no observable side effect when the audit fails closed
    }

    private static OutboundTraceRecord TraceRecord(string tenantId, string sendId, string? replayRunId)
        => new(
            tenantId,
            "project-001",
            "draft-001",
            "approval-001",
            sendId,
            "requester-001",
            "actor-alpha",
            "AuthenticatedUserSend",
            "send",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            replayRunId,
            Now);

    private sealed class ThrowingOutboundTraceStore : IOutboundTraceStore
    {
        public ValueTask RecordAsync(OutboundTraceRecord record, CancellationToken cancellationToken)
            => throw new InvalidOperationException("trace store down");

        public IReadOnlyList<OutboundTraceRecord> EnumerateForTenant(string tenantId)
            => throw new InvalidOperationException("trace store down");

        public IReadOnlyList<string> EnumerateTenants() => [ProductionTenant];
    }
}
