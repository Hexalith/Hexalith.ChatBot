using Dapr.Workflow;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class CorrectionPropagationRetryStatusActivity(
    ICorrectionPropagationWorkflowStatusSink statusSink)
    : WorkflowActivity<CorrectionPropagationRetryStatusInput, bool>
{
    public override async Task<bool> RunAsync(
        WorkflowActivityContext context,
        CorrectionPropagationRetryStatusInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        await statusSink.ReportAsync(
            input.Request,
            input.WorkflowStatus,
            input.RetryCount,
            input.FailureCode,
            CancellationToken.None,
            input.RetryDueAt).ConfigureAwait(false);
        return true;
    }
}
