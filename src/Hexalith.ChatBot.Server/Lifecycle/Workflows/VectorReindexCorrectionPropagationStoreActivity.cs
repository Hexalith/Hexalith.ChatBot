using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Projections.DerivedStores;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>
/// The vector-reindex correction-propagation activity (Story 9.6, AC1) — wires the named
/// <c>ReindexVectors(tenantId, correctionId, sourceVersion)</c> M2 operation into the existing Story 2.8
/// correction-propagation coordinator seam as a real <see cref="ICorrectionPropagationStoreActivity"/> (it is NOT a
/// parallel coordinator). Its <see cref="StoreKey"/> is <see cref="CorrectionPropagationStoreKeys.VectorReindex"/>, so a
/// deployment that registers it runs the M2 scope (<see cref="CorrectionPropagationStoreKeys.RequiredM2"/>); the four
/// metadata-only M0 activities are unchanged.
/// <para>
/// <b>Affected-resource-id derivation (scoped tight).</b> <see cref="CorrectionPropagationActivityRequest"/> carries no
/// explicit resource-id list, so the affected resource id is derived deterministically from the correction identity —
/// the association id + the prior project id, consistent with how the prior association keyed its derived entries. The
/// richer "every entry that referenced the prior association" enumeration is left to the live M2 binding if it needs a
/// store-side index (it would require a cross-aggregate query this story deliberately does not invent).
/// </para>
/// </summary>
internal sealed class VectorReindexCorrectionPropagationStoreActivity(IVectorReindexer reindexer)
    : ICorrectionPropagationStoreActivity
{
    /// <summary>The reason code surfaced when a completed reindex exceeded the M2 SLO deadline (NFR17a, P2 incident).</summary>
    public const string SloExceededReasonCode = "vector_reindex_slo_exceeded";

    public string StoreKey => CorrectionPropagationStoreKeys.VectorReindex;

    public async ValueTask<CorrectionPropagationActivityResult> InvalidateAndRebuildAsync(
        CorrectionPropagationActivityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<string> affectedResourceIds = [AffectedResourceId(request)];

        VectorReindexOutcome outcome = await reindexer
            .ReindexVectorsAsync(
                request.TenantId,
                request.CorrectionId,
                request.SourceVersion,
                affectedResourceIds,
                request.StartedAtUtc,
                cancellationToken)
            .ConfigureAwait(false);

        // A hard failure wins; otherwise a completed-but-late reindex surfaces the SLO-exceeded reason so the
        // coordinator marks correction-delayed. A clean reindex (and an idempotent version-guard skip within SLO) is a
        // success — the idempotent no-op is NOT an error.
        string? reasonCode = outcome.FailureReasonCode
            ?? (outcome.SloBreached ? SloExceededReasonCode : null);

        return new CorrectionPropagationActivityResult(
            StoreKey,
            reasonCode is null ? "success" : "failed",
            reasonCode,
            outcome.CompletedAtUtc);
    }

    private static string AffectedResourceId(CorrectionPropagationActivityRequest request)
        => $"{request.AssociationId}:{request.PriorProjectId}";
}
