using Dapr.Workflow;

using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class CorrectionPropagationRunStoreActivity(
    ICorrectionPropagationActivityCatalog activityCatalog,
    ICorrectionPropagationCommandWriter writer,
    IChatBotMetrics? metrics = null)
    : WorkflowActivity<CorrectionPropagationStoreActivityInput, CorrectionPropagationActivityResult>
{
    private readonly IChatBotMetrics _metrics = metrics ?? NullChatBotMetrics.Instance;

    public override async Task<CorrectionPropagationActivityResult> RunAsync(
        WorkflowActivityContext context,
        CorrectionPropagationStoreActivityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        CorrectionPropagationRequest request = input.Request;
        CorrectionPropagationActivityResult result;

        if (!activityCatalog.TryGet(input.StoreKey, out ICorrectionPropagationStoreActivity? storeActivity))
        {
            result = new CorrectionPropagationActivityResult(
                input.StoreKey,
                "failed",
                CorrectionPropagationWorkflowFailureCodes.StoreUnavailable,
                input.StartedAtUtc);
        }
        else
        {
            CorrectionPropagationActivityRequest activityRequest = new(
                request.TenantId,
                request.AssociationId,
                request.CorrectionId,
                request.WorkflowInstanceId,
                input.StoreKey,
                request.SourceVersion,
                request.PriorProjectId,
                request.CorrectedProjectId,
                input.StartedAtUtc,
                request.CorrelationId);
            result = await storeActivity
                .InvalidateAndRebuildAsync(activityRequest, CancellationToken.None)
                .ConfigureAwait(false);
        }

        await writer.SubmitAsync(
            request,
            nameof(AcknowledgeMailboxAssociationCorrectionStoreInvalidated),
            new AcknowledgeMailboxAssociationCorrectionStoreInvalidated(
                request.AssociationId,
                request.CorrectionId,
                request.WorkflowInstanceId,
                input.StoreKey,
                request.SourceVersion,
                request.PriorProjectId,
                request.CorrectedProjectId,
                input.StartedAtUtc,
                result.CompletedAtUtc,
                result.Outcome,
                result.FailureReasonCode,
                "metadata_only",
                "collaboration_input",
                DaprCorrectionPropagationCoordinator.SchemaVersion),
            CancellationToken.None)
            .ConfigureAwait(false);

        _metrics.RecordWorkflowLifecycle(
            request.TenantId,
            result.IsSuccessful ? "store-completed" : CorrectionPropagationWorkflowStatuses.Failed,
            result.FailureReasonCode ?? CorrectionPropagationWorkflowFailureCodes.None);

        return result;
    }
}
