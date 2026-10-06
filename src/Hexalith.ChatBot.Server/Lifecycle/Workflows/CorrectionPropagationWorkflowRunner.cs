namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

internal static class CorrectionPropagationWorkflowRunner
{
    private static readonly TimeSpan RemoteStatusPollDelay = TimeSpan.FromSeconds(30);
    private const int MaxCaseResolutionAttempts = 5;

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
                    // A healthy pending poll carries no failure: report it as propagation pending, not as a
                    // store outage. Only an explicit store failure code reaches the status sink as a failure.
                    await WaitForRetryAsync(resolvedInput, steps, 0,
                        result.FailureReasonCode ?? CorrectionPropagationWorkflowFailureCodes.None,
                        results.Count).ConfigureAwait(true);
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
                if (retryCount >= MaxCaseResolutionAttempts)
                {
                    steps.SetStatus(Progress(
                        input,
                        CorrectionPropagationWorkflowStatuses.Failed,
                        0,
                        CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable) with { RetryCount = retryCount });
                    await PublishRetryStatusSafelyAsync(steps, new CorrectionPropagationRetryStatusInput(
                        input, retryCount, CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable,
                        CorrectionPropagationWorkflowStatuses.Failed)).ConfigureAwait(true);
                    throw new InvalidOperationException(CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable, exception);
                }

                steps.SetStatus(Progress(
                    input,
                    CorrectionPropagationWorkflowStatuses.Retrying,
                    0,
                    CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable) with { RetryCount = retryCount });
                await WaitForRetryAsync(input, steps, retryCount,
                    CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable, 0).ConfigureAwait(true);
            }
        }
    }

    private static async Task WaitForRetryAsync(
        CorrectionPropagationRequest input,
        ICorrectionPropagationWorkflowSteps steps,
        int retryCount,
        string failureCode,
        int storesCompleted)
    {
        // ISystemClock is the observable UTC authority. Reading it in an activity records
        // the result in workflow history; replay never reads wall time directly.
        DateTimeOffset dueAt = (await steps.ReadUtcAsync().ConfigureAwait(true)).Add(RemoteStatusPollDelay);
        Task timer = steps.CreateTimerAtAsync(dueAt);
        steps.SetStatus(Progress(input, CorrectionPropagationWorkflowStatuses.Retrying, storesCompleted, failureCode)
            with { RetryCount = retryCount, RetryDueAt = dueAt });
        await PublishRetryStatusSafelyAsync(steps, new CorrectionPropagationRetryStatusInput(
            input, retryCount, failureCode, RetryDueAt: dueAt)).ConfigureAwait(true);
        await timer.ConfigureAwait(true);
        // Custom workflow status is authoritative if publishing the projection is unavailable.
        steps.SetStatus(Progress(input, CorrectionPropagationWorkflowStatuses.Started, storesCompleted, failureCode)
            with { RetryCount = retryCount });
        await PublishRetryStatusSafelyAsync(steps, new CorrectionPropagationRetryStatusInput(
            input, retryCount, failureCode, CorrectionPropagationWorkflowStatuses.Started)).ConfigureAwait(true);
    }

    private static async Task PublishRetryStatusSafelyAsync(
        ICorrectionPropagationWorkflowSteps steps,
        CorrectionPropagationRetryStatusInput input)
    {
        try
        {
            await steps.CallRetryStatusAsync(input).ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Status is a projection. Publication exhaustion cannot abandon a scheduled timer.
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
