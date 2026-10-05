namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal static class CorrectionPropagationWorkflowRunner
{
    private static readonly TimeSpan RemoteStatusPollDelay = TimeSpan.FromSeconds(30);
    private const int MaxCaseResolutionRetries = 5;

    public static async Task<CorrectionPropagationWorkflowResult> RunAsync(
        CorrectionPropagationRequest input,
        ICorrectionPropagationWorkflowSteps steps)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(steps);

        steps.SetStatus(Progress(input, CorrectionPropagationWorkflowStatuses.Started, 0, CorrectionPropagationWorkflowFailureCodes.None));

        string correctedCaseId = string.IsNullOrWhiteSpace(input.CorrectedCaseId)
            ? await ResolveCorrectedCaseAsync(input, steps).ConfigureAwait(true)
            : input.CorrectedCaseId;
        ArgumentException.ThrowIfNullOrWhiteSpace(correctedCaseId);
        CorrectionPropagationRequest resolvedInput = input with { CorrectedCaseId = correctedCaseId };

        IReadOnlyList<string> scope = await steps.CallScopeAsync(resolvedInput).ConfigureAwait(true);

        await steps.CallStartAsync(new CorrectionPropagationStartInput(resolvedInput, scope)).ConfigureAwait(true);

        List<CorrectionPropagationActivityResult> results = [];
        foreach (string storeKey in scope)
        {
            CorrectionPropagationActivityResult result;
            string? remoteOperationId = null;
            do
            {
                steps.SetStatus(Progress(resolvedInput, CorrectionPropagationWorkflowStatuses.Started, results.Count, CorrectionPropagationWorkflowFailureCodes.None));
                result = await steps
                    .CallStoreAsync(new CorrectionPropagationStoreActivityInput(
                        resolvedInput,
                        storeKey,
                        steps.CurrentUtc,
                        remoteOperationId))
                    .ConfigureAwait(true);
                remoteOperationId = result.RemoteOperationId;
                if (result.IsPending)
                {
                    await steps.CreateTimerAsync(RemoteStatusPollDelay).ConfigureAwait(true);
                }
            }
            while (result.IsPending);
            results.Add(result);
        }

        if (results.All(static result => result.IsSuccessful))
        {
            steps.SetStatus(Progress(resolvedInput, CorrectionPropagationWorkflowStatuses.Completed, results.Count, CorrectionPropagationWorkflowFailureCodes.None));
            await steps.CallCompleteAsync(resolvedInput).ConfigureAwait(true);
            return new CorrectionPropagationWorkflowResult(
                CorrectionPropagationWorkflowStatuses.Completed,
                results.Count,
                null,
                scope);
        }

        string delayReason = results
            .FirstOrDefault(static result => !result.IsSuccessful)?.FailureReasonCode
            ?? DaprCorrectionPropagationCoordinator.DefaultDelayReasonCode;
        steps.SetStatus(Progress(resolvedInput, CorrectionPropagationWorkflowStatuses.Delayed, results.Count, delayReason));
        bool delaySucceeded = await steps
            .CallDelayAsync(new CorrectionPropagationDelayInput(resolvedInput, delayReason))
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

    private static async Task<string> ResolveCorrectedCaseAsync(
        CorrectionPropagationRequest input,
        ICorrectionPropagationWorkflowSteps steps)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                string correctedCaseId = await steps.CallResolveCorrectedCaseAsync(input).ConfigureAwait(true);
                ArgumentException.ThrowIfNullOrWhiteSpace(correctedCaseId);
                return correctedCaseId;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                retryCount++;
                if (retryCount >= MaxCaseResolutionRetries)
                {
                    steps.SetStatus(Progress(
                        input,
                        CorrectionPropagationWorkflowStatuses.Failed,
                        retryCount,
                        CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable));
                    await steps.CallRetryStatusAsync(new CorrectionPropagationRetryStatusInput(
                        input, retryCount, CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable,
                        CorrectionPropagationWorkflowStatuses.Failed)).ConfigureAwait(true);
                    throw new InvalidOperationException(CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable, exception);
                }

                steps.SetStatus(Progress(
                    input,
                    CorrectionPropagationWorkflowStatuses.Retrying,
                    retryCount,
                    CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable));
                // Schedule first, then publish a due time derived from the workflow clock. Activity
                // latency can no longer make the status eligible before the timer exists.
                Task timer = steps.CreateTimerAsync(RemoteStatusPollDelay);
                DateTimeOffset dueAt = steps.CurrentUtc.Add(RemoteStatusPollDelay);
                await steps.CallRetryStatusAsync(new CorrectionPropagationRetryStatusInput(
                    input, retryCount, CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable,
                    RetryDueAt: dueAt)).ConfigureAwait(true);
                await timer.ConfigureAwait(true);
                // The due window closes when the timer fires and the next attempt starts.
                await steps.CallRetryStatusAsync(new CorrectionPropagationRetryStatusInput(
                    input, retryCount, CorrectionPropagationWorkflowFailureCodes.None,
                    CorrectionPropagationWorkflowStatuses.Started)).ConfigureAwait(true);
            }
        }
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
