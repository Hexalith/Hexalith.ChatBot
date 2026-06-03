using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.2 (AC2/AC3, NFR50a/NFR9a) coverage for the scheduled production assertion: per-tenant rolling-7-day fraction
/// computed by rebuilding state from the WORM log and diffing the live projection. Reconstructability is STRONGER than
/// field presence (an all-fields-present operation whose rebuilt state has no matching projection is not
/// reconstructable); unmeasurable runs fail closed to a breach (never a fabricated 1.0); replay events are excluded
/// from both numerator and denominator; the first-diverging locator is a safe token; and measurement is per-tenant
/// isolated. Read-only/out-of-band (D4): the measurement appends nothing and adds no commit-path gate.
/// </summary>
public sealed class AuditCompletenessMeasurerTests
{
    private static AuditEnvelope MappedEnvelope(string tenant, string resourceId)
        => WormAuditTestData.Envelope(tenant, commandName: "CreateOutboundDraft", resourceId: resourceId);

    private static GovernedOperationView Projection(string tenant, string noteId, string redactionState = GovernedOperationView.MetadataOnlyRedactionState)
        => new(
            tenant,
            noteId,
            GovernedOperationView.CurrentSchemaVersion,
            GovernedOperationView.GovernedCommandProvenance,
            GovernedOperationView.CurrentDerivationKernelVersion,
            redactionState,
            GovernedOperationView.GovernedOperationalRetentionClass,
            SourceVersion: 1,
            RecordedAt: WormAuditTestData.FixedNow,
            LastUpdatedAt: WormAuditTestData.FixedNow);

    private static AuditCompletenessMeasurer Measurer(IWormAuditStore store, IGovernedOperationProjectionStore projection)
        => new(store, projection, new WormAuditTestData.FixedClock(WormAuditTestData.FixedNow));

