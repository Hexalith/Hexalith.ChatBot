using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.2 (AC2, NFR50a/NFR15a) coverage for the <see cref="AuditCompletenessAlertCoordinator"/>: a below-target
/// tenant writes a pre-commit audit envelope and emits exactly one metadata-only <b>P1</b>
/// <see cref="OperatorAlertKind.AuditCompletenessBudgetBreached"/> alert (audit-then-deliver); a within-budget tenant
/// emits nothing; an unmeasurable tenant fails closed to a breach alert (never silence); and a fail-closed audit
/// suppresses the alert.
/// </summary>
public sealed class AuditCompletenessAlertCoordinatorTests
{
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    private static AuditEnvelope MappedEnvelope(string tenant, string resourceId)
        => WormAuditTestData.Envelope(tenant, commandName: "CreateOutboundDraft", resourceId: resourceId);

    private static GovernedOperationView Projection(string tenant, string noteId)
        => new(
            tenant,
            noteId,
            GovernedOperationView.CurrentSchemaVersion,
            GovernedOperationView.GovernedCommandProvenance,
            GovernedOperationView.CurrentDerivationKernelVersion,
            GovernedOperationView.MetadataOnlyRedactionState,
            GovernedOperationView.GovernedOperationalRetentionClass,
            SourceVersion: 1,
            RecordedAt: WormAuditTestData.FixedNow,
            LastUpdatedAt: WormAuditTestData.FixedNow);

    private static AuditCompletenessAlertCoordinator Coordinator(
        IWormAuditStore store,
        IGovernedOperationProjectionStore projection,
        IAuditWriter auditWriter,
        IOperatorAlertSink alertSink)
    {
        WormAuditTestData.FixedClock clock = new(WormAuditTestData.FixedNow);
        AuditCompletenessMeasurer measurer = new(store, projection, clock);
        return new AuditCompletenessAlertCoordinator(store, measurer, auditWriter, alertSink, clock);
    }

    [Fact]
    public async Task BelowTargetAuditsThenEmitsExactlyOneP1Alert()
    {
        // One mapped operation with NO projection → fraction 0.0 < 99.5% → Exhausted (P1 breach).
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();

        AuditCompletenessMeasurement result = await Coordinator(store, new InMemoryGovernedOperationProjectionStore(), auditWriter, alertSink)
            .MeasureTenantAndAlertAsync("tenant-alpha", Correlation, TestContext.Current.CancellationToken);

        result.IsMeasurable.ShouldBeTrue();
        result.Fraction.ShouldBe(0.0);

        OperatorAlert alert = alertSink.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.AuditCompletenessBudgetBreached);
        alert.TenantId.ShouldBe("tenant-alpha");
        alert.CorrelationId.ShouldBe(Correlation);

