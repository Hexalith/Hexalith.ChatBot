namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal static class CorrectionPropagationWorkflowRunner
{
    public static async Task<CorrectionPropagationWorkflowResult> RunAsync(
        CorrectionPropagationRequest input,
        ICorrectionPropagationWorkflowSteps steps)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(steps);

        steps.SetStatus(Progress(input, CorrectionPropagationWorkflowStatuses.Started, 0, CorrectionPropagationWorkflowFailureCodes.None));

        IReadOnlyList<string> scope = await steps.CallScopeAsync(input).ConfigureAwait(true);

        await steps.CallStartAsync(new CorrectionPropagationStartInput(input, scope)).ConfigureAwait(true);

        List<CorrectionPropagationActivityResult> results = [];
        foreach (string storeKey in scope)
        {
            steps.SetStatus(Progress(input, CorrectionPropagationWorkflowStatuses.Started, results.Count, CorrectionPropagationWorkflowFailureCodes.None));
            CorrectionPropagationActivityResult result = await steps
                .CallStoreAsync(new CorrectionPropagationStoreActivityInput(input, storeKey, steps.CurrentUtc))
                .ConfigureAwait(true);
            results.Add(result);
        }

        if (results.All(static result => result.IsSuccessful))
        {
            steps.SetStatus(Progress(input, CorrectionPropagationWorkflowStatuses.Completed, results.Count, CorrectionPropagationWorkflowFailureCodes.None));
            await steps.CallCompleteAsync(input).ConfigureAwait(true);
            return new CorrectionPropagationWorkflowResult(
                CorrectionPropagationWorkflowStatuses.Completed,
                results.Count,
                null,
                scope);
        }

        string delayReason = results
            .FirstOrDefault(static result => !result.IsSuccessful)?.FailureReasonCode
            ?? DaprCorrectionPropagationCoordinator.DefaultDelayReasonCode;
        steps.SetStatus(Progress(input, CorrectionPropagationWorkflowStatuses.Delayed, results.Count, delayReason));
        bool delaySucceeded = await steps
            .CallDelayAsync(new CorrectionPropagationDelayInput(input, delayReason))
            .ConfigureAwait(true);
        if (!delaySucceeded)
        {
            throw new InvalidOperationException(CorrectionPropagationWorkflowFailureCodes.AuditUnavailable);
        }

        return new CorrectionPropagationWorkflowResult(
            CorrectionPropagationWorkflowStatuses.Delayed,
            results.Count,
            delayReason,
            scope);
    }

    private static CorrectionPropagationWorkflowProgress Progress(
        CorrectionPropagationRequest request,
        string status,
        int storesCompleted,
        string failureCode)
        => new(
            status,
            request.WorkflowInstanceId,
            request.TenantId,
            request.CorrectionId,
            request.SourceVersion,
            storesCompleted,
            failureCode,
            request.CorrelationId);
}
