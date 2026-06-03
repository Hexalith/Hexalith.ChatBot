using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>
/// A metadata-only derived-store entry (Story 9.5, AC1, NFR2/NFR42 no-leak floor). Derived stores hold the system's most
/// sensitive material (embeddings, prompt context, candidate payloads); a <see cref="DerivedStoreEntry"/> carries
/// <b>only</b> safe bounded tokens — a safe <see cref="ResourceId"/> and a bounded <see cref="ContentDigest"/>/sentinel
/// token — <b>never</b> raw vector floats, embedding values, prompt text, or candidate payloads. Every field is reduced
/// to an <see cref="AuditMetadata"/>-safe token via <see cref="Create"/>, so a malformed token can never smuggle content
/// into the store (mirrors <c>OutboundTraceRecord.FromRequest</c>).
/// </summary>
/// <param name="ResourceId">The safe logical resource id this entry is keyed by.</param>
/// <param name="ContentDigest">A bounded metadata-only digest/sentinel token standing in for the (absent) content.</param>
internal sealed record DerivedStoreEntry(string ResourceId, string ContentDigest)
{
    private const string SafeFallback = "redacted-ref";

    /// <summary>Builds a metadata-only entry, sanitizing both fields to safe bounded tokens.</summary>
    /// <param name="resourceId">The logical resource id.</param>
    /// <param name="contentDigest">A metadata-only digest/sentinel token (never raw content).</param>
    /// <returns>The sanitized entry.</returns>
    public static DerivedStoreEntry Create(string resourceId, string? contentDigest)
        => new(Safe(resourceId), Safe(contentDigest));

    private static string Safe(string? value) => AuditMetadata.SafeOptionalToken(value) ?? SafeFallback;
}

/// <summary>
/// The tenant-partitioned derived-store access seam (Story 9.5, AC1, FR55a/NFR9a). Isolation lives <b>here, below the
/// application layer</b>: every method takes <c>tenantId</c> first and the implementation builds the
/// <see cref="DerivedStorePartition"/> partition before touching data, so a read under one tenant can never observe
/// another tenant's records — there is no shared collection a caller filters. Mirrors the
/// <see cref="Adapters.Mailbox.IOutboundTraceStore"/> / <c>IGovernedOperationProjectionStore</c> tenant-first shape.
/// <see cref="EnumerateTenants"/> lets the nightly cross-tenant isolation probe sweep every partition.
/// <para>
/// The in-memory default (<see cref="InMemoryDerivedStore"/>) is the shippable seam-first deliverable; the deferred M2
/// live binding is an additive implementation of this same interface whose partition is the Hexalith.Memories
/// <c>IndexSchemaDefinitions</c> convention (see <see cref="DerivedStorePartition"/>).
/// </para>
/// </summary>
internal interface IDerivedStore
{
    /// <summary>Writes (or overwrites) a metadata-only entry into the tenant's partition for a derived-store class.</summary>
    /// <param name="cls">The derived-store class.</param>
    /// <param name="tenantId">The owning tenant (the partition the write is scoped to).</param>
    /// <param name="resourceId">The logical resource id within the partition.</param>
    /// <param name="entry">The metadata-only entry to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task once the entry is stored.</returns>
    ValueTask PutAsync(DerivedStoreClass cls, string tenantId, string resourceId, DerivedStoreEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Reads a single entry from the tenant's partition. A foreign/unknown tenant — or an unknown resource id within the
    /// tenant — yields <see langword="null"/> (a safe not-found that never confirms another tenant's resource exists).
    /// </summary>
    /// <param name="cls">The derived-store class.</param>
    /// <param name="tenantId">The reading tenant (the partition the read is scoped to).</param>
    /// <param name="resourceId">The logical resource id within the partition.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entry, or <see langword="null"/> if it is not in this tenant's partition.</returns>
    ValueTask<DerivedStoreEntry?> GetAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken);

    /// <summary>Returns the resource ids in a tenant's partition for a class, in store order. A foreign tenant yields empty.</summary>
    /// <param name="cls">The derived-store class.</param>
    /// <param name="tenantId">The tenant whose partition to enumerate.</param>
    /// <returns>The resource ids the tenant owns for the class.</returns>
    IReadOnlyList<string> EnumerateResourceIds(DerivedStoreClass cls, string tenantId);

    /// <summary>Returns the tenant refs that currently hold any derived-store entry, so the isolation probe can sweep per tenant pair.</summary>
    /// <returns>The tenants with at least one partition.</returns>
    IReadOnlyList<string> EnumerateTenants();

    /// <summary>
    /// Invalidates (structurally removes) a single entry from the tenant's partition for a derived-store class — the
    /// Story 9.6 deliverable closing the Story 9.5 Senior Review follow-up (the 9.5 seam had Put/Get/Enumerate but
    /// <b>no delete op</b>, so a stale/misassigned derived entry could only be hidden, never physically removed).
    /// <c>ReindexVectors</c>-driven correction propagation relies on this to make "invalidate" mean structural
    /// removal, <b>not</b> a filter flag the read side could forget to apply (FR91a/NFR9a): after invalidation, a
    /// <see cref="GetAsync"/> for the same resource yields the safe not-found.
    /// <para>
    /// Tenant-first and fail-closed, exactly like <see cref="GetAsync"/>: the tenant partition is built via
    /// <see cref="DerivedStorePartition"/> (an empty/unsafe tenant or resource id throws — never a shared/global key), so
    /// a foreign/unknown tenant resolves only its own subtree and can never remove another tenant's same-id entry.
    /// Idempotent: re-invalidating an absent entry is a no-op that returns <see langword="false"/>.
    /// </para>
    /// </summary>
    /// <param name="cls">The derived-store class.</param>
    /// <param name="tenantId">The owning tenant (the partition the removal is scoped to).</param>
    /// <param name="resourceId">The logical resource id within the partition.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> if an entry was present and removed; <see langword="false"/> otherwise (idempotent re-invalidate).</returns>
    ValueTask<bool> InvalidateAsync(DerivedStoreClass cls, string tenantId, string resourceId, CancellationToken cancellationToken);
}

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
