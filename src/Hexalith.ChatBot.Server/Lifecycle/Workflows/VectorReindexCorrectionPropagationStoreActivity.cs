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
/// The live canonical adapter receives the governed association/intake and Projects-resolved corrected case as distinct
/// identities. The legacy affected-resource list remains only for the deterministic in-memory development adapter.
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

        VectorReindexOutcome outcome = reindexer is ICanonicalVectorReindexer canonical
            ? await canonical
                .ReindexCanonicalVectorsAsync(
                    request.TenantId,
                    request.AssociationId,
                    request.IntakeId,
                    request.CorrectionId,
                    request.SourceVersion,
                    request.CorrectedCaseId,
                    request.RemoteOperationId,
                    request.StartedAtUtc,
                    cancellationToken)
                .ConfigureAwait(false)
            : await reindexer
                .ReindexVectorsAsync(
                    request.TenantId,
                    request.CorrectionId,
                    request.SourceVersion,
                    [request.AssociationId],
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
            !outcome.IsTerminal ? "awaiting-completion" : reasonCode is null ? "success" : "failed",
            reasonCode,
            outcome.CompletedAtUtc,
            outcome.RemoteOperationId);
    }
}
