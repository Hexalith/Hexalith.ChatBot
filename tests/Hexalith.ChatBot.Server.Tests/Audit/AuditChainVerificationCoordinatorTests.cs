using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.1 (AC2, NFR49a/NFR15a) coverage for the <see cref="AuditChainVerificationCoordinator"/>: a broken chain
/// writes a pre-commit audit envelope and emits exactly one metadata-only <see cref="OperatorAlertKind.AuditChainBroken"/>
/// alert within the five-minute detection budget; a verified chain emits nothing; an unavailable store fails closed to
/// <c>Unknown</c> (a breach, never silent success); and a fail-closed audit suppresses the alert (no side effect).
/// </summary>
public sealed class AuditChainVerificationCoordinatorTests
{
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public async Task BrokenChainAuditsThenEmitsExactlyOneMetadataOnlyAlertWithinFiveMinuteBudget()
    {
        IReadOnlyList<WormAuditChainRecord> tampered = await WormAuditTestData.BuildTamperedChainAsync("tenant-alpha", 4, tamperAtSequence: 2);
        WormAuditTestData.StubWormAuditStore store = new("tenant-alpha", tampered);
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        WormAuditTestData.FixedClock clock = new(WormAuditTestData.FixedNow);
        AuditChainVerificationCoordinator coordinator = new(store, auditWriter, alertSink, clock);

        DateTimeOffset detectionStart = clock.UtcNow;
        WormAuditChainVerificationResult result = await coordinator.VerifyTenantAndAlertAsync("tenant-alpha", Correlation, CancellationToken.None);

        result.Status.ShouldBe(WormChainVerificationStatus.Broken);

        // Exactly one operator alert, carrying the breach reason, tenant, correlation, and the first-break locator.
        OperatorAlert alert = alertSink.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.AuditChainBroken);
        alert.TenantId.ShouldBe("tenant-alpha");
        alert.CorrelationId.ShouldBe(Correlation);
        alert.ReasonCode.ShouldBe(WormAuditChainVerificationResult.RecordHashMismatchReasonCode);
        alert.FirstBreakLocator.ShouldBe("seq:2");

        // The detection→emit path is synchronous, so the AC2 five-minute budget holds by construction.
        (alert.RaisedAt - detectionStart).ShouldBeLessThanOrEqualTo(WormAuditChainVerifier.DetectionToAlertBudget);
        WormAuditChainVerifier.DetectionToAlertBudget.ShouldBe(TimeSpan.FromMinutes(5));

        // The breach was audited pre-commit before the alert (audit-then-deliver), metadata-only.
        AuditEnvelope envelope = auditWriter.Envelopes.ShouldHaveSingleItem();
        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.CommandName.ShouldBe("AuditChainBroken");
    }

    [Fact]
    public async Task VerifiedChainEmitsNoAlert()
    {
        InMemoryWormAuditStore store = new();
        for (int i = 0; i < 3; i++)
        {
            _ = await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha", resourceId: $"r{i}"), CancellationToken.None);
        }

        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        AuditChainVerificationCoordinator coordinator = new(store, auditWriter, alertSink, new WormAuditTestData.FixedClock(WormAuditTestData.FixedNow));

        WormAuditChainVerificationResult result = await coordinator.VerifyTenantAndAlertAsync("tenant-alpha", Correlation, CancellationToken.None);

        result.Status.ShouldBe(WormChainVerificationStatus.Verified);
        alertSink.Alerts.ShouldBeEmpty();
        auditWriter.Envelopes.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnavailableStoreFailsClosedToUnknownBreachAndAlerts()
    {
        WormAuditTestData.ThrowingWormAuditStore store = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        AuditChainVerificationCoordinator coordinator = new(store, auditWriter, alertSink, new WormAuditTestData.FixedClock(WormAuditTestData.FixedNow));

        WormAuditChainVerificationResult result = await coordinator.VerifyTenantAndAlertAsync("tenant-alpha", Correlation, CancellationToken.None);

        result.Status.ShouldBe(WormChainVerificationStatus.Unknown);
        result.ReasonCode.ShouldBe(WormAuditChainVerificationResult.VerificationIncompleteReasonCode);
        alertSink.Alerts.ShouldHaveSingleItem().Kind.ShouldBe(OperatorAlertKind.AuditChainBroken);
    }

    [Fact]
    public async Task FailClosedAuditSuppressesTheAlert()
    {
        IReadOnlyList<WormAuditChainRecord> tampered = await WormAuditTestData.BuildTamperedChainAsync("tenant-alpha", 3, tamperAtSequence: 1);
        WormAuditTestData.StubWormAuditStore store = new("tenant-alpha", tampered);
        InMemoryOperatorAlertSink alertSink = new();
        AuditChainVerificationCoordinator coordinator = new(store, new WormAuditTestData.UnavailableAuditWriter(), alertSink, new WormAuditTestData.FixedClock(WormAuditTestData.FixedNow));

        WormAuditChainVerificationResult result = await coordinator.VerifyTenantAndAlertAsync("tenant-alpha", Correlation, CancellationToken.None);

        // The breach is still surfaced to the caller (non-silent), but no observable alert side effect occurred.
        result.IsBreach.ShouldBeTrue();
        alertSink.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task VerifyAllTenantsSweepsEveryChainAndCountsBreaches()
    {
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha"), CancellationToken.None);
        _ = await store.AppendAsync(WormAuditTestData.Envelope("tenant-beta"), CancellationToken.None);

        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        AuditChainVerificationCoordinator coordinator = new(store, auditWriter, alertSink, new WormAuditTestData.FixedClock(WormAuditTestData.FixedNow));

        AuditChainVerificationOutcome outcome = await coordinator.VerifyAllTenantsAsync(Correlation, CancellationToken.None);

        outcome.TenantsChecked.ShouldBe(2);
        outcome.Breaches.ShouldBe(0);
        outcome.Alerted.ShouldBe(0);
        alertSink.Alerts.ShouldBeEmpty();
    }
}