    [Fact]
    public async Task ReconstructableOperationWhoseProjectionAgreesCountsTowardCompleteness()
    {
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        InMemoryGovernedOperationProjectionStore projection = new();
        await projection.SaveAsync(Projection("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);

        AuditCompletenessMeasurement result = await Measurer(store, projection).MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        result.IsMeasurable.ShouldBeTrue();
        result.TotalCount.ShouldBe(1);
        result.ReconstructableCount.ShouldBe(1);
        result.Fraction.ShouldBe(1.0);
        result.FirstDivergingOperationLocator.ShouldBeNull();
    }

    [Fact]
    public async Task AllFieldsPresentButNoMatchingProjectionIsNotReconstructable()
    {
        // The defining distinction: field presence is necessary but NOT sufficient. The chain envelope carries every
        // required field, yet with no live projection to diff against the operation is NOT reconstructable.
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        InMemoryGovernedOperationProjectionStore projection = new(); // projection deliberately absent

        AuditCompletenessMeasurement result = await Measurer(store, projection).MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        result.IsMeasurable.ShouldBeTrue();
        result.TotalCount.ShouldBe(1);
        result.ReconstructableCount.ShouldBe(0);
        result.Fraction.ShouldBe(0.0);
        result.FirstDivergingOperationLocator.ShouldBe("op:note-1");
    }

    [Fact]
    public async Task ProjectionStructuralMismatchDivergesAndIsNotReconstructable()
    {
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        InMemoryGovernedOperationProjectionStore projection = new();
        await projection.SaveAsync(Projection("tenant-alpha", "note-1", redactionState: "some_other_state"), TestContext.Current.CancellationToken);

        AuditCompletenessMeasurement result = await Measurer(store, projection).MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        result.ReconstructableCount.ShouldBe(0);
        result.Fraction.ShouldBe(0.0);
    }

    [Fact]
    public async Task UnmeasurableRunFailsClosedToBreachNeverFabricatedComplete()
    {
        // Enumeration throws → the measurement cannot complete → Unmeasurable (breach), never a fabricated 1.0.
        WormAuditTestData.ThrowingWormAuditStore store = new();
        AuditCompletenessMeasurement result = await Measurer(store, new InMemoryGovernedOperationProjectionStore())
            .MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        result.IsMeasurable.ShouldBeFalse();
        result.IsBreach.ShouldBeTrue();
        result.ReasonCode.ShouldBe(AuditCompletenessMeasurement.UnmeasurableReasonCode);
    }

    [Fact]
    public async Task ProjectionReadFailureFailsClosedToBreach()
    {
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);

        AuditCompletenessMeasurement result = await Measurer(store, new ThrowingProjectionStore())
            .MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        result.IsMeasurable.ShouldBeFalse();
        result.IsBreach.ShouldBeTrue();
    }

    [Fact]
    public async Task EmptyWindowIsVacuouslyComplete()
    {
        // A tenant with no in-scope operations completed the measurement with zero work — vacuously complete (1.0),
        // distinct from "cannot complete". It must not page anyone.
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(WormAuditTestData.Envelope("tenant-alpha"), TestContext.Current.CancellationToken);

        // Clock is far in the future so the FixedNow-stamped record falls outside the rolling 7-day window.
        AuditCompletenessMeasurer measurer = new(
            store,
            new InMemoryGovernedOperationProjectionStore(),
            new WormAuditTestData.FixedClock(WormAuditTestData.FixedNow.AddDays(30)));

        AuditCompletenessMeasurement result = await measurer.MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        result.IsMeasurable.ShouldBeTrue();
        result.TotalCount.ShouldBe(0);
        result.Fraction.ShouldBe(1.0);
    }

    [Fact]
    public async Task ReplayEnvelopesAreExcludedFromBothNumeratorAndDenominator()
    {
        // AC3 (FR95a): a replay-marked operation is removed from BOTH terms. Without exclusion the un-projected replay
        // op would drag the fraction to 0.5; with exclusion the fraction stays 1.0 over the single production op.
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-replay") with { ReplayRunId = "replay-run-1" }, TestContext.Current.CancellationToken);
        InMemoryGovernedOperationProjectionStore projection = new();
        await projection.SaveAsync(Projection("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);

        AuditCompletenessMeasurement result = await Measurer(store, projection).MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        result.TotalCount.ShouldBe(1);
        result.ReconstructableCount.ShouldBe(1);
        result.Fraction.ShouldBe(1.0);
    }

    [Fact]
    public async Task MeasurementIsPerTenantIsolatedWithNoCrossTenantLinkage()
    {
        // tenant-beta has a diverging projection for the SAME noteId; measuring tenant-alpha must read only alpha's
        // projection (tenant-partitioned key) and stay reconstructable (NFR9a).
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        _ = await store.AppendAsync(MappedEnvelope("tenant-beta", "note-1"), TestContext.Current.CancellationToken);
        InMemoryGovernedOperationProjectionStore projection = new();
        await projection.SaveAsync(Projection("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        await projection.SaveAsync(Projection("tenant-beta", "note-1", redactionState: "diverged"), TestContext.Current.CancellationToken);

        AuditCompletenessMeasurement alpha = await Measurer(store, projection).MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        alpha.Fraction.ShouldBe(1.0);
        alpha.ReconstructableCount.ShouldBe(1);
    }

    [Fact]
    public async Task MeasurementIsReadOnlyAndAddsNoCommitPathSideEffect()
    {
        // Two-phase audit (D4): the measurement only reads. The chain length is unchanged after measuring, and the
        // measurer has no audit-writer/alert dependency at all (it cannot mutate the commit path).
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        int before = store.EnumerateChain("tenant-alpha").Count;

        _ = await Measurer(store, new InMemoryGovernedOperationProjectionStore()).MeasureAllTenantsAsync(TestContext.Current.CancellationToken);

        store.EnumerateChain("tenant-alpha").Count.ShouldBe(before);
    }

    [Fact]
    public async Task FirstDivergingLocatorIsASafeToken()
    {
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);

        AuditCompletenessMeasurement result = await Measurer(store, new InMemoryGovernedOperationProjectionStore())
            .MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        string locator = result.FirstDivergingOperationLocator.ShouldNotBeNull();
        foreach (string banned in new[] { "secret", "password", "bearer", "token", "exception", ".txt", ".json", ".xml" })
        {
            locator.ShouldNotContain(banned, Case.Insensitive);
        }
    }

    [Fact]
    public async Task PartialReconstructabilityYieldsTheCorrectFractionAndFirstDivergingLocator()
    {
        // AC2: the fraction is reconstructable ÷ total over MORE THAN ONE operation. Three production operations,
        // two with an agreeing projection (reconstructable) and one without (diverges) → fraction 2/3, and the locator
        // points at the first (in chain order) non-reconstructable operation.
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-2"), TestContext.Current.CancellationToken);
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-3"), TestContext.Current.CancellationToken);
        InMemoryGovernedOperationProjectionStore projection = new();
        await projection.SaveAsync(Projection("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);
        await projection.SaveAsync(Projection("tenant-alpha", "note-2"), TestContext.Current.CancellationToken);
        // note-3 deliberately has no projection → diverges.

        AuditCompletenessMeasurement result = await Measurer(store, projection).MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        result.IsMeasurable.ShouldBeTrue();
        result.TotalCount.ShouldBe(3);
        result.ReconstructableCount.ShouldBe(2);
        result.Fraction.ShouldBe(2.0 / 3.0);
        result.FirstDivergingOperationLocator.ShouldBe("op:note-3");
    }

    [Fact]
    public async Task OperationsOutsideTheRollingSevenDayWindowAreExcludedFromBothTerms()
    {
        // AC2: the denominator is the rolling 7-day window. An operation stamped 10 days ago is out of window and is
        // excluded from BOTH numerator and denominator — even though it has no projection (would otherwise drag the
        // fraction down). Only the in-window, projection-agreeing operation is counted → total 1, fraction 1.0.
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-recent"), TestContext.Current.CancellationToken);
        _ = await store.AppendAsync(
            MappedEnvelope("tenant-alpha", "note-stale") with { Timestamp = WormAuditTestData.FixedNow.AddDays(-10) },
            TestContext.Current.CancellationToken);
        InMemoryGovernedOperationProjectionStore projection = new();
        await projection.SaveAsync(Projection("tenant-alpha", "note-recent"), TestContext.Current.CancellationToken);

        AuditCompletenessMeasurement result = await Measurer(store, projection).MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        result.IsMeasurable.ShouldBeTrue();
        result.TotalCount.ShouldBe(1);
        result.ReconstructableCount.ShouldBe(1);
        result.Fraction.ShouldBe(1.0);
    }

    [Fact]
    public async Task ProjectionFromAnotherTenantDivergesAndIsNotReconstructable()
    {
        // NFR9a defensive guard: even if a projection store returned a view whose TenantId is not the measured tenant,
        // the measurer treats the cross-tenant view as divergence (never reconstructable), so no measurement can be
        // satisfied by another tenant's state.
        InMemoryWormAuditStore store = new();
        _ = await store.AppendAsync(MappedEnvelope("tenant-alpha", "note-1"), TestContext.Current.CancellationToken);

        AuditCompletenessMeasurement result = await Measurer(store, new CrossTenantProjectionStore())
            .MeasureTenantAsync("tenant-alpha", TestContext.Current.CancellationToken);

        result.IsMeasurable.ShouldBeTrue();
        result.TotalCount.ShouldBe(1);
        result.ReconstructableCount.ShouldBe(0);
        result.Fraction.ShouldBe(0.0);
    }

    private sealed class CrossTenantProjectionStore : IGovernedOperationProjectionStore
    {
        // Returns a structurally-matching view but stamped with a DIFFERENT tenant id, exercising the measurer's
        // cross-tenant linkage guard.
        public Task<GovernedOperationView?> GetAsync(string tenantId, string noteId, CancellationToken cancellationToken = default)
            => Task.FromResult<GovernedOperationView?>(new GovernedOperationView(
                "tenant-other",
                noteId,
                GovernedOperationView.CurrentSchemaVersion,
                GovernedOperationView.GovernedCommandProvenance,
                GovernedOperationView.CurrentDerivationKernelVersion,
                GovernedOperationView.MetadataOnlyRedactionState,
                GovernedOperationView.GovernedOperationalRetentionClass,
                SourceVersion: 1,
                RecordedAt: WormAuditTestData.FixedNow,
                LastUpdatedAt: WormAuditTestData.FixedNow));

        public Task SaveAsync(GovernedOperationView view, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ThrowingProjectionStore : IGovernedOperationProjectionStore
    {
        public Task<GovernedOperationView?> GetAsync(string tenantId, string noteId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("projection store down");

        public Task SaveAsync(GovernedOperationView view, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
