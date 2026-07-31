using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.5 (AC2, FR55a/NFR9a/NFR59) coverage for the synthetic cross-tenant isolation probe: the pure verifier's
/// active-negative-probe semantics (a SUCCESSFUL cross-tenant read = breach), and the coordinator's fail-closed
/// audit-then-deliver discipline (modeled on the Story 9.4 replay probe). A correctly-partitioned store ⇒ <c>Clean</c>,
/// zero breaches, no alert; a deliberately leaky store (one that ignores the tenant scope on read) ⇒ <c>Breach</c> +
/// exactly one alert written audit-then-deliver; a seam that throws on seed/read ⇒ <c>Unknown</c> (never a silent pass);
/// the outcome counts are accurate (the release-gate contract); and the probe leaves only unambiguous probe-artifact
/// sentinels behind.
/// </summary>
public sealed class DerivedStoreIsolationProbeCoordinatorTests
{
    private const string Correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TenantAlpha = "tenant-alpha";
    private const string TenantBeta = "tenant-beta";
    private const string RealResourceId = "real-001";
    private static readonly DateTimeOffset Now = WormAuditTestData.FixedNow;

    // -- Pure verifier --------------------------------------------------------------------------------------------

    [Fact]
    public void VerifierIsCleanWhenTheIntruderObservesNoneOfTheOwnerSentinels()
    {
        DerivedStoreIsolationVerificationResult result = DerivedStoreIsolationVerifier.Verify(
            TenantAlpha,
            TenantBeta,
            ["iso-probe:a", "iso-probe:b"],
            idsObservableThroughIntruderScope: []);

        result.Status.ShouldBe(DerivedStoreIsolationStatus.Clean);
        result.IsBreach.ShouldBeFalse();
        result.FirstOffenderLocator.ShouldBeNull();
    }

    [Fact]
    public void VerifierFlagsAnObservableSentinelAsABreach()
    {
        DerivedStoreIsolationVerificationResult result = DerivedStoreIsolationVerifier.Verify(
            TenantAlpha,
            TenantBeta,
            ["iso-probe:a", "iso-probe:b"],
            idsObservableThroughIntruderScope: ["iso-probe:b"]);

        result.Status.ShouldBe(DerivedStoreIsolationStatus.Breach);
        result.ReasonCode.ShouldBe(DerivedStoreIsolationVerificationResult.BreachReasonCode);
        result.FirstOffenderLocator.ShouldBe("derived-store-sentinel:iso-probe:b");
        result.OwnerTenantRef.ShouldBe(TenantAlpha);
        result.IntruderTenantRef.ShouldBe(TenantBeta);
    }

    [Fact]
    public void VerifierFirstOffenderLocatorIsDeterministicInOwnerSentinelOrder()
    {
        // Two of the owner's sentinels leak; the locator must be the FIRST in owner-sentinel order (not observable
        // order), so the breach locator is stable across runs.
        DerivedStoreIsolationVerificationResult result = DerivedStoreIsolationVerifier.Verify(
            TenantAlpha,
            TenantBeta,
            ["iso-probe:a", "iso-probe:b", "iso-probe:c"],
            idsObservableThroughIntruderScope: ["iso-probe:c", "iso-probe:b"]);

        result.Status.ShouldBe(DerivedStoreIsolationStatus.Breach);
        result.FirstOffenderLocator.ShouldBe("derived-store-sentinel:iso-probe:b");
    }

    [Theory]
    [InlineData("", TenantBeta)]
    [InlineData(TenantAlpha, "")]
    [InlineData("   ", TenantBeta)]
    public void VerifierThrowsOnAMissingOwnerOrIntruderTenant(string owner, string intruder)
        => Should.Throw<ArgumentException>(() => DerivedStoreIsolationVerifier.Verify(owner, intruder, ["iso-probe:a"], []));

    // -- Coordinator ----------------------------------------------------------------------------------------------

