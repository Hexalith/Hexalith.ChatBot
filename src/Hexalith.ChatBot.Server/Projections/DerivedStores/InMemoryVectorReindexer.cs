using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;

namespace Hexalith.ChatBot.Server.Projections.DerivedStores;

/// <summary>
/// The in-memory, seam-first <see cref="IVectorReindexer"/> (Story 9.6, AC1/AC2) — the shippable
/// <c>ReindexVectors(tenantId, correctionId, sourceVersion)</c> default that operates through the Story 9.5
/// <see cref="IDerivedStore"/> tenant-partition seam. For each of the four <see cref="DerivedStorePartition.AllClasses"/>
/// it consults the single <see cref="IVectorReindexLedger"/> version guard: a re-delivered/older correction advances no
/// partition and is a no-op (<see cref="VectorReindexOutcome.VersionGuardSkipped"/>); otherwise it
/// <see cref="IDerivedStore.InvalidateAsync"/>s every affected resource id (structural removal — the prior association's
/// entries are physically gone) and rebuilds the corrected entries as metadata-only
/// <see cref="DerivedStoreEntry"/> values. The effective deadline and SLO-breach flag come from the define-once
/// <see cref="CorrectionPropagationSlo"/> (M2 = 60 min, NFR17a). A store/ledger throw is a fail-closed delay signal
/// (<see cref="VectorReindexFailedReasonCode"/>), never a silent success.
/// <para>
/// <b>Deferred-M2 (additive, not a rewrite).</b> The live Hexalith.Memories Redis-Vector/FalkorDB reindex binding, the
/// async/long-running reindex runtime (this in-memory reindex is synchronous), and the periodic SLO-deadline sweep are
/// deferred. The contract here is built so the live binding is an additive <see cref="IVectorReindexer"/> whose
/// partition is the Memories <c>IndexSchemaDefinitions</c> convention (see <see cref="DerivedStorePartition"/>).
/// </para>
/// </summary>
internal sealed class InMemoryVectorReindexer(
    IDerivedStore store,
    IVectorReindexLedger ledger,
    ISystemClock clock) : IVectorReindexer
{
    /// <summary>The fail-closed reason code emitted when the reindex throws (never a silent success).</summary>
    public const string VectorReindexFailedReasonCode = "vector_reindex_failed";

    public async ValueTask<VectorReindexOutcome> ReindexVectorsAsync(
        string tenantId,
        string correctionId,
        long sourceVersion,
        IReadOnlyList<string> affectedResourceIds,
        DateTimeOffset startedAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correctionId);
        ArgumentNullException.ThrowIfNull(affectedResourceIds);

        DateTimeOffset deadline = CorrectionPropagationSlo.DeadlineFor(CorrectionPropagationScope.M2, startedAtUtc);

        // A correction-stamped, metadata-only digest — DerivedStoreEntry.Create sanitizes it to a safe bounded token, so
        // a rebuilt entry can NEVER carry vector/embedding/prompt/candidate content.
        string rebuiltDigest = $"reindex:{correctionId}:{sourceVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

        int invalidated = 0;
        int rebuilt = 0;
        int advanced = 0;

        try
        {
            foreach (DerivedStoreClass cls in DerivedStorePartition.AllClasses)
            {
                // Single version-guard authority: an older/duplicate correction advances no partition (idempotent no-op).
                // ShouldReindex is a PURE read — the watermark is committed (TryAdvance) only AFTER the invalidate/rebuild
                // is applied, so a mid-reindex throw leaves the watermark un-advanced and a redelivery re-does this
                // partition rather than skipping it (FR91a/NFR9a: stale material must never survive a correction).
                if (!ledger.ShouldReindex(cls, tenantId, sourceVersion))
                {
                    continue;
                }

                advanced++;

                foreach (string resourceId in affectedResourceIds)
                {
                    // Invalidate means structural removal of the prior association's entry, not a filter flag.
                    if (await store.InvalidateAsync(cls, tenantId, resourceId, cancellationToken).ConfigureAwait(false))
                    {
                        invalidated++;
                    }

                    // Rebuild the corrected entry — metadata-only by construction.
                    await store
                        .PutAsync(cls, tenantId, resourceId, DerivedStoreEntry.Create(resourceId, rebuiltDigest), cancellationToken)
                        .ConfigureAwait(false);
                    rebuilt++;
                }

                // Commit the watermark only now that this partition's invalidate+rebuild has actually been applied.
                ledger.TryAdvance(cls, tenantId, sourceVersion);
            }
        }
        catch (Exception) when (cancellationToken is { IsCancellationRequested: false })
        {
            // Fail-closed: a reindex that cannot complete is a delay signal, never a silent partial success.
            DateTimeOffset failedAt = clock.UtcNow;
            return new VectorReindexOutcome(
                invalidated,
                rebuilt,
                VersionGuardSkipped: false,
                SloBreached: CorrectionPropagationSlo.IsBreached(deadline, failedAt),
                deadline,
                failedAt,
                VectorReindexFailedReasonCode);
        }

        DateTimeOffset completedAt = clock.UtcNow;
        return new VectorReindexOutcome(
            invalidated,
            rebuilt,
            VersionGuardSkipped: advanced == 0,
            SloBreached: CorrectionPropagationSlo.IsBreached(deadline, completedAt),
            deadline,
            completedAt,
            FailureReasonCode: null);
    }
}
