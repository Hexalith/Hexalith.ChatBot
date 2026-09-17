using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>
/// In-process, tenant-partitioned <see cref="IDerivedStore"/> — the seam-first M0 test/dev default (Story 9.5, AC1),
/// mirroring <c>InMemoryOutboundTraceStore</c>. Storage is nested tenant-first: a lock-guarded
/// <c>Dictionary&lt;tenant, Dictionary&lt;partition-prefix, Dictionary&lt;resourceId, entry&gt;&gt;&gt;</c>. Isolation is
/// <b>structural, not filtered</b>: a read under tenant B starts at B's own subtree (keyed by B's id) and builds B's
/// partition prefix via <see cref="DerivedStorePartition.PartitionPrefix"/>, so it physically cannot reach tenant A's
/// subtree — there is no shared collection scanned with a <c>WHERE tenant = …</c> predicate. The production swap is a
/// durable tenant-partitioned store (the M2 Redis-Vector/FalkorDB binding) behind this same interface.
/// </summary>
internal sealed class InMemoryDerivedStore : IDerivedStore
{
    private readonly Lock _gate = new();

    // tenantId -> partitionPrefix ({tenant}:{class}:) -> resourceId -> entry. Tenant-first nesting makes a cross-tenant
    // read structurally impossible: a tenant only ever indexes into its own subtree.
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, DerivedStoreEntry>>> _byTenant =
        new(StringComparer.Ordinal);

    public ValueTask PutAsync(DerivedStoreClass cls, string tenantId, string resourceId, DerivedStoreEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        // PartitionPrefix validates the tenant id (fail-closed); KeyFor validates the resource id.
        string prefix = DerivedStorePartition.PartitionPrefix(cls, tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_byTenant.TryGetValue(tenantId, out Dictionary<string, Dictionary<string, DerivedStoreEntry>>? partitions))
            {
                partitions = new Dictionary<string, Dictionary<string, DerivedStoreEntry>>(StringComparer.Ordinal);
                _byTenant[tenantId] = partitions;
            }

            if (!partitions.TryGetValue(prefix, out Dictionary<string, DerivedStoreEntry>? partition))
            {
                partition = new Dictionary<string, DerivedStoreEntry>(StringComparer.Ordinal);
                partitions[prefix] = partition;
            }

            partition[resourceId] = entry;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<DerivedStoreEntry?> GetAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
    {
        string prefix = DerivedStorePartition.PartitionPrefix(cls, tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_byTenant.TryGetValue(tenantId, out Dictionary<string, Dictionary<string, DerivedStoreEntry>>? partitions)
                && partitions.TryGetValue(prefix, out Dictionary<string, DerivedStoreEntry>? partition)
                && partition.TryGetValue(resourceId, out DerivedStoreEntry? entry))
            {
                return ValueTask.FromResult<DerivedStoreEntry?>(entry);
            }
        }

        // Safe not-found: never confirms another tenant's resource exists across the boundary.
        return ValueTask.FromResult<DerivedStoreEntry?>(null);
    }

    public IReadOnlyList<string> EnumerateResourceIds(DerivedStoreClass cls, string tenantId)
    {
        string prefix = DerivedStorePartition.PartitionPrefix(cls, tenantId);
        lock (_gate)
        {
            return _byTenant.TryGetValue(tenantId, out Dictionary<string, Dictionary<string, DerivedStoreEntry>>? partitions)
                && partitions.TryGetValue(prefix, out Dictionary<string, DerivedStoreEntry>? partition)
                ? [.. partition.Keys]
                : [];
        }
    }

    public IReadOnlyList<string> EnumerateTenants()
    {
        lock (_gate)
        {
            return [.. _byTenant.Keys];
        }
    }

    public ValueTask<bool> InvalidateAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken)
    {
        // PartitionPrefix validates the tenant id (fail-closed); the resource id is validated the same way GetAsync does.
        string prefix = DerivedStorePartition.PartitionPrefix(cls, tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            // Tenant-first navigation: a foreign/unknown tenant only ever indexes into its OWN subtree, so a removal can
            // never reach another tenant's partition. The removal is structural — the resource id is gone from the
            // innermost dictionary, not merely flagged — so a subsequent GetAsync is a real key miss.
            if (_byTenant.TryGetValue(tenantId, out Dictionary<string, Dictionary<string, DerivedStoreEntry>>? partitions)
                && partitions.TryGetValue(prefix, out Dictionary<string, DerivedStoreEntry>? partition))
            {
                return ValueTask.FromResult(partition.Remove(resourceId));
            }
        }

        // Idempotent re-invalidate / foreign tenant: nothing was present to remove.
        return ValueTask.FromResult(false);
    }
}
