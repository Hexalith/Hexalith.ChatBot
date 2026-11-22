using Dapr.Workflow;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Sends transition commands outside replayed orchestration code.</summary>
internal sealed class IngestionReviewActivity(IWorkflowReviewCommandWriter writer) : WorkflowActivity<IngestionReviewInput, bool>
{
    public override async Task<bool> RunAsync(WorkflowActivityContext context, IngestionReviewInput input)
    {
        var request = input.Request;
        string subject = ProjectConversationStableId.Hash(System.Text.Json.JsonSerializer.Serialize(
            new[] { request.AssociationId, request.IntakeId, input.Phase }));
        var command = new RecordWorkflowReviewTransition(OperationalQueueFamily.FailedIngestion, subject,
            request.AssociatedProjectId, request.SourceVersion, input.Attempt, input.State, input.Reason,
            new WorkflowReviewTarget(request.AssociationId, request.IntakeId, input.Phase, input.IndexedUnits));
        await writer.RecordAsync(request.TenantId, request.CorrelationId, command, CancellationToken.None).ConfigureAwait(false);
        return true;
    }
}
