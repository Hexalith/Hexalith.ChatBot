using Dapr.Workflow;

using Hexalith.ChatBot.Server.Association;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class CorrectionPropagationStartActivity(ICorrectionPropagationCommandWriter writer)
    : WorkflowActivity<CorrectionPropagationStartInput, bool>
{
    public override async Task<bool> RunAsync(
        WorkflowActivityContext context,
        CorrectionPropagationStartInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
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
        return true;
    }
}
