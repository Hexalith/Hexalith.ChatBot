using Hexalith.ChatBot.Server.Projections.DerivedStores;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Projections;

/// <summary>
/// Story 9.5 (AC1, FR55a/NFR9a) coverage for the single-source derived-store partition contract: every key is
/// tenant-prefixed-by-construction (<c>{tenant}:{derived-class}:{resourceId}</c>); the SAME logical resource id under
/// two tenants — or two derived-store classes — never collides; and an empty/unsafe tenant id or resource id resolves
/// NO partition (fail-closed throw), never a shared/global key. Mirrors
/// <c>CrossTenantStorePartitioningTests.KeyForShouldBeTenantPrefixedAndDistinctAcrossTenants</c>.
/// </summary>
public sealed class DerivedStorePartitionTests
{
    private const string TenantAlpha = "tenant-alpha";
    private const string TenantBeta = "tenant-beta";
    private const string ResourceId = "res-001";

    [Fact]
    public void KeyForIsTenantPrefixedAndDistinctAcrossTenants()
    {
        string alphaKey = DerivedStorePartition.KeyFor(DerivedStoreClass.VectorIndex, TenantAlpha, ResourceId);
        string betaKey = DerivedStorePartition.KeyFor(DerivedStoreClass.VectorIndex, TenantBeta, ResourceId);

        alphaKey.ShouldBe($"{TenantAlpha}:{DerivedStorePartition.VectorIndexSegment}:{ResourceId}");
        alphaKey.ShouldStartWith($"{TenantAlpha}:");
        betaKey.ShouldStartWith($"{TenantBeta}:");
        // Same logical resource id, different tenants → the key is never shared across tenants.
        alphaKey.ShouldNotBe(betaKey);
    }

    [Fact]
    public void EachDerivedStoreClassProducesADistinctPartitionSegment()
    {
        string[] keys =
        [
            DerivedStorePartition.KeyFor(DerivedStoreClass.VectorIndex, TenantAlpha, ResourceId),
            DerivedStorePartition.KeyFor(DerivedStoreClass.EmbeddingStore, TenantAlpha, ResourceId),
            DerivedStorePartition.KeyFor(DerivedStoreClass.PromptContextCache, TenantAlpha, ResourceId),
            DerivedStorePartition.KeyFor(DerivedStoreClass.CandidateRankingCache, TenantAlpha, ResourceId),
        ];

        // The same tenant + same resource id under four classes → four distinct keys.
        keys.Distinct(StringComparer.Ordinal).Count().ShouldBe(4);
    }

    [Fact]
    public void PartitionPrefixIsTheTenantClassOwnerOfEveryKey()
    {
        string prefix = DerivedStorePartition.PartitionPrefix(DerivedStoreClass.EmbeddingStore, TenantAlpha);
        string key = DerivedStorePartition.KeyFor(DerivedStoreClass.EmbeddingStore, TenantAlpha, ResourceId);

        prefix.ShouldBe($"{TenantAlpha}:{DerivedStorePartition.EmbeddingStoreSegment}:");
        key.ShouldStartWith(prefix);
    }

    [Fact]
    public void AllClassesEnumeratesTheFourDerivedStoreClasses()
        => DerivedStorePartition.AllClasses.ShouldBe(
            [
                DerivedStoreClass.VectorIndex,
                DerivedStoreClass.EmbeddingStore,
                DerivedStoreClass.PromptContextCache,
                DerivedStoreClass.CandidateRankingCache,
            ]);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tenant with spaces")]
    [InlineData("tenant/secret")]
    public void KeyForThrowsOnEmptyOrUnsafeTenantId(string tenantId)
        => Should.Throw<ArgumentException>(() => DerivedStorePartition.KeyFor(DerivedStoreClass.VectorIndex, tenantId, ResourceId));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("res with spaces")]
    public void KeyForThrowsOnEmptyOrUnsafeResourceId(string resourceId)
        => Should.Throw<ArgumentException>(() => DerivedStorePartition.KeyFor(DerivedStoreClass.VectorIndex, TenantAlpha, resourceId));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tenant with spaces")]
    public void PartitionPrefixThrowsOnEmptyOrUnsafeTenantId(string tenantId)
        => Should.Throw<ArgumentException>(() => DerivedStorePartition.PartitionPrefix(DerivedStoreClass.VectorIndex, tenantId));

    [Fact]
    public void SegmentReturnsTheStableWireConstantForEveryClass()
    {
        // The segment is part of every key and index name — a fixed wire constant the deferred M2 live binding adopts,
        // never a renamed enum-name reflection. Pin the literals so a rename is caught as a wire-format break.
        DerivedStorePartition.Segment(DerivedStoreClass.VectorIndex).ShouldBe("vector-index");
        DerivedStorePartition.Segment(DerivedStoreClass.EmbeddingStore).ShouldBe("embedding-store");
        DerivedStorePartition.Segment(DerivedStoreClass.PromptContextCache).ShouldBe("prompt-context-cache");
        DerivedStorePartition.Segment(DerivedStoreClass.CandidateRankingCache).ShouldBe("candidate-ranking-cache");
    }

    [Fact]
    public void SegmentThrowsOnAnUnknownDerivedStoreClass()
        => Should.Throw<ArgumentOutOfRangeException>(() => DerivedStorePartition.Segment((DerivedStoreClass)999));

    [Fact]
    public void PartitionPrefixIsDistinctPerClassForTheSameTenant()
    {
        string[] prefixes =
        [
            DerivedStorePartition.PartitionPrefix(DerivedStoreClass.VectorIndex, TenantAlpha),
            DerivedStorePartition.PartitionPrefix(DerivedStoreClass.EmbeddingStore, TenantAlpha),
            DerivedStorePartition.PartitionPrefix(DerivedStoreClass.PromptContextCache, TenantAlpha),
            DerivedStorePartition.PartitionPrefix(DerivedStoreClass.CandidateRankingCache, TenantAlpha),
        ];

        // One tenant, four classes → four distinct partition buckets that can never alias one another.
        prefixes.Distinct(StringComparer.Ordinal).Count().ShouldBe(4);
    }
}
