using Dapr.Workflow;

using Hexalith.ChatBot.Server.Projections.DerivedStores;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Reads one safe ingestion status from Memories and maps it to the ChatBot workflow boundary.</summary>
internal sealed class IngestionBindingGetStatusActivity(IIngestionBindingSourceAdapter sourceAdapter)
    : WorkflowActivity<IngestionBindingSourceOperation, IngestionBindingSourceStatus>
{
    public override Task<IngestionBindingSourceStatus> RunAsync(
        WorkflowActivityContext context,
        IngestionBindingSourceOperation input)
        => sourceAdapter.GetStatusAsync(input, CancellationToken.None);
}
