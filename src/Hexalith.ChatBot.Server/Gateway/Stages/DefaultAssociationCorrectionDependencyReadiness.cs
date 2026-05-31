using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class DefaultAssociationCorrectionDependencyReadiness(
    ICorrectionPropagationCoordinator propagationCoordinator,
    IAssociationProjectionStore associationProjectionStore,
    IGovernedOperationProjectionStore governedOperationProjectionStore,
    IOperationStatusStore operationStatusStore,
    IAuditWriter auditWriter,
    IIdempotencyStore idempotencyStore) : IAssociationCorrectionDependencyReadiness
{
    public AssociationCorrectionDependencyReadinessStatus Status
    {
        get
        {
            bool projectionReady = associationProjectionStore is not null &&
                governedOperationProjectionStore is not null &&
                operationStatusStore is not null;

            return new AssociationCorrectionDependencyReadinessStatus(
                propagationCoordinator.IsReady,
                projectionReady,
                auditWriter is not null,
                idempotencyStore is not null);
        }
    }

    public bool IsProjectionInvalidationReady => Status.IsReady;
}
