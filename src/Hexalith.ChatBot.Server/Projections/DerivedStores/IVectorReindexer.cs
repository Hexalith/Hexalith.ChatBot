namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>
/// The metadata-only outcome of a <see cref="IVectorReindexer.ReindexVectorsAsync"/> call (Story 9.6, AC1/AC2). Carries
/// <b>only</b> safe counts and flags — never vector floats, embedding values, prompt text, or candidate payloads
/// (NFR2/NFR42 no-leak floor).
/// </summary>
/// <param name="EntriesInvalidated">How many previously-present entries were structurally removed across the four derived-store classes.</param>
/// <param name="EntriesRebuilt">How many corrected entries were rebuilt across the four derived-store classes.</param>
/// <param name="VersionGuardSkipped">True when the entire reindex was a version-guard no-op (a re-delivered/older correction advanced no partition).</param>
/// <param name="SloBreached">True when the reindex completed after its computed M2 deadline (NFR17a, surfaces correction-delayed).</param>
/// <param name="DeadlineUtc">The effective M2 completion deadline (started-at + 60 min), from <c>CorrectionPropagationSlo</c>.</param>
/// <param name="CompletedAtUtc">When the reindex finished.</param>
/// <param name="FailureReasonCode">A safe reason code when the reindex failed (e.g. <c>vector_reindex_failed</c>), or null on success.</param>
internal sealed record VectorReindexOutcome(
    int EntriesInvalidated,
    int EntriesRebuilt,
    bool VersionGuardSkipped,
    bool SloBreached,
    DateTimeOffset DeadlineUtc,
    DateTimeOffset CompletedAtUtc,
    string? FailureReasonCode);

/// <summary>
/// The named <c>ReindexVectors(tenantId, correctionId, sourceVersion)</c> M2 correction-propagation operation
/// (architecture.md:190/399, FR91a, NFR9a) — invalidates and rebuilds the derived-store entries (vector index,
/// embedding store, prompt-context cache, candidate-ranking cache) that referenced the <b>prior</b> association, so M2
/// derived stores never serve stale/misassigned material. It operates <b>through</b> the Story 9.5
/// <see cref="IDerivedStore"/> tenant-partition seam (it is not a second derived-store abstraction) and is idempotent +
/// version-guarded (order-tolerant last-writer-wins): a re-delivered or out-of-order correction is a no-op.
/// <para>
/// The in-memory default (<see cref="InMemoryVectorReindexer"/>) is the shippable seam-first deliverable; the live
/// Hexalith.Memories Redis-Vector/FalkorDB reindex binding is the deferred-M2 wiring, additive behind this same seam (a
/// Memories-backed <see cref="IVectorReindexer"/> whose partition is the Memories <c>IndexSchemaDefinitions</c>
/// convention), never a rewrite.
/// </para>
/// </summary>
internal interface IVectorReindexer
{
    /// <summary>
    /// Invalidates and rebuilds the affected derived-store entries for a correction, idempotently and version-guarded.
    /// </summary>
    /// <param name="tenantId">The owning tenant (every partition is scoped here).</param>
    /// <param name="correctionId">The correction identity (stamped into the rebuilt entries' metadata-only digest).</param>
    /// <param name="sourceVersion">The correction's source version — the version-guard watermark (last-writer-wins).</param>
    /// <param name="affectedResourceIds">The resource ids referenced by the prior association to invalidate and rebuild.</param>
    /// <param name="startedAtUtc">When the correction propagation started (the SLO deadline base).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata-only reindex outcome.</returns>
    ValueTask<VectorReindexOutcome> ReindexVectorsAsync(
        string tenantId,
        string correctionId,
        long sourceVersion,
        IReadOnlyList<string> affectedResourceIds,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken);
}
