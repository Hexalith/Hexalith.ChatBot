using Dapr.Workflow;

using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class CorrectionPropagationCompleteActivity(
    ICorrectionPropagationCommandWriter writer,
    ISystemClock clock,
    IChatBotMetrics? metrics = null,
    ICorrectionPropagationWorkflowStatusSink? statusSink = null)
    : WorkflowActivity<CorrectionPropagationRequest, bool>
{
    private readonly IChatBotMetrics _metrics = metrics ?? NullChatBotMetrics.Instance;
    private readonly ICorrectionPropagationWorkflowStatusSink _statusSink =
        statusSink ?? NullCorrectionPropagationWorkflowStatusSink.Instance;

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
        await _statusSink
            .ReportAsync(
                request,
                CorrectionPropagationWorkflowStatuses.Completed,
                workflowRetryCount: 0,
                CorrectionPropagationWorkflowFailureCodes.None,
                CancellationToken.None)
            .ConfigureAwait(false);
        return true;
    }
}