    [Fact]
    public async Task CorrectlyPartitionedStoreSweepsWithZeroBreachesAndNoAlert()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        // Two tenants must hold a partition so the sweep has an ordered pair to probe.
        await SeedRealEntryAsync(store, TenantAlpha, token);
        await SeedRealEntryAsync(store, TenantBeta, token);
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        DerivedStoreIsolationProbeCoordinator coordinator = new(store, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        DerivedStoreIsolationProbeOutcome outcome = await coordinator.SweepAllTenantPairsAsync(Correlation, token);

        outcome.PartitionsProbed.ShouldBe(2); // (alpha,beta) and (beta,alpha)
        outcome.Breaches.ShouldBe(0);
        outcome.Alerted.ShouldBe(0);
        alertSink.Alerts.ShouldBeEmpty();
        auditWriter.Envelopes.ShouldBeEmpty();
    }

    [Fact]
    public async Task EmptyStoreSweepReportsAnEmptyPopulationForTheReleaseGateToReject()
    {
        // Zero pairs over an empty population is observable, not a clean cross-tenant proof. The release gate rejects
        // this state; exactly one positively observed tenant is the only structural exemption.
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        DerivedStoreIsolationProbeCoordinator coordinator = new(new InMemoryDerivedStore(), auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        DerivedStoreIsolationProbeOutcome outcome = await coordinator.SweepAllTenantPairsAsync(Correlation, TestContext.Current.CancellationToken);

        outcome.ShouldBe(new DerivedStoreIsolationProbeOutcome(0, 0, 0));
        alertSink.Alerts.ShouldBeEmpty();
        auditWriter.Envelopes.ShouldBeEmpty();
    }

    [Fact]
    public async Task KnownWormTenantsShouldDrivePairCoverageWhenTheDerivedStoreStartsEmpty()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        InMemoryWormAuditStore tenantStore = new();
        _ = await tenantStore.AppendAsync(WormAuditTestData.Envelope(TenantAlpha), token);
        _ = await tenantStore.AppendAsync(WormAuditTestData.Envelope(TenantBeta), token);
        DerivedStoreIsolationProbeCoordinator coordinator = new(
            new InMemoryDerivedStore(),
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            new WormAuditTestData.FixedClock(Now),
            tenantStore);

        DerivedStoreIsolationProbeOutcome outcome = await coordinator.SweepAllTenantPairsAsync(Correlation, token);

        outcome.TenantsEnumerated.ShouldBe(2);
        outcome.PartitionsProbed.ShouldBe(2);
        outcome.Breaches.ShouldBe(0);
    }

    [Fact]
    public async Task SingleTenantStoreSweepProbesNoPairs()
    {
        // One tenant cannot form an ordered (owner, intruder) pair — there is nothing to probe and nothing to alert.
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        await SeedRealEntryAsync(store, TenantAlpha, token);
        DerivedStoreIsolationProbeCoordinator coordinator = new(store, new InMemoryAuditWriter(), new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now));

        DerivedStoreIsolationProbeOutcome outcome = await coordinator.SweepAllTenantPairsAsync(Correlation, token);

