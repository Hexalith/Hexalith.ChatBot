using Dapr.Workflow;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class CorrectionPropagationWorkflow
    : Workflow<CorrectionPropagationRequest, CorrectionPropagationWorkflowResult>
{
    public override Task<CorrectionPropagationWorkflowResult> RunAsync(
        WorkflowContext context,
        CorrectionPropagationRequest input)
        => CorrectionPropagationWorkflowRunner.RunAsync(input, new DaprWorkflowSteps(context));

    private sealed class DaprWorkflowSteps(WorkflowContext context) : ICorrectionPropagationWorkflowSteps
    {
        private readonly WorkflowTaskOptions _retryOptions = new(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(2),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromMinutes(1)));

        public DateTimeOffset CurrentUtc => context.CurrentUtcDateTime;

        public void SetStatus(CorrectionPropagationWorkflowProgress progress)
            => context.SetCustomStatus(progress);

        public Task<IReadOnlyList<string>> CallScopeAsync(CorrectionPropagationRequest request)
            => context.CallActivityAsync<IReadOnlyList<string>>(
                nameof(CorrectionPropagationScopeActivity),
                request,
                _retryOptions);

        public Task CallStartAsync(CorrectionPropagationStartInput input)
            => context.CallActivityAsync<bool>(
                nameof(CorrectionPropagationStartActivity),
                input,
                _retryOptions);

        public Task<CorrectionPropagationActivityResult> CallStoreAsync(CorrectionPropagationStoreActivityInput input)
            => context.CallActivityAsync<CorrectionPropagationActivityResult>(
                nameof(CorrectionPropagationRunStoreActivity),
                input,
                _retryOptions);

        public Task CallCompleteAsync(CorrectionPropagationRequest request)
            => context.CallActivityAsync<bool>(
                nameof(CorrectionPropagationCompleteActivity),
                request,
                _retryOptions);

        public Task<bool> CallDelayAsync(CorrectionPropagationDelayInput input)
            => context.CallActivityAsync<bool>(
                nameof(CorrectionPropagationDelayActivity),
                input,
                _retryOptions);
    }
}
