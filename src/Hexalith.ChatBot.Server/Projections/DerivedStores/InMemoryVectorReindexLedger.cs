namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>
/// In-process, tenant-partitioned <see cref="IVectorReindexLedger"/> — the seam-first M0 default mirroring
/// <see cref="InMemoryDerivedStore"/>'s lock-guarded, tenant-first nesting. The production swap is a durable
/// tenant-partitioned ledger behind this same interface (alongside the M2 live reindex binding).
/// </summary>
internal sealed class InMemoryVectorReindexLedger : IVectorReindexLedger
{
    private readonly Lock _gate = new();

    // tenantId -> partition prefix ({tenant}:{class}:) -> last-applied source version. Tenant-first nesting keeps a
    // foreign tenant indexing only into its own subtree (it can never read or advance another tenant's watermark).
    private readonly Dictionary<string, Dictionary<string, long>> _byTenant = new(StringComparer.Ordinal);

    public bool ShouldReindex(DerivedStoreClass cls, string tenantId, long sourceVersion)
    {
        // PartitionPrefix validates the tenant id (fail-closed) and yields the single-source partition key.
        string prefix = DerivedStorePartition.PartitionPrefix(cls, tenantId);

        lock (_gate)
        {
            // Pure read — never creates or mutates a partition entry: a redelivery whose work failed last time must still
            // be seen as "needs reindex" until TryAdvance durably commits the watermark.
            return !(_byTenant.TryGetValue(tenantId, out Dictionary<string, long>? partitions)
                && partitions.TryGetValue(prefix, out long lastApplied)
                && sourceVersion <= lastApplied);
        }
    }

    public bool TryAdvance(DerivedStoreClass cls, string tenantId, long sourceVersion)
    {
        // PartitionPrefix validates the tenant id (fail-closed) and yields the single-source partition key.
        string prefix = DerivedStorePartition.PartitionPrefix(cls, tenantId);

        lock (_gate)
        {
            if (!_byTenant.TryGetValue(tenantId, out Dictionary<string, long>? partitions))
            {
                partitions = new Dictionary<string, long>(StringComparer.Ordinal);
                _byTenant[tenantId] = partitions;
            }

            if (partitions.TryGetValue(prefix, out long lastApplied) && sourceVersion <= lastApplied)
            {
                // Re-delivered or out-of-order correction — last-writer-wins keeps the watermark, no-op.
                return false;
            }

            partitions[prefix] = sourceVersion;
            return true;
        }
    }
}
