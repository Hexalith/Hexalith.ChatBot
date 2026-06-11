using Dapr.Workflow;

using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class CorrectionPropagationCompleteActivity(
    ICorrectionPropagationCommandWriter writer,
    ISystemClock clock,
    IChatBotMetrics? metrics = null)
    : WorkflowActivity<CorrectionPropagationRequest, bool>
{
    private readonly IChatBotMetrics _metrics = metrics ?? NullChatBotMetrics.Instance;

    public override async Task<bool> RunAsync(
        WorkflowActivityContext context,
        CorrectionPropagationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await writer.SubmitAsync(
            request,
            nameof(CompleteMailboxAssociationCorrectionPropagation),
            new CompleteMailboxAssociationCorrectionPropagation(
                request.AssociationId,
                request.CorrectionId,
                request.WorkflowInstanceId,
                request.SourceVersion,
                clock.UtcNow,
                CorrectionPropagationStatuses.Complete,
                DaprCorrectionPropagationCoordinator.SchemaVersion),
            CancellationToken.None)
            .ConfigureAwait(false);
        _metrics.RecordWorkflowLifecycle(
            request.TenantId,
            CorrectionPropagationWorkflowStatuses.Completed,
            CorrectionPropagationWorkflowFailureCodes.None);
        return true;
    }
}
