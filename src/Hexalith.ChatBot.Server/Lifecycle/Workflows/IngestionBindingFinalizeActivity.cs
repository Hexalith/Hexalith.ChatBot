using Dapr.Workflow;

using Hexalith.ChatBot.Server.Projections.DerivedStores;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Publishes one complete ordered association/intake binding atomically in Memories.</summary>
internal sealed class IngestionBindingFinalizeActivity(IIngestionBindingSourceAdapter sourceAdapter)
    : WorkflowActivity<IngestionBindingFinalizeInput, bool>
{
    public override Task<bool> RunAsync(
        WorkflowActivityContext context,
        IngestionBindingFinalizeInput input)
        => sourceAdapter.FinalizeAsync(input, CancellationToken.None);
}
