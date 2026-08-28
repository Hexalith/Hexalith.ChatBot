using Dapr.Workflow;

using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Resolves the prior Memories case and exact provider source metadata.</summary>
internal sealed class IngestionBindingResolveActivity(
    IMemoriesCaseResolver caseResolver,
    IProjectConversationProjectionStore projectionStore)
    : WorkflowActivity<IngestionBindingRequest, IngestionBindingResolvedContext>
{
    public override async Task<IngestionBindingResolvedContext> RunAsync(
        WorkflowActivityContext context,
        IngestionBindingRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);
        string priorCaseId = await caseResolver
            .ResolveCaseIdAsync(
                input.TenantId,
                input.AssociatedProjectId,
                input.CorrelationId,
                CancellationToken.None)
            .ConfigureAwait(false);
        ProjectConversationIngestionSource? source = await projectionStore
            .GetIngestionSourceAsync(
                input.TenantId,
                input.AssociatedProjectId,
                input.AssociationId,
                input.IntakeId,
                CancellationToken.None)
            .ConfigureAwait(false);
        return source is null
            ? throw new InvalidOperationException("ingestion_binding_source_unavailable")
            : new IngestionBindingResolvedContext(priorCaseId, source);
    }
}
