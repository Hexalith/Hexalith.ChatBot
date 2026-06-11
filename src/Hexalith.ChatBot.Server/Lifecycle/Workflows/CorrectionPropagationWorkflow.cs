using Dapr.Workflow;

namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal sealed class CorrectionPropagationWorkflow
    : Workflow<CorrectionPropagationRequest, CorrectionPropagationWorkflowResult>
{
    public override async Task<CorrectionPropagationWorkflowResult> RunAsync(
        WorkflowContext context,
        CorrectionPropagationRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);

        WorkflowTaskOptions retryOptions = new(new WorkflowRetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(2),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromMinutes(1)));

        context.SetCustomStatus(Progress(input, CorrectionPropagationWorkflowStatuses.Started, 0, CorrectionPropagationWorkflowFailureCodes.None));

        IReadOnlyList<string> scope = await context
            .CallActivityAsync<IReadOnlyList<string>>(nameof(CorrectionPropagationScopeActivity), input, retryOptions)
            .ConfigureAwait(true);

        await context
            .CallActivityAsync<bool>(nameof(CorrectionPropagationStartActivity), new CorrectionPropagationStartInput(input, scope), retryOptions)
            .ConfigureAwait(true);

        List<CorrectionPropagationActivityResult> results = [];
        foreach (string storeKey in scope)
        {
            context.SetCustomStatus(Progress(input, CorrectionPropagationWorkflowStatuses.Retrying, results.Count, CorrectionPropagationWorkflowFailureCodes.None));
            CorrectionPropagationActivityResult result = await context
                .CallActivityAsync<CorrectionPropagationActivityResult>(
                    nameof(CorrectionPropagationRunStoreActivity),
                    new CorrectionPropagationStoreActivityInput(input, storeKey, context.CurrentUtcDateTime),
                    retryOptions)
                .ConfigureAwait(true);
            results.Add(result);
        }

        if (results.All(static result => result.IsSuccessful))
        {
            context.SetCustomStatus(Progress(input, CorrectionPropagationWorkflowStatuses.Completed, results.Count, CorrectionPropagationWorkflowFailureCodes.None));
            await context
                .CallActivityAsync<bool>(nameof(CorrectionPropagationCompleteActivity), input, retryOptions)
                .ConfigureAwait(true);
            return new CorrectionPropagationWorkflowResult(
                CorrectionPropagationWorkflowStatuses.Completed,
                results.Count,
                null,
                scope);
        }

        string delayReason = results
            .FirstOrDefault(static result => !result.IsSuccessful)?.FailureReasonCode
            ?? DaprCorrectionPropagationCoordinator.DefaultDelayReasonCode;
        context.SetCustomStatus(Progress(input, CorrectionPropagationWorkflowStatuses.Delayed, results.Count, delayReason));
        await context
            .CallActivityAsync<bool>(nameof(CorrectionPropagationDelayActivity), new CorrectionPropagationDelayInput(input, delayReason), retryOptions)
            .ConfigureAwait(true);
        return new CorrectionPropagationWorkflowResult(
            CorrectionPropagationWorkflowStatuses.Delayed,
            results.Count,
            delayReason,
            scope);
    }

    private static CorrectionPropagationWorkflowProgress Progress(
        CorrectionPropagationRequest request,
        string status,
        int retryCount,
        string failureCode)
        => new(
            status,
            request.WorkflowInstanceId,
            request.TenantId,
            request.CorrectionId,
            request.SourceVersion,
            retryCount,
            failureCode,
            request.CorrelationId);
}
