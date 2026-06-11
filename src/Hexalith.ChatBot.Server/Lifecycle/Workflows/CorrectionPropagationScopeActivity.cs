using Dapr.Workflow;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class CorrectionPropagationScopeActivity(ICorrectionPropagationActivityCatalog activityCatalog)
    : WorkflowActivity<CorrectionPropagationRequest, IReadOnlyList<string>>
{
    public override Task<IReadOnlyList<string>> RunAsync(
        WorkflowActivityContext context,
        CorrectionPropagationRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Task.FromResult(activityCatalog.Scope);
    }
}