        // Audit-then-deliver, pre-commit, and the envelope carries explicit P1 severity.
        AuditEnvelope envelope = auditWriter.Envelopes.ShouldHaveSingleItem();
        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.CommandName.ShouldBe("AuditCompletenessBudgetBreached");
        envelope.SourceEvidenceRefs.ShouldContain("audit-completeness-severity:p1");
    }

    [Fact]
    public async Task WithinBudgetEmitsNoAlert()
    {
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        InMemoryGovernedOperationProjectionStore projection = new();
        await projection.SaveAsync(Projection("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();

        AuditCompletenessMeasurement result = await Coordinator(store, projection, auditWriter, alertSink)
            .MeasureTenantAndAlertAsync("tenant-alpha", Correlation, TestContext.Current.CancellationToken);

        result.Fraction.ShouldBe(1.0);
        alertSink.Alerts.ShouldBeEmpty();
        auditWriter.Envelopes.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnmeasurableTenantFailsClosedToBreachAndAlerts()
    {
        WormAuditTestData.ThrowingWormAuditStore store = new();
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();

        AuditCompletenessMeasurement result = await Coordinator(store, new InMemoryGovernedOperationProjectionStore(), auditWriter, alertSink)
            .MeasureTenantAndAlertAsync("tenant-alpha", Correlation, TestContext.Current.CancellationToken);

        result.IsMeasurable.ShouldBeFalse();
        alertSink.Alerts.ShouldHaveSingleItem().Kind.ShouldBe(OperatorAlertKind.AuditCompletenessBudgetBreached);
        auditWriter.Envelopes.ShouldHaveSingleItem()
            .SourceEvidenceRefs.ShouldContain("audit-completeness-status:unmeasurable");
    }

    [Fact]
    public async Task FailClosedAuditSuppressesTheAlert()
    {
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        InMemoryOperatorAlertSink alertSink = new();

        AuditCompletenessMeasurement result = await Coordinator(store, new InMemoryGovernedOperationProjectionStore(), new WormAuditTestData.UnavailableAuditWriter(), alertSink)
            .MeasureTenantAndAlertAsync("tenant-alpha", Correlation, TestContext.Current.CancellationToken);

        // The breach is still surfaced to the caller (non-silent), but no observable alert side effect occurred.
        result.IsBreach.ShouldBeFalse(); // measurable, but below budget
        result.Fraction.ShouldBe(0.0);
        alertSink.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task SweepMeasuresEveryTenantAndCountsBreaches()
    {
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        _ = await store.AppendAsync(MappedEnvelope("tenant-beta", "note-2"), TestContext.Current.CancellationToken);
        InMemoryGovernedOperationProjectionStore projection = new();
        await projection.SaveAsync(Projection("tenant-alpha", "note-1"), TestContext.Current.CancellationToken); // alpha within budget; beta diverges
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();

        AuditCompletenessSweepOutcome outcome = await Coordinator(store, projection, auditWriter, alertSink)
            .MeasureAllTenantsAndAlertAsync(Correlation, TestContext.Current.CancellationToken);

        outcome.TenantsMeasured.ShouldBe(2);
        outcome.Breaches.ShouldBe(1);   // only beta breaches
        outcome.Unmeasurable.ShouldBe(0);
        alertSink.Alerts.ShouldHaveSingleItem().TenantId.ShouldBe("tenant-beta");
    }

    [Fact]
    public async Task SweepCountsAnUnmeasurableTenantAsBothABreachAndUnmeasurableAndStillAlerts()
    {
        // Fail-closed sweep (Epic 8 no-fabrication): one within-budget tenant and one whose chain cannot be read. The
        // unmeasurable tenant is counted in BOTH the breach tally and the unmeasurable tally, and it still pages — never
        // a silent skip.
        InMemoryWormAuditStore alphaStore = new();
        _ = await alphaStore.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        SelectiveThrowingWormAuditStore store = new(alphaStore.EnumerateChain("tenant-alpha"));
        InMemoryGovernedOperationProjectionStore projection = new();
        await projection.SaveAsync(Projection("tenant-alpha", "note-1"), TestContext.Current.CancellationToken); // alpha within budget
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();

        AuditCompletenessSweepOutcome outcome = await Coordinator(store, projection, auditWriter, alertSink)
            .MeasureAllTenantsAndAlertAsync(Correlation, TestContext.Current.CancellationToken);

        outcome.TenantsMeasured.ShouldBe(2);
        outcome.Breaches.ShouldBe(1);       // only the unmeasurable tenant breaches
        outcome.Unmeasurable.ShouldBe(1);   // and it is also counted as unmeasurable
        OperatorAlert alert = alertSink.Alerts.ShouldHaveSingleItem();
        alert.TenantId.ShouldBe("tenant-beta");
        alert.Kind.ShouldBe(OperatorAlertKind.AuditCompletenessBudgetBreached);
    }

    // Serves a preset chain for tenant-alpha and throws for tenant-beta, so a sweep sees one measurable and one
    // unmeasurable tenant.
    private sealed class SelectiveThrowingWormAuditStore(IReadOnlyList<WormAuditChainRecord> alphaChain) : IWormAuditStore
    {
        public ValueTask<WormAuditAppendOutcome> AppendAsync(AuditEnvelope envelope, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IReadOnlyList<WormAuditChainRecord> EnumerateChain(string tenantId)
            => string.Equals(tenantId, "tenant-alpha", StringComparison.Ordinal)
                ? alphaChain
                : throw new InvalidOperationException("tenant-beta store down");

        public IReadOnlyList<string> EnumerateTenants() => ["tenant-alpha", "tenant-beta"];
    }
}
