using System.Collections.Concurrent;

using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1.DerivedStores;

namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>
/// Production diagnostic-probe adapter over the supported Memories REST client. These four categories are confined to
/// Memories' metadata-only diagnostic namespace and never represent canonical memory-unit, vector, or graph records.
/// </summary>
internal sealed class MemoriesDerivedStore(MemoriesClient client) : IDerivedStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<DerivedStoreClass, ConcurrentDictionary<string, byte>>> _known =
        new(StringComparer.Ordinal);

    public async ValueTask PutAsync(
        DerivedStoreClass cls,
        string tenantId,
        string resourceId,
        DerivedStoreEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _ = DerivedStorePartition.KeyFor(cls, tenantId, resourceId);
        await client
            .PutDiagnosticEntryAsync(
                tenantId,
                Map(cls),
                resourceId,
                new DiagnosticStoreEntry(resourceId, entry.ContentDigest),
                cancellationToken)
            .ConfigureAwait(false);
        KnownResources(tenantId, cls)[resourceId] = 0;
    }

    public async ValueTask<DerivedStoreEntry?> GetAsync(
        DerivedStoreClass cls,
        string tenantId,
        string resourceId,
        CancellationToken cancellationToken)
    {
        _ = DerivedStorePartition.KeyFor(cls, tenantId, resourceId);
        DiagnosticStoreEntry? entry = await client
            .GetDiagnosticEntryAsync(tenantId, Map(cls), resourceId, cancellationToken)
            .ConfigureAwait(false);
        return entry is null ? null : DerivedStoreEntry.Create(entry.ResourceId, entry.ContentDigest);
    }

    public IReadOnlyList<string> EnumerateResourceIds(DerivedStoreClass cls, string tenantId)
    {
        _ = DerivedStorePartition.PartitionPrefix(cls, tenantId);
        return _known.TryGetValue(tenantId, out ConcurrentDictionary<DerivedStoreClass, ConcurrentDictionary<string, byte>>? classes)
            && classes.TryGetValue(cls, out ConcurrentDictionary<string, byte>? resources)
                ? [.. resources.Keys.Order(StringComparer.Ordinal)]
                : [];
    }

    public async ValueTask<IReadOnlyList<string>> EnumerateResourceIdsAsync(
        DerivedStoreClass cls,
        string tenantId,
        CancellationToken cancellationToken)
    {
        _ = DerivedStorePartition.PartitionPrefix(cls, tenantId);
        IReadOnlyList<DiagnosticStoreEntry> entries = await client
            .ListDiagnosticEntriesAsync(tenantId, Map(cls), cancellationToken)
            .ConfigureAwait(false);
        ConcurrentDictionary<string, byte> known = KnownResources(tenantId, cls);
        known.Clear();
        foreach (DiagnosticStoreEntry entry in entries)
        {
            known[entry.ResourceId] = 0;
        }

        return entries.Select(static entry => entry.ResourceId).ToArray();
    }

    public IReadOnlyList<string> EnumerateTenants() => [.. _known.Keys.Order(StringComparer.Ordinal)];

    public async ValueTask<bool> InvalidateAsync(
        DerivedStoreClass cls,
        string tenantId,
        string resourceId,
        CancellationToken cancellationToken)
    {
        _ = DerivedStorePartition.KeyFor(cls, tenantId, resourceId);
        bool deleted = await client
            .DeleteDiagnosticEntryAsync(tenantId, Map(cls), resourceId, cancellationToken)
            .ConfigureAwait(false);
        if (_known.TryGetValue(tenantId, out ConcurrentDictionary<DerivedStoreClass, ConcurrentDictionary<string, byte>>? classes)
            && classes.TryGetValue(cls, out ConcurrentDictionary<string, byte>? resources))
        {
            _ = resources.TryRemove(resourceId, out _);
        }

        return deleted;
    }

    private ConcurrentDictionary<string, byte> KnownResources(string tenantId, DerivedStoreClass cls)
        => _known
            .GetOrAdd(
                tenantId,
                static _ => new ConcurrentDictionary<DerivedStoreClass, ConcurrentDictionary<string, byte>>())
            .GetOrAdd(cls, static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));

    private static DiagnosticStoreClass Map(DerivedStoreClass cls)
        => cls switch
        {
            DerivedStoreClass.VectorIndex => DiagnosticStoreClass.VectorIndex,
            DerivedStoreClass.EmbeddingStore => DiagnosticStoreClass.EmbeddingStore,
            DerivedStoreClass.PromptContextCache => DiagnosticStoreClass.PromptContextCache,
            DerivedStoreClass.CandidateRankingCache => DiagnosticStoreClass.CandidateRankingCache,
            _ => throw new ArgumentOutOfRangeException(nameof(cls), cls, "Unknown diagnostic derived-store class."),
        };
}
