using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

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
