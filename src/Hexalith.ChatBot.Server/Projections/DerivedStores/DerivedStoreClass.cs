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
