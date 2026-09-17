namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>
/// The single version-guard authority for vector reindexing (Story 9.6, AC1) — records the last-applied correction
/// <c>sourceVersion</c> per <see cref="DerivedStorePartition"/> key (tenant + derived-store class). It is the same
/// order-tolerant last-writer-wins idea the projection path already uses
/// (<c>GovernedOperationProjectionHandler</c>: <c>existing.SourceVersion &gt;= notification.SourceVersion ⇒ Ignored</c>):
/// a re-delivered or out-of-order correction whose <c>sourceVersion</c> is <b>≤</b> the last applied to a partition is a
/// no-op. There is exactly one such guard — never two drifting checks.
/// </summary>
internal interface IVectorReindexLedger
{
    /// <summary>
    /// Returns whether a correction at <paramref name="sourceVersion"/> still needs to be applied to the (class, tenant)
    /// partition — i.e. <paramref name="sourceVersion"/> is strictly greater than the last <b>committed</b> watermark. A
    /// <b>pure read</b> that does <b>not</b> mutate the watermark: the reindexer uses it to decide the version-guard skip
    /// <b>before</b> doing the invalidate/rebuild work, then commits with <see cref="TryAdvance"/> only once that work has
    /// actually been applied — so a mid-reindex failure leaves the watermark un-advanced and a redelivery re-does the
    /// partition rather than skipping it (FR91a/NFR9a: stale material must never survive a correction).
    /// </summary>
    /// <param name="cls">The derived-store class (part of the partition key).</param>
    /// <param name="tenantId">The owning tenant (part of the partition key).</param>
    /// <param name="sourceVersion">The candidate correction source version.</param>
    /// <returns><see langword="true"/> if the correction is newer than the committed watermark; <see langword="false"/> if it is stale/duplicate.</returns>
    bool ShouldReindex(DerivedStoreClass cls, string tenantId, long sourceVersion);

    /// <summary>
    /// Attempts to advance a partition's watermark to <paramref name="sourceVersion"/>. Returns <see langword="false"/>
    /// (skip — version-guard no-op) when <paramref name="sourceVersion"/> is less than or equal to the last applied for
    /// the (class, tenant) partition; otherwise records the new watermark and returns <see langword="true"/>. Call this
    /// only <b>after</b> the partition's invalidate/rebuild has been applied (it is the durable commit of the watermark).
    /// </summary>
    /// <param name="cls">The derived-store class (part of the partition key).</param>
    /// <param name="tenantId">The owning tenant (part of the partition key).</param>
    /// <param name="sourceVersion">The candidate correction source version.</param>
    /// <returns><see langword="true"/> if the watermark advanced; <see langword="false"/> if the correction is stale/duplicate.</returns>
    bool TryAdvance(DerivedStoreClass cls, string tenantId, long sourceVersion);
}
