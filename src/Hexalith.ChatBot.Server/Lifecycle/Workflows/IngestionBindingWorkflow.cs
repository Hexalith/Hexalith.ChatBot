using Dapr.Workflow;

using Hexalith.ChatBot.Server.Adapters.Memories;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>ChatBot-owned durable workflow that ingests every governed source then finalizes one binding.</summary>
internal sealed class IngestionBindingWorkflow
    : Workflow<IngestionBindingRequest, IngestionBindingWorkflowResult>
{
    public override Task<IngestionBindingWorkflowResult> RunAsync(
        WorkflowContext context,
        IngestionBindingRequest input)
        => IngestionBindingWorkflowRunner.RunAsync(input, new DaprWorkflowSteps(context));

    private sealed class DaprWorkflowSteps(WorkflowContext context) : IIngestionBindingWorkflowSteps
    {
        private readonly WorkflowTaskOptions _retryOptions = new(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(2),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromMinutes(1)));

        public Task<IngestionBindingResolvedContext> ResolveAsync(IngestionBindingRequest request)
            => context.CallActivityAsync<IngestionBindingResolvedContext>(
                nameof(IngestionBindingResolveActivity),
                request,
                _retryOptions);

        public Task<IngestionBindingSourceOperation> StartAsync(IngestionBindingSourceRequest request)
            => context.CallActivityAsync<IngestionBindingSourceOperation>(
                nameof(IngestionBindingStartSourceActivity),
                request,
                _retryOptions);

        public Task<IngestionBindingSourceStatus> GetStatusAsync(IngestionBindingSourceOperation operation)
            => context.CallActivityAsync<IngestionBindingSourceStatus>(
                nameof(IngestionBindingGetStatusActivity),
                operation,
                _retryOptions);

        public Task DelayAsync(TimeSpan delay)
            => context.CreateTimer(delay);

        public async Task FinalizeAsync(IngestionBindingFinalizeInput input)
        {
            bool finalized = await context.CallActivityAsync<bool>(
                nameof(IngestionBindingFinalizeActivity),
                input,
                _retryOptions).ConfigureAwait(true);
            if (!finalized)
            {
                throw new InvalidOperationException("ingestion_binding_finalize_identity_mismatch");
            }
        }
    }
}
