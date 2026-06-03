using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections.DerivedStores;

/// <summary>
/// Story 9.5 (AC1, FR55a/NFR9a) coverage for the tenant-partitioned-by-construction in-memory derived store: the SAME
/// logical resource id seeded under two tenants reads back only its OWN entry; a third, unseeded tenant gets a safe
/// not-found; a read under tenant B never observes tenant A's entry for ANY of the four derived-store classes; and the
/// per-tenant enumeration only ever returns that tenant's own resources. Mirrors
/// <c>CrossTenantStorePartitioningTests</c> — isolation is structural (a key miss at the store layer), not a filter.
/// </summary>
public sealed class InMemoryDerivedStoreTests
{
    private const string TenantAlpha = "tenant-alpha";
    private const string TenantBeta = "tenant-beta";
    private const string TenantGamma = "tenant-gamma";
    private const string SharedResourceId = "res-shared-001";

    [Fact]
    public async Task SameResourceIdUnderTwoTenantsReadsBackOnlyItsOwnEntryForEveryClass()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            await store.PutAsync(cls, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "digest-alpha"), token);
            await store.PutAsync(cls, TenantBeta, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "digest-beta"), token);

            DerivedStoreEntry? alpha = await store.GetAsync(cls, TenantAlpha, SharedResourceId, token);
            DerivedStoreEntry? beta = await store.GetAsync(cls, TenantBeta, SharedResourceId, token);

            alpha.ShouldNotBeNull();
            beta.ShouldNotBeNull();
            alpha.ContentDigest.ShouldBe("digest-alpha");
            beta.ContentDigest.ShouldBe("digest-beta");
        }
    }

    [Fact]
    public async Task AThirdUnseededTenantGetsASafeNotFoundForEveryClass()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            await store.PutAsync(cls, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "digest-alpha"), token);

            // A foreign/unknown tenant read yields null — it never confirms tenant-alpha's resource exists.
            (await store.GetAsync(cls, TenantGamma, SharedResourceId, token)).ShouldBeNull();
        }
    }

    [Fact]
    public async Task AReadUnderTenantBNeverObservesTenantAEntryForEveryClass()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            // Only tenant-alpha seeds the resource.
            await store.PutAsync(cls, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "digest-alpha"), token);

            // Tenant-beta reads the SAME logical resource id through its own scope — structurally a key miss.
            (await store.GetAsync(cls, TenantBeta, SharedResourceId, token)).ShouldBeNull();
            store.EnumerateResourceIds(cls, TenantBeta).ShouldBeEmpty();
            store.EnumerateResourceIds(cls, TenantAlpha).ShouldBe([SharedResourceId]);
        }
    }

    [Fact]
    public async Task EnumerateTenantsReturnsOnlyTenantsThatHoldEntries()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        await store.PutAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "d"), token);
        await store.PutAsync(DerivedStoreClass.EmbeddingStore, TenantBeta, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "d"), token);

        store.EnumerateTenants().ShouldBe([TenantAlpha, TenantBeta], ignoreOrder: true);
    }

    [Fact]
    public async Task EntriesInDifferentClassesForOneTenantDoNotCollide()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        await store.PutAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "vec"), token);
        await store.PutAsync(DerivedStoreClass.EmbeddingStore, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "emb"), token);

        (await store.GetAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, token)).ShouldNotBeNull().ContentDigest.ShouldBe("vec");
        (await store.GetAsync(DerivedStoreClass.EmbeddingStore, TenantAlpha, SharedResourceId, token)).ShouldNotBeNull().ContentDigest.ShouldBe("emb");
        // A class with no entry for the tenant is a safe not-found.
        (await store.GetAsync(DerivedStoreClass.PromptContextCache, TenantAlpha, SharedResourceId, token)).ShouldBeNull();
    }

    [Fact]
    public async Task PutWithAnUnsafeTenantIdThrowsFailClosed()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        _ = await Should.ThrowAsync<ArgumentException>(
            () => store.PutAsync(DerivedStoreClass.VectorIndex, "tenant with spaces", SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "d"), token).AsTask());
    }

    [Fact]
    public async Task PutOverwritesTheEntryForTheSameTenantClassAndResource()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        await store.PutAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "first"), token);
        await store.PutAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "second"), token);

        // Last write wins for a given partition key — no duplicate resource id is left behind.
        (await store.GetAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, token)).ShouldNotBeNull().ContentDigest.ShouldBe("second");
        store.EnumerateResourceIds(DerivedStoreClass.VectorIndex, TenantAlpha).ShouldBe([SharedResourceId]);
    }

    [Fact]
    public async Task PutWithANullEntryThrows()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        _ = await Should.ThrowAsync<ArgumentNullException>(
            () => store.PutAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, null!, token).AsTask());
    }

    [Fact]
    public async Task GetWithAnUnsafeTenantIdThrowsFailClosed()
    {
        // Reads are fail-closed too: an unsafe tenant id resolves NO partition, so a read can never fall back to a
        // shared/global bucket (the partition prefix is validated before any lookup).
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        _ = await Should.ThrowAsync<ArgumentException>(
            () => store.GetAsync(DerivedStoreClass.VectorIndex, "tenant/secret", SharedResourceId, token).AsTask());
    }

    [Fact]
    public void EnumerateResourceIdsWithAnUnsafeTenantIdThrowsFailClosed()
    {
        InMemoryDerivedStore store = new();

        Should.Throw<ArgumentException>(() => store.EnumerateResourceIds(DerivedStoreClass.VectorIndex, "tenant with spaces"));
    }

    [Fact]
    public async Task PutHonorsCancellation()
    {
        InMemoryDerivedStore store = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => store.PutAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "d"), cts.Token).AsTask());
    }

    [Fact]
    public async Task GetHonorsCancellation()
    {
        InMemoryDerivedStore store = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => store.GetAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, cts.Token).AsTask());
    }

    // ----- Story 9.6 (AC1): the InvalidateAsync delete seam closing the Story 9.5 review follow-up -----

    [Fact]
    public async Task InvalidateRemovesTheEntrySoASubsequentGetIsTheSafeNotFoundForEveryClass()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
        {
            await store.PutAsync(cls, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "digest-alpha"), token);

            // Invalidate returns true (an entry was present) and physically removes it — Get is a real key miss after.
            (await store.InvalidateAsync(cls, TenantAlpha, SharedResourceId, token)).ShouldBeTrue();
            (await store.GetAsync(cls, TenantAlpha, SharedResourceId, token)).ShouldBeNull();
            store.EnumerateResourceIds(cls, TenantAlpha).ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task ReInvalidatingAnAbsentEntryIsAnIdempotentNoOpReturningFalse()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        await store.PutAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "d"), token);

        (await store.InvalidateAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, token)).ShouldBeTrue();
        // Re-invalidate: nothing left to remove — false, no throw (idempotent).
        (await store.InvalidateAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, token)).ShouldBeFalse();
        // Invalidating a never-seeded resource id is also a no-op false.
        (await store.InvalidateAsync(DerivedStoreClass.VectorIndex, TenantAlpha, "never-seeded", token)).ShouldBeFalse();
    }

    [Fact]
    public async Task InvalidatingTenantBsResourceIdNeverRemovesTenantAsSameIdEntry()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        // Both tenants hold the SAME logical resource id in the same class.
        await store.PutAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "alpha"), token);
        await store.PutAsync(DerivedStoreClass.VectorIndex, TenantBeta, SharedResourceId, DerivedStoreEntry.Create(SharedResourceId, "beta"), token);

        // Tenant B invalidates its own entry; tenant A's same-id entry is untouched (structural isolation).
        (await store.InvalidateAsync(DerivedStoreClass.VectorIndex, TenantBeta, SharedResourceId, token)).ShouldBeTrue();
        (await store.GetAsync(DerivedStoreClass.VectorIndex, TenantBeta, SharedResourceId, token)).ShouldBeNull();
        (await store.GetAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, token)).ShouldNotBeNull().ContentDigest.ShouldBe("alpha");

        // A foreign/unknown tenant invalidating tenant A's id is a no-op that never touches tenant A.
        (await store.InvalidateAsync(DerivedStoreClass.VectorIndex, TenantGamma, SharedResourceId, token)).ShouldBeFalse();
        (await store.GetAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, token)).ShouldNotBeNull().ContentDigest.ShouldBe("alpha");
    }

    [Fact]
    public async Task InvalidateWithAnUnsafeTenantIdThrowsFailClosed()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        _ = await Should.ThrowAsync<ArgumentException>(
            () => store.InvalidateAsync(DerivedStoreClass.VectorIndex, "tenant with spaces", SharedResourceId, token).AsTask());
    }

    [Fact]
    public async Task InvalidateWithAnUnsafeResourceIdThrowsFailClosed()
    {
        InMemoryDerivedStore store = new();
        CancellationToken token = TestContext.Current.CancellationToken;

        _ = await Should.ThrowAsync<ArgumentException>(
            () => store.InvalidateAsync(DerivedStoreClass.VectorIndex, TenantAlpha, "   ", token).AsTask());
    }

    [Fact]
    public async Task InvalidateHonorsCancellation()
    {
        InMemoryDerivedStore store = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => store.InvalidateAsync(DerivedStoreClass.VectorIndex, TenantAlpha, SharedResourceId, cts.Token).AsTask());
    }
}
