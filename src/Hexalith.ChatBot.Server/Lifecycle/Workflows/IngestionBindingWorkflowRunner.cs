namespace Hexalith.ChatBot.Server.Lifecycle.Workflows;

/// <summary>Deterministic orchestration logic for source ingestion and atomic binding finalization.</summary>
internal static class IngestionBindingWorkflowRunner
{
    private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(30);

    public static async Task<IngestionBindingWorkflowResult> RunAsync(
        IngestionBindingRequest request,
        IIngestionBindingWorkflowSteps steps)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(steps);

        IngestionBindingResolvedContext context = await ResolveValidatedContextAsync(request, steps).ConfigureAwait(true);

        List<IngestionBindingSourceRequest> sources =
        [
            new(request, context, IngestionBindingRecordKind.Message, 0, null, "application/json"),
            .. context.Source.Attachments.Select(attachment => new IngestionBindingSourceRequest(
                request,
                context,
                IngestionBindingRecordKind.Attachment,
                attachment.Ordinal,
                attachment.ProviderAttachmentId,
                attachment.ContentType)),
        ];

        var completed = new List<IngestionBindingCompletedSource>(sources.Count);
        foreach (IngestionBindingSourceRequest source in sources)
        {
            completed.Add(await CompleteSourceAsync(source, steps).ConfigureAwait(true));
        }

        await RetryAsync(
            () => steps.FinalizeAsync(new IngestionBindingFinalizeInput(request, context, completed)),
            steps).ConfigureAwait(true);
        return new IngestionBindingWorkflowResult("completed", context.PriorCaseId, completed.Count);
    }

    private static async Task<IngestionBindingCompletedSource> CompleteSourceAsync(
        IngestionBindingSourceRequest source,
        IIngestionBindingWorkflowSteps steps)
    {
        while (true)
        {
            IngestionBindingSourceOperation operation = await RetryAsync(
                () => steps.StartAsync(source),
                steps).ConfigureAwait(true);
            while (true)
            {
                IngestionBindingSourceStatus status = await RetryAsync(
                    () => steps.GetStatusAsync(operation),
                    steps).ConfigureAwait(true);
                if (IsTerminalSuccess(status))
                {
                    return new IngestionBindingCompletedSource(source.RecordKind, source.Ordinal, status.MemoryUnitId!);
                }

                if (IsTerminalFailure(status))
                {
                    await steps.DelayAsync(PollDelay).ConfigureAwait(true);
                    break;
                }

                await steps.DelayAsync(PollDelay).ConfigureAwait(true);
            }
        }
    }

    private static bool IsTerminalSuccess(IngestionBindingSourceStatus status)
        => string.Equals(status.RuntimeStatus, "Completed", StringComparison.OrdinalIgnoreCase)
        && status.IsIndexed
        && !string.IsNullOrWhiteSpace(status.MemoryUnitId);

    private static bool IsTerminalFailure(IngestionBindingSourceStatus status)
        => string.Equals(status.RuntimeStatus, "Failed", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status.RuntimeStatus, "Terminated", StringComparison.OrdinalIgnoreCase)
        || (string.Equals(status.RuntimeStatus, "Completed", StringComparison.OrdinalIgnoreCase)
            && !status.IsIndexed);

    private static async Task<T> RetryAsync<T>(Func<Task<T>> operation, IIngestionBindingWorkflowSteps steps)
    {
        while (true)
        {
            try
            {
                return await operation().ConfigureAwait(true);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await steps.DelayAsync(PollDelay).ConfigureAwait(true);
            }
        }
    }

    private static async Task<IngestionBindingResolvedContext> ResolveValidatedContextAsync(
        IngestionBindingRequest request,
        IIngestionBindingWorkflowSteps steps)
    {
        while (true)
        {
            IngestionBindingResolvedContext resolved;
            try
            {
                resolved = await steps.ResolveAsync(request).ConfigureAwait(true);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await steps.DelayAsync(PollDelay).ConfigureAwait(true);
                continue;
            }

            try
            {
                ValidateContext(request, resolved);
                return resolved;
            }
            catch (InvalidOperationException exception)
                when (string.Equals(exception.Message, "ingestion_binding_source_mismatch", StringComparison.Ordinal))
            {
                await steps.DelayAsync(PollDelay).ConfigureAwait(true);
            }
        }
    }

    private static async Task RetryAsync(Func<Task> operation, IIngestionBindingWorkflowSteps steps)
    {
        while (true)
        {
            try
            {
                await operation().ConfigureAwait(true);
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await steps.DelayAsync(PollDelay).ConfigureAwait(true);
            }
        }
    }

    private static void ValidateContext(IngestionBindingRequest request, IngestionBindingResolvedContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context.PriorCaseId);
        ArgumentNullException.ThrowIfNull(context.Source);
        if (!string.Equals(context.Source.TenantId, request.TenantId, StringComparison.Ordinal)
            || !string.Equals(context.Source.ProjectId, request.AssociatedProjectId, StringComparison.Ordinal)
            || !string.Equals(context.Source.AssociationId, request.AssociationId, StringComparison.Ordinal)
            || !string.Equals(context.Source.IntakeId, request.IntakeId, StringComparison.Ordinal)
            || context.Source.SourceVersion != request.SourceVersion)
        {
            throw new InvalidOperationException("ingestion_binding_source_mismatch");
        }

        int[] ordinals = [.. context.Source.Attachments.Select(static attachment => attachment.Ordinal)];
        if (ordinals.Distinct().Count() != ordinals.Length
            || !ordinals.SequenceEqual(Enumerable.Range(1, ordinals.Length))
            || context.Source.Attachments.Select(static attachment => attachment.ProviderAttachmentId).Distinct(StringComparer.Ordinal).Count() != ordinals.Length)
        {
            throw new InvalidOperationException("ingestion_binding_attachment_manifest_invalid");
        }
    }
}
