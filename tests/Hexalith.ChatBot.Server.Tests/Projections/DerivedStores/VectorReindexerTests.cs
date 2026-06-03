using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections.DerivedStores;

/// <summary>
/// Story 9.6 (AC1/AC2) coverage for the in-memory <c>ReindexVectors</c> operation: a correction invalidates the prior
/// entries and rebuilds the corrected ones across all four derived-store classes; re-running the SAME
/// <c>(tenant, correction, sourceVersion)</c> is an idempotent version-guard no-op (no duplicate entries); an OLDER
/// source version is skipped; a foreign tenant cannot reindex another tenant's partition; the outcome counts are
/// accurate; and a throwing store fails closed with <c>vector_reindex_failed</c> rather than a silent partial success.
/// </summary>
public sealed class VectorReindexerTests
{
    private const string TenantAlpha = "tenant-alpha";
    private const string TenantBeta = "tenant-beta";
    private const string ResourceId = "assoc-001:project-prior";
    private static readonly DateTimeOffset StartedAt = new(2026, 6, 3, 9, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyList<string> Affected = [ResourceId];

    private static InMemoryVectorReindexer NewReindexer(IDerivedStore store, ISystemClock clock)
        => new(store, new InMemoryVectorReindexLedger(), clock);

    private static async Task SeedPriorEntriesAsync(IDerivedStore store, string tenantId, CancellationToken token)
    {
        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            await store.PutAsync(cls, tenantId, ResourceId, DerivedStoreEntry.Create(ResourceId, "stale-prior-digest"), token).ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task ACorrectionInvalidatesPriorEntriesAndRebuildsAcrossAllFourClasses()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        await SeedPriorEntriesAsync(store, TenantAlpha, token);
        InMemoryVectorReindexer reindexer = NewReindexer(store, new FixedClock(StartedAt.AddMinutes(1)));

        VectorReindexOutcome outcome = await reindexer.ReindexVectorsAsync(TenantAlpha, "assoc-001:correction:5", 5, Affected, StartedAt, token);

        outcome.VersionGuardSkipped.ShouldBeFalse();
        outcome.FailureReasonCode.ShouldBeNull();
        outcome.EntriesInvalidated.ShouldBe(4);
        outcome.EntriesRebuilt.ShouldBe(4);
        outcome.DeadlineUtc.ShouldBe(StartedAt.AddMinutes(60));
        outcome.SloBreached.ShouldBeFalse();

        // The stale prior digest is physically replaced by the correction-stamped rebuilt digest in every class.
        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            DerivedStoreEntry? entry = await store.GetAsync(cls, TenantAlpha, ResourceId, token);
            entry.ShouldNotBeNull();
            entry.ContentDigest.ShouldBe("reindex:assoc-001:correction:5:5");
            store.EnumerateResourceIds(cls, TenantAlpha).ShouldBe([ResourceId]);
        }
    }

    [Fact]
    public async Task ReRunningTheSameCorrectionIsAnIdempotentVersionGuardNoOpWithNoDuplicateEntries()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        await SeedPriorEntriesAsync(store, TenantAlpha, token);
        InMemoryVectorReindexer reindexer = NewReindexer(store, new FixedClock(StartedAt.AddMinutes(1)));

        _ = await reindexer.ReindexVectorsAsync(TenantAlpha, "assoc-001:correction:5", 5, Affected, StartedAt, token);
        VectorReindexOutcome second = await reindexer.ReindexVectorsAsync(TenantAlpha, "assoc-001:correction:5", 5, Affected, StartedAt, token);

        // Same (tenant, correction, sourceVersion): every partition is already at v5 — nothing advances.
        second.VersionGuardSkipped.ShouldBeTrue();
        second.EntriesInvalidated.ShouldBe(0);
        second.EntriesRebuilt.ShouldBe(0);
        second.FailureReasonCode.ShouldBeNull();

