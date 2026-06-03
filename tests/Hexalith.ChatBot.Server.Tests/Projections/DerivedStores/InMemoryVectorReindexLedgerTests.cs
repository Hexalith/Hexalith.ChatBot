using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections.DerivedStores;

/// <summary>
/// Story 9.6 (AC1) coverage for the single version-guard authority <see cref="InMemoryVectorReindexLedger"/>: the
/// order-tolerant last-writer-wins <c>TryAdvance</c> contract. A fresh partition advances; a re-delivered equal version
/// (the <c>&lt;=</c> boundary) and an out-of-order older version are both no-ops; a strictly-newer version advances;
/// per-(class, tenant) partitions are independent so advancing one never advances another; and an unsafe tenant id is
/// fail-closed (the partition prefix rejects it). The reindexer relies on this guard for its idempotency property, so it
/// is asserted directly here rather than only through the reindexer.
/// </summary>
public sealed class InMemoryVectorReindexLedgerTests
{
    private const string TenantAlpha = "tenant-alpha";
    private const string TenantBeta = "tenant-beta";

    [Fact]
    public void TryAdvanceOnAFreshPartitionAdvances()
    {
        InMemoryVectorReindexLedger ledger = new();

        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeTrue();
    }

    [Fact]
    public void ReDeliveringTheSameVersionIsANoOpAtTheBoundary()
    {
        InMemoryVectorReindexLedger ledger = new();

        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeTrue();

        // sourceVersion == lastApplied is the <= boundary — last-writer-wins keeps the watermark, no advance.
        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeFalse();
    }

    [Fact]
    public void AnOlderVersionIsANoOp()
    {
        InMemoryVectorReindexLedger ledger = new();

        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeTrue();

        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantAlpha, 3).ShouldBeFalse();
    }

    [Fact]
    public void AStrictlyNewerVersionAdvances()
    {
        InMemoryVectorReindexLedger ledger = new();

        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeTrue();

        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantAlpha, 6).ShouldBeTrue();
        // ...and the new watermark is now the one that holds (re-delivering v6 is a no-op).
        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantAlpha, 6).ShouldBeFalse();
    }

    [Fact]
    public void EachDerivedStoreClassIsAnIndependentPartition()
    {
        InMemoryVectorReindexLedger ledger = new();

        // Advancing one class must not advance another class for the same tenant + version.
        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeTrue();

        ledger.TryAdvance(DerivedStoreClass.EmbeddingStore, TenantAlpha, 5).ShouldBeTrue();
        ledger.TryAdvance(DerivedStoreClass.PromptContextCache, TenantAlpha, 5).ShouldBeTrue();
        ledger.TryAdvance(DerivedStoreClass.CandidateRankingCache, TenantAlpha, 5).ShouldBeTrue();
    }

    [Fact]
    public void EachTenantIsAnIndependentPartition()
    {
        InMemoryVectorReindexLedger ledger = new();

        // Tenant alpha's watermark must never gate tenant beta's first reindex of the same class + version.
        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeTrue();

        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantBeta, 5).ShouldBeTrue();
    }

    [Fact]
    public void ShouldReindexIsAPureReadThatDoesNotCommitTheWatermark()
    {
        InMemoryVectorReindexLedger ledger = new();

        // A pure read returns true for a fresh partition — and, crucially, does NOT advance the watermark, so a later
        // commit (TryAdvance) for the same version still succeeds. This is what lets a failed reindex be retried.
        ledger.ShouldReindex(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeTrue();
        ledger.ShouldReindex(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeTrue();

        // Only TryAdvance commits — after it, the same version is no longer reindexable.
        ledger.TryAdvance(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeTrue();
        ledger.ShouldReindex(DerivedStoreClass.VectorIndex, TenantAlpha, 5).ShouldBeFalse();
        ledger.ShouldReindex(DerivedStoreClass.VectorIndex, TenantAlpha, 6).ShouldBeTrue();
    }

    [Fact]
    public void ShouldReindexWithAnUnsafeTenantIdThrowsFailClosed()
    {
        InMemoryVectorReindexLedger ledger = new();

        Should.Throw<ArgumentException>(
            () => ledger.ShouldReindex(DerivedStoreClass.VectorIndex, "tenant with spaces", 5));
    }

    [Fact]
    public void TryAdvanceWithAnUnsafeTenantIdThrowsFailClosed()
    {
        InMemoryVectorReindexLedger ledger = new();

        // The partition prefix rejects an unsafe tenant id (fail-closed) — a malformed tenant resolves no partition.
        Should.Throw<ArgumentException>(
            () => ledger.TryAdvance(DerivedStoreClass.VectorIndex, "tenant with spaces", 5));
    }
}
