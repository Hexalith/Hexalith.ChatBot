using Dapr.Workflow;

using Hexalith.ChatBot.Server.Association;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class CorrectionPropagationStartActivity(
    ICorrectionPropagationCommandWriter writer,
    ICorrectionPropagationWorkflowStatusSink? statusSink = null)
    : WorkflowActivity<CorrectionPropagationStartInput, bool>
{
    private readonly ICorrectionPropagationWorkflowStatusSink _statusSink =
        statusSink ?? NullCorrectionPropagationWorkflowStatusSink.Instance;

    public override async Task<bool> RunAsync(
        WorkflowActivityContext context,
        CorrectionPropagationStartInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Request);
        CorrectionPropagationRequest request = input.Request;

        await writer.SubmitAsync(
            request,
            nameof(StartMailboxAssociationCorrectionPropagation),
            new StartMailboxAssociationCorrectionPropagation(
                request.AssociationId,
                request.IntakeId,
                request.CorrectionId,
                request.WorkflowInstanceId,
                request.PriorProjectId,
                request.CorrectedProjectId,
                input.Scope,
                request.SourceVersion,
                request.StartedAtUtc,
                request.EstimatedCompletionAtUtc,
                DaprCorrectionPropagationCoordinator.ResponsibleOwnerRole,
                DaprCorrectionPropagationCoordinator.PendingNextSafeAction,
                DaprCorrectionPropagationCoordinator.SchemaVersion),
            CancellationToken.None)
            .ConfigureAwait(false);

        await _statusSink
            .ReportAsync(
                request,
                CorrectionPropagationWorkflowStatuses.Started,
                workflowRetryCount: 0,
                CorrectionPropagationWorkflowFailureCodes.None,
                CancellationToken.None)
            .ConfigureAwait(false);
        return true;
    }
}
