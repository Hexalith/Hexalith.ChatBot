using Dapr.Workflow;

using Hexalith.ChatBot.Server.Adapters.Projects;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Resolves the corrected Project to its sole included Memories case reference.</summary>
internal sealed class CorrectionPropagationResolveCaseActivity(IMemoriesCaseResolver caseResolver)
    : WorkflowActivity<CorrectionPropagationRequest, string>
{
    public override async Task<string> RunAsync(
        WorkflowActivityContext context,
        CorrectionPropagationRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return await caseResolver
            .ResolveCaseIdAsync(
                input.TenantId,
                input.CorrectedProjectId,
                input.CorrelationId,
                CancellationToken.None)
            .ConfigureAwait(false);
    }
}
