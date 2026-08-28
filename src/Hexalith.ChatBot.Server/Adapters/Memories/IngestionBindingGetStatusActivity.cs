using Dapr.Workflow;

using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;

namespace Hexalith.ChatBot.Server.Adapters.Memories;

/// <summary>Reads one safe ingestion status from Memories and maps it to the ChatBot workflow boundary.</summary>
internal sealed class IngestionBindingGetStatusActivity(MemoriesClient memories)
    : WorkflowActivity<IngestionBindingSourceOperation, IngestionBindingSourceStatus>
{
    public override async Task<IngestionBindingSourceStatus> RunAsync(
        WorkflowActivityContext context,
        IngestionBindingSourceOperation input)
    {
        ArgumentNullException.ThrowIfNull(input);
        IngestionWorkflowStatus status = await memories
            .GetIngestionWorkflowStatusAsync(input.InstanceId, CancellationToken.None)
            .ConfigureAwait(false);
        if (!string.Equals(status.InstanceId, input.InstanceId, StringComparison.Ordinal)
            || !string.Equals(status.TenantId, input.Source.Request.TenantId, StringComparison.Ordinal)
            || !string.Equals(status.CaseId, input.Source.Context.PriorCaseId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("ingestion_binding_status_identity_mismatch");
        }

        return new IngestionBindingSourceStatus(
            status.RuntimeStatus,
            status.MemoryUnitId,
            status.MemoryUnitStatus is MemoryUnitStatus.Indexed);
    }
}
