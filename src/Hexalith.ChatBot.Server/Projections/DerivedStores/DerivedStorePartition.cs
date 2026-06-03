using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>The four derived-store classes FR55a/NFR9a names — each owns a distinct tenant-partition segment.</summary>
internal enum DerivedStoreClass
{
    /// <summary>A tenant's vector/similarity index (the M2 Redis-Vector binding's partition).</summary>
    VectorIndex,

    /// <summary>A tenant's embedding store.</summary>
    EmbeddingStore,

    /// <summary>A tenant's prompt-context cache.</summary>
    PromptContextCache,

    /// <summary>A tenant's candidate-ranking cache.</summary>
    CandidateRankingCache,
}

/// <summary>
/// The single authoritative tenant-partition contract for the four derived-store classes (Story 9.5, AC1, FR55a/NFR9a,
/// cross-cutting define-once). Isolation is <b>physical partitioning at the store layer</b>, never an application-side
/// <c>WHERE tenantId = …</c> filter: every derived-store key is tenant-prefixed by construction so a cross-tenant read
/// is a key miss at the storage layer. This mirrors the proven projection-key convention
/// <c><see cref="GovernedOperationView.KeyFor"/></c> ⇒ <c>{tenant}:governed-operation:{noteId}</c> exactly — the tenant
/// id is always first — and is the same idea applied once across all four derived-store classes (the Story 9.4
/// <c>ReplayTenantPolicy</c> define-once lesson: one helper, consumed everywhere a derived-store key is built
/// <b>and</b> by the isolation probe sweep, never a second drifting <c>{tenant}:</c> scheme).
/// <para>
/// <b>Shape.</b> <see cref="KeyFor"/> ⇒ <c>{tenantId}:{derived-class}:{resourceId}</c>;
/// <see cref="PartitionPrefix"/> ⇒ <c>{tenantId}:{derived-class}:</c>. Each <see cref="DerivedStoreClass"/> maps to a
/// stable, distinct segment token (<c>vector-index</c>, <c>embedding-store</c>, <c>prompt-context-cache</c>,
/// <c>candidate-ranking-cache</c>), so the same logical resource id under two tenants — or two classes — is never the
/// same key.
/// </para>
/// <para>
/// <b>Fail-closed (Epic 8/9 no-fabrication doctrine).</b> The tenant id must be an
/// <see cref="AuditMetadata.IsSafeStableIdentifier"/>-safe bounded token before it can scope a partition; an empty,
/// whitespace, or unsafe tenant id resolves <b>no</b> partition (throws <see cref="ArgumentException"/>) — never a
/// shared or global key. The resource id is validated the same way so a malformed token can never smuggle content into
/// a key.
/// </para>
/// <para>
/// <b>M2 live target (documented, not duplicated).</b> The deferred live Hexalith.Memories Redis-Vector / FalkorDB
/// binding adopts <b>this</b> contract: its tenant-scoped index/key convention is
/// <c>Hexalith.Memories.Server.Infrastructure.IndexSchemaDefinitions</c> — semantic index name
/// <c>{tenantId}:memories:vec</c>, natural-language index <c>{tenantId}:memories:vec:nl</c>, key prefixes
/// <c>{tenantId}:vec:</c> and <c>{tenantId}:mu:</c> — all tenant-prefixed-by-construction. The M2 binding is an
/// <b>additive</b> <see cref="IDerivedStore"/> implementation whose partition is this contract (mapped onto
/// <c>IndexSchemaDefinitions</c>), not a new prefix scheme invented later.
/// </para>
/// </summary>
internal static class DerivedStorePartition
{
    /// <summary>The stable partition segment for the vector-index class.</summary>
    public const string VectorIndexSegment = "vector-index";

    /// <summary>The stable partition segment for the embedding-store class.</summary>
    public const string EmbeddingStoreSegment = "embedding-store";

    /// <summary>The stable partition segment for the prompt-context-cache class.</summary>
    public const string PromptContextCacheSegment = "prompt-context-cache";

    /// <summary>The stable partition segment for the candidate-ranking-cache class.</summary>
    public const string CandidateRankingCacheSegment = "candidate-ranking-cache";

    /// <summary>The four derived-store classes, in declaration order — the set a probe sweep iterates.</summary>
    public static readonly IReadOnlyList<DerivedStoreClass> AllClasses =
    [
        DerivedStoreClass.VectorIndex,
        DerivedStoreClass.EmbeddingStore,
        DerivedStoreClass.PromptContextCache,
        DerivedStoreClass.CandidateRankingCache,
    ];

    /// <summary>
    /// Returns the stable, distinct partition segment token for a derived-store class. The token is part of every key
    /// and index name, so it is a fixed wire constant, never a renamed enum-name reflection.
    /// </summary>
    /// <param name="cls">The derived-store class.</param>
    /// <returns>The partition segment token.</returns>
    public static string Segment(DerivedStoreClass cls)
        => cls switch
        {
            DerivedStoreClass.VectorIndex => VectorIndexSegment,
            DerivedStoreClass.EmbeddingStore => EmbeddingStoreSegment,
            DerivedStoreClass.PromptContextCache => PromptContextCacheSegment,
            DerivedStoreClass.CandidateRankingCache => CandidateRankingCacheSegment,
            _ => throw new ArgumentOutOfRangeException(nameof(cls), cls, "Unknown derived-store class."),
        };

    /// <summary>
    /// Builds the tenant-partitioned key for a derived-store resource ⇒ <c>{tenantId}:{derived-class}:{resourceId}</c>.
    /// The tenant id is always first so a foreign-tenant read is a key miss at the store layer (NFR9a). Fails closed: an
    /// empty/unsafe tenant id or resource id throws — no shared/global key is ever produced.
    /// </summary>
    /// <param name="cls">The derived-store class.</param>
    /// <param name="tenantId">The owning tenant (validated safe-token, always first).</param>
    /// <param name="resourceId">The logical resource id within the partition (validated safe-token).</param>
    /// <returns>The tenant-partitioned key.</returns>
    public static string KeyFor(DerivedStoreClass cls, string tenantId, string resourceId)
    {
        RequireSafeTenant(tenantId);
        RequireSafeResource(resourceId);
        return $"{tenantId}:{Segment(cls)}:{resourceId}";
    }

    /// <summary>
    /// Builds the tenant-partition prefix that owns every key of a class for a tenant ⇒ <c>{tenantId}:{derived-class}:</c>.
    /// This is the partition a store buckets by, so a read under one tenant physically cannot reach another's bucket.
    /// Fails closed on an empty/unsafe tenant id.
    /// </summary>
    /// <param name="cls">The derived-store class.</param>
    /// <param name="tenantId">The owning tenant (validated safe-token, always first).</param>
    /// <returns>The tenant-partition prefix.</returns>
    public static string PartitionPrefix(DerivedStoreClass cls, string tenantId)
    {
        RequireSafeTenant(tenantId);
        return $"{tenantId}:{Segment(cls)}:";
    }

    private static void RequireSafeTenant(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (!AuditMetadata.IsSafeStableIdentifier(tenantId))
        {
            throw new ArgumentException(
                "An unsafe tenant id resolves no derived-store partition (fail-closed); it must be a safe bounded token.",
                nameof(tenantId));
        }
    }

    private static void RequireSafeResource(string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        if (!AuditMetadata.IsSafeStableIdentifier(resourceId))
        {
            throw new ArgumentException(
                "An unsafe resource id cannot scope a derived-store key (fail-closed); it must be a safe bounded token.",
                nameof(resourceId));
        }
    }
}