        // No duplicate entries — exactly one resource id per class, same corrected digest.
        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            store.EnumerateResourceIds(cls, TenantAlpha).ShouldBe([ResourceId]);
            (await store.GetAsync(cls, TenantAlpha, ResourceId, token)).ShouldNotBeNull().ContentDigest.ShouldBe("reindex:assoc-001:correction:5:5");
        }
    }

    [Fact]
    public async Task AnOlderSourceVersionIsSkipped()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        InMemoryVectorReindexer reindexer = NewReindexer(store, new FixedClock(StartedAt.AddMinutes(1)));

        _ = await reindexer.ReindexVectorsAsync(TenantAlpha, "assoc-001:correction:5", 5, Affected, StartedAt, token);
        VectorReindexOutcome older = await reindexer.ReindexVectorsAsync(TenantAlpha, "assoc-001:correction:3", 3, Affected, StartedAt, token);

        older.VersionGuardSkipped.ShouldBeTrue();
        older.EntriesInvalidated.ShouldBe(0);
        older.EntriesRebuilt.ShouldBe(0);
        // The v5 corrected entry survives — an out-of-order older correction never rolls it back.
        (await store.GetAsync(DerivedStoreClass.VectorIndex, TenantAlpha, ResourceId, token)).ShouldNotBeNull().ContentDigest.ShouldBe("reindex:assoc-001:correction:5:5");
    }

    [Fact]
    public async Task AForeignTenantCannotReindexAnotherTenantsPartition()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        await SeedPriorEntriesAsync(store, TenantAlpha, token);
        InMemoryVectorReindexer reindexer = NewReindexer(store, new FixedClock(StartedAt.AddMinutes(1)));

        // Reindex under tenant-beta for the SAME logical resource id: it rebuilds beta's own partition only.
        VectorReindexOutcome outcome = await reindexer.ReindexVectorsAsync(TenantBeta, "assoc-001:correction:5", 5, Affected, StartedAt, token);

        // Beta had nothing to invalidate (its partition was empty) but rebuilt its own corrected entries.
        outcome.EntriesInvalidated.ShouldBe(0);
        outcome.EntriesRebuilt.ShouldBe(4);
        // Tenant-alpha's entries are untouched — its stale digest is still present (beta's reindex never reached it).
        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            (await store.GetAsync(cls, TenantAlpha, ResourceId, token)).ShouldNotBeNull().ContentDigest.ShouldBe("stale-prior-digest");
        }
    }

    [Fact]
    public async Task AReindexThatCompletesPastTheM2DeadlineReportsSloBreached()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        await SeedPriorEntriesAsync(store, TenantAlpha, token);
        // The clock is 61 min after start — past the 60-min M2 deadline, so the completed reindex is late (NFR17a).
        InMemoryVectorReindexer reindexer = NewReindexer(store, new FixedClock(StartedAt.AddMinutes(61)));

        VectorReindexOutcome outcome = await reindexer.ReindexVectorsAsync(TenantAlpha, "assoc-001:correction:5", 5, Affected, StartedAt, token);

        // The reindex still succeeded (no failure reason) but breached its SLO — the coordinator surfaces this as
        // vector_reindex_slo_exceeded ⇒ correction-delayed.
        outcome.FailureReasonCode.ShouldBeNull();
        outcome.DeadlineUtc.ShouldBe(StartedAt.AddMinutes(60));
        outcome.SloBreached.ShouldBeTrue();
        outcome.EntriesRebuilt.ShouldBe(4);
    }

    [Fact]
    public async Task MultipleAffectedResourceIdsScaleTheInvalidatedAndRebuiltCountsAcrossEveryClass()
    {
        const string secondResourceId = "assoc-001:project-prior-2";
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        await SeedPriorEntriesAsync(store, TenantAlpha, token);
        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            await store.PutAsync(cls, TenantAlpha, secondResourceId, DerivedStoreEntry.Create(secondResourceId, "stale-prior-digest"), token);
        }

        InMemoryVectorReindexer reindexer = NewReindexer(store, new FixedClock(StartedAt.AddMinutes(1)));

        VectorReindexOutcome outcome = await reindexer.ReindexVectorsAsync(
            TenantAlpha,
            "assoc-001:correction:5",
            5,
            [ResourceId, secondResourceId],
            StartedAt,
            token);

        // Two resource ids across four classes ⇒ 8 invalidated + 8 rebuilt; both ids survive with the corrected digest.
        outcome.EntriesInvalidated.ShouldBe(8);
        outcome.EntriesRebuilt.ShouldBe(8);
        outcome.VersionGuardSkipped.ShouldBeFalse();
        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            store.EnumerateResourceIds(cls, TenantAlpha).OrderBy(static id => id, StringComparer.Ordinal)
                .ShouldBe([ResourceId, secondResourceId]);
            (await store.GetAsync(cls, TenantAlpha, secondResourceId, token)).ShouldNotBeNull().ContentDigest.ShouldBe("reindex:assoc-001:correction:5:5");
        }
    }

    [Fact]
    public async Task AnEmptyAffectedResourceIdListAdvancesTheGuardButInvalidatesAndRebuildsNothing()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        InMemoryVectorReindexer reindexer = NewReindexer(store, new FixedClock(StartedAt.AddMinutes(1)));

        VectorReindexOutcome outcome = await reindexer.ReindexVectorsAsync(TenantAlpha, "assoc-001:correction:5", 5, [], StartedAt, token);

        // Nothing was affected, so no entry was touched — but the version guard still advanced (it was not skipped),
        // so a re-delivered older/equal correction afterward remains a no-op.
        outcome.EntriesInvalidated.ShouldBe(0);
        outcome.EntriesRebuilt.ShouldBe(0);
        outcome.VersionGuardSkipped.ShouldBeFalse();
        outcome.FailureReasonCode.ShouldBeNull();

        VectorReindexOutcome reDelivered = await reindexer.ReindexVectorsAsync(TenantAlpha, "assoc-001:correction:5", 5, [], StartedAt, token);
        reDelivered.VersionGuardSkipped.ShouldBeTrue();
    }

    [Fact]
    public async Task AThrowingStoreFailsClosedWithTheVectorReindexFailedReasonCode()
    {
        ThrowingDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        InMemoryVectorReindexer reindexer = NewReindexer(store, new FixedClock(StartedAt.AddMinutes(1)));

        VectorReindexOutcome outcome = await reindexer.ReindexVectorsAsync(TenantAlpha, "assoc-001:correction:5", 5, Affected, StartedAt, token);

        outcome.FailureReasonCode.ShouldBe(InMemoryVectorReindexer.VectorReindexFailedReasonCode);
    }

    [Fact]
    public async Task APartitionWhoseReindexFailedIsNotMarkedAppliedSoARedeliveryReindexesItRatherThanSkipping()
    {
        // A transient store failure on the FIRST attempt (the case the deferred live Redis/FalkorDB binding will hit):
        // the version-guard watermark must NOT advance for the failed partition, otherwise a redelivery of the SAME
        // correction would skip it and leave the prior association's stale entry behind (FR91a/NFR9a).
        FlakyDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;
        await SeedPriorEntriesAsync(store, TenantAlpha, token);
        IVectorReindexLedger ledger = new InMemoryVectorReindexLedger();
        InMemoryVectorReindexer reindexer = new(store, ledger, new FixedClock(StartedAt.AddMinutes(1)));

        // First delivery: the store throws on the first rebuild ⇒ fail-closed, no partition is durably reindexed.
        store.FailNextPut = true;
        VectorReindexOutcome first = await reindexer.ReindexVectorsAsync(TenantAlpha, "assoc-001:correction:5", 5, Affected, StartedAt, token);
        first.FailureReasonCode.ShouldBe(InMemoryVectorReindexer.VectorReindexFailedReasonCode);

        // The failed partition's watermark never advanced, so it is still seen as needing reindex.
        ledger.ShouldReindex(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeTrue();

        // Redelivery of the SAME correction now succeeds and DOES reindex every class (not a version-guard skip).
        VectorReindexOutcome second = await reindexer.ReindexVectorsAsync(TenantAlpha, "assoc-001:correction:5", 5, Affected, StartedAt, token);
        second.VersionGuardSkipped.ShouldBeFalse();
        second.FailureReasonCode.ShouldBeNull();
        second.EntriesRebuilt.ShouldBe(4);
        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            (await store.GetAsync(cls, TenantAlpha, ResourceId, token)).ShouldNotBeNull().ContentDigest.ShouldBe("reindex:assoc-001:correction:5:5");
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    // A real in-memory store that throws on the NEXT PutAsync when armed — models a transient backing-store failure so
    // the retry/idempotency property can be exercised against actual stored state.
    private sealed class FlakyDerivedStore : IDerivedStore
    {
        private readonly InMemoryDerivedStore _inner = new();

        public bool FailNextPut { get; set; }

        public ValueTask PutAsync(DerivedStoreClass cls, string tenantId, string resourceId, DerivedStoreEntry entry, CancellationToken cancellationToken)
        {
            if (FailNextPut)
            {
                FailNextPut = false;
                throw new InvalidOperationException("simulated transient store failure");
            }

            return _inner.PutAsync(cls, tenantId, resourceId, entry, cancellationToken);
        }

        public ValueTask<DerivedStoreEntry?> GetAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
            => _inner.GetAsync(cls, tenantId, resourceId, cancellationToken);

        public IReadOnlyList<string> EnumerateResourceIds(DerivedStoreClass cls, string tenantId)
            => _inner.EnumerateResourceIds(cls, tenantId);

        public IReadOnlyList<string> EnumerateTenants() => _inner.EnumerateTenants();

        public ValueTask<bool> InvalidateAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
            => _inner.InvalidateAsync(cls, tenantId, resourceId, cancellationToken);
    }

    // A store whose write path throws — exercises the fail-closed reindex branch without a real backing store.
    private sealed class ThrowingDerivedStore : IDerivedStore
    {
        public ValueTask PutAsync(DerivedStoreClass cls, string tenantId, string resourceId, DerivedStoreEntry entry, CancellationToken cancellationToken)
            => throw new InvalidOperationException("simulated store failure");

        public ValueTask<DerivedStoreEntry?> GetAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
            => ValueTask.FromResult<DerivedStoreEntry?>(null);

        public IReadOnlyList<string> EnumerateResourceIds(DerivedStoreClass cls, string tenantId) => [];

        public IReadOnlyList<string> EnumerateTenants() => [];

        public ValueTask<bool> InvalidateAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
            => ValueTask.FromResult(false);
    }
}