        // TenantsEnumerated is 1, not 0: the store was reachable and held a tenant, there was simply no pair to probe.
        // The M2 release gate reads exactly this to separate "nothing to check" (structurally impossible below two
        // tenants, and the normal state on the single-tenant M0 topology) from "checked nothing" (a real anomaly).
        // Without the population, both are zero coverage and the gate must either red-light every single-tenant
        // release or accept vacuous evidence.
        outcome.ShouldBe(new DerivedStoreIsolationProbeOutcome(0, 0, 0, TenantsEnumerated: 1));
    }

    [Fact]
    public async Task CleanPairProbeReturnsCleanWithoutAuditingOrAlerting()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        await SeedRealEntryAsync(store, TenantAlpha, token);
        await SeedRealEntryAsync(store, TenantBeta, token);
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        DerivedStoreIsolationProbeCoordinator coordinator = new(store, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        DerivedStoreIsolationVerificationResult result = await coordinator.ProbePairAndAlertAsync(TenantAlpha, TenantBeta, Correlation, token);

        result.Status.ShouldBe(DerivedStoreIsolationStatus.Clean);
        result.IsBreach.ShouldBeFalse();
        auditWriter.Envelopes.ShouldBeEmpty(); // a clean pair writes no breach envelope and emits no alert
        alertSink.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task LeakyStoreAuditsThenEmitsExactlyOneAlertForABreachedPair()
    {
        LeakyDerivedStore store = new(TenantAlpha, TenantBeta);
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        DerivedStoreIsolationProbeCoordinator coordinator = new(store, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        DerivedStoreIsolationVerificationResult result = await coordinator.ProbePairAndAlertAsync(
            TenantAlpha,
            TenantBeta,
            Correlation,
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(DerivedStoreIsolationStatus.Breach);

        // Audited pre-commit BEFORE the alert (audit-then-deliver), metadata-only.
        AuditEnvelope envelope = auditWriter.Envelopes.ShouldHaveSingleItem();
        envelope.Phase.ShouldBe(AuditCommitPhase.PreCommit);
        envelope.CommandName.ShouldBe("DerivedStoreIsolationBreach");
        envelope.ReplayRunId.ShouldBeNull(); // the system breach record is itself production

        OperatorAlert alert = alertSink.Alerts.ShouldHaveSingleItem();
        alert.Kind.ShouldBe(OperatorAlertKind.DerivedStoreIsolationBreach);
        alert.TenantId.ShouldBe(TenantAlpha); // the owner whose data leaked
        alert.CorrelationId.ShouldBe(Correlation);
        alert.ReasonCode.ShouldBe(DerivedStoreIsolationVerificationResult.BreachReasonCode);
    }

    [Fact]
    public async Task LeakyStoreSweepOutcomeCountsEveryBreachedPair()
    {
        LeakyDerivedStore store = new(TenantAlpha, TenantBeta);
        InMemoryAuditWriter auditWriter = new();
        InMemoryOperatorAlertSink alertSink = new();
        DerivedStoreIsolationProbeCoordinator coordinator = new(store, auditWriter, alertSink, new WormAuditTestData.FixedClock(Now));

        DerivedStoreIsolationProbeOutcome outcome = await coordinator.SweepAllTenantPairsAsync(Correlation, TestContext.Current.CancellationToken);

        outcome.PartitionsProbed.ShouldBe(2);
        outcome.Breaches.ShouldBe(2);
        outcome.Alerted.ShouldBe(2);
    }

    [Fact]
    public async Task SeamThatThrowsFailsClosedToUnknownNoSilentPass()
    {
        DerivedStoreIsolationProbeCoordinator coordinator = new(
            new ThrowingDerivedStore(),
            new InMemoryAuditWriter(),
            new InMemoryOperatorAlertSink(),
            new WormAuditTestData.FixedClock(Now));

        DerivedStoreIsolationVerificationResult result = await coordinator.ProbePairAndAlertAsync(
            TenantAlpha,
            TenantBeta,
            Correlation,
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(DerivedStoreIsolationStatus.Unknown);
        result.IsBreach.ShouldBeTrue();
        result.ReasonCode.ShouldBe(DerivedStoreIsolationVerificationResult.ProbeIncompleteReasonCode);
    }

    [Fact]
    public async Task FailClosedAuditSuppressesTheAlertButStillSurfacesTheBreach()
    {
        LeakyDerivedStore store = new(TenantAlpha, TenantBeta);
        InMemoryOperatorAlertSink alertSink = new();
        DerivedStoreIsolationProbeCoordinator coordinator = new(
            store,
            new WormAuditTestData.UnavailableAuditWriter(),
            alertSink,
            new WormAuditTestData.FixedClock(Now));

        DerivedStoreIsolationVerificationResult result = await coordinator.ProbePairAndAlertAsync(
            TenantAlpha,
            TenantBeta,
            Correlation,
            TestContext.Current.CancellationToken);

        result.IsBreach.ShouldBeTrue();
        alertSink.Alerts.ShouldBeEmpty(); // no observable side effect when the audit fails closed
    }

    [Fact]
    public async Task ProbeLeavesOnlyUnambiguousProbeArtifactSentinelsBehind()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        await SeedRealEntryAsync(store, TenantAlpha, token);
        await SeedRealEntryAsync(store, TenantBeta, token);
        DerivedStoreIsolationProbeCoordinator coordinator = new(store, new InMemoryAuditWriter(), new InMemoryOperatorAlertSink(), new WormAuditTestData.FixedClock(Now));

        _ = await coordinator.SweepAllTenantPairsAsync(Correlation, token);

        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            foreach (string tenant in new[] { TenantAlpha, TenantBeta })
            {
                foreach (string resourceId in store.EnumerateResourceIds(cls, tenant))
                {
                    bool isRealOrProbeArtifact =
                        string.Equals(resourceId, RealResourceId, StringComparison.Ordinal)
                        || resourceId.StartsWith(DerivedStoreIsolationProbeCoordinator.SentinelResourceIdPrefix, StringComparison.Ordinal);
                    isRealOrProbeArtifact.ShouldBeTrue($"unexpected residual resource id '{resourceId}'");
                }
            }
        }
    }

    private static ValueTask SeedRealEntryAsync(InMemoryDerivedStore store, string tenant, CancellationToken token)
        => store.PutAsync(DerivedStoreClass.VectorIndex, tenant, RealResourceId, DerivedStoreEntry.Create(RealResourceId, "real-digest"), token);

    /// <summary>A deliberately broken store that ignores the tenant scope on read — every tenant sees every entry.</summary>
    private sealed class LeakyDerivedStore(params string[] tenants) : IDerivedStore
    {
        private readonly Dictionary<string, DerivedStoreEntry> _flat = new(StringComparer.Ordinal);

        public ValueTask PutAsync(DerivedStoreClass cls, string tenantId, string resourceId, DerivedStoreEntry entry, CancellationToken cancellationToken)
        {
            // Flat keying by class+resource ONLY — the tenant boundary is (wrongly) dropped.
            _flat[$"{DerivedStorePartition.Segment(cls)}:{resourceId}"] = entry;
            return ValueTask.CompletedTask;
        }

        public ValueTask<DerivedStoreEntry?> GetAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
            => ValueTask.FromResult(_flat.GetValueOrDefault($"{DerivedStorePartition.Segment(cls)}:{resourceId}"));

        public IReadOnlyList<string> EnumerateResourceIds(DerivedStoreClass cls, string tenantId) => [];

        public IReadOnlyList<string> EnumerateTenants() => tenants;

        public ValueTask<bool> InvalidateAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
            => ValueTask.FromResult(_flat.Remove($"{DerivedStorePartition.Segment(cls)}:{resourceId}"));
    }

    /// <summary>A store whose seed/read throws, exercising the fail-closed (Unknown) probe path.</summary>
    private sealed class ThrowingDerivedStore : IDerivedStore
    {
        public ValueTask PutAsync(DerivedStoreClass cls, string tenantId, string resourceId, DerivedStoreEntry entry, CancellationToken cancellationToken)
            => throw new InvalidOperationException("derived store down");

        public ValueTask<DerivedStoreEntry?> GetAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("derived store down");

        public IReadOnlyList<string> EnumerateResourceIds(DerivedStoreClass cls, string tenantId) => [];

        public IReadOnlyList<string> EnumerateTenants() => [TenantAlpha, TenantBeta];

        public ValueTask<bool> InvalidateAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
            => throw new InvalidOperationException("derived store down");
    }
}
