using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class ProjectionCorrectedContextReadinessPolicy(
    IAssociationProjectionStore associationProjectionStore) : ICorrectedContextReadinessPolicy
{
    public async ValueTask<CorrectedContextReadiness> EvaluateAsync(
        string tenantId,
        string associationId,
        long sourceVersion,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(associationId);

        AssociationCandidateView? view = await associationProjectionStore
            .GetAsync(tenantId, associationId, cancellationToken)
            .ConfigureAwait(false);
        if (view is null || view.SourceVersion < sourceVersion)
        {
            return new CorrectedContextReadiness(
                false,
                CorrectionPropagationStatuses.Pending,
                "association_correction_stale_source_version",
                CorrectionPropagationStoreKeys.RequiredM0);
        }

        if (!string.Equals(view.DownstreamImpactStatus, CorrectionPropagationStatuses.Complete, StringComparison.Ordinal) ||
            view.IsCorrectedContextStale)
        {
            string[] completed = view.CompletedStoreKeys?.ToArray() ?? [];
            string[] pending = (view.RequiredStoreKeys ?? CorrectionPropagationStoreKeys.RequiredM0)
                .Where(storeKey => !completed.Contains(storeKey, StringComparer.Ordinal))
                .ToArray();
            return new CorrectedContextReadiness(
                false,
                view.PropagationStatus ?? CorrectionPropagationStatuses.Correcting,
                "association_ai_context_blocked",
                pending);
        }

        return new CorrectedContextReadiness(true, CorrectionPropagationStatuses.Complete, "none", []);
    }
}
