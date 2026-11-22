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

        IngestionBindingResolvedContext context = await RetryAsync(async () =>
        {
            var resolved = await steps.ResolveAsync(request).ConfigureAwait(true);
            ValidateContext(request, resolved);
            return resolved;
        }, request, "resolve", steps).ConfigureAwait(true);

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
        if (request.RetryPhase == "finalize" && (request.IndexedUnits?.Length != sources.Count || request.IndexedUnits.Any(string.IsNullOrWhiteSpace)))
        { throw new InvalidOperationException("ingestion_binding_finalize_checkpoint_unavailable"); }
        foreach (IngestionBindingSourceRequest source in sources)
        {
            completed.Add(request.RetryPhase == "finalize"
                ? new(source.RecordKind, source.Ordinal, request.IndexedUnits![source.Ordinal])
                : await CompleteSourceAsync(source, steps).ConfigureAwait(true));
        }

        await RetryAsync(async () =>
        {
            await steps.FinalizeAsync(new IngestionBindingFinalizeInput(request, context, completed)).ConfigureAwait(true);
            return true;
        }, request, "finalize", steps, completed.Select(unit => unit.MemoryUnitId).ToArray()).ConfigureAwait(true);
        if (request.RetryPhase is { } retriedPhase)
        {
            await steps.RecordReviewAsync(new(request, retriedPhase, request.RetryAttempt, "recovered", "binding_confirmed")).ConfigureAwait(true);
        }
        return new IngestionBindingWorkflowResult("completed", context.PriorCaseId, completed.Count);
    }

    private static async Task<IngestionBindingCompletedSource> CompleteSourceAsync(
        IngestionBindingSourceRequest source,
        IIngestionBindingWorkflowSteps steps)
    {
        string phase = $"{source.RecordKind}-{source.Ordinal}";
        // A manual and automatic retry of the same confirmed terminal attempt rejoin one Memories operation.
        int firstGeneration = source.Request.RetryPhase == phase ? source.Request.RetryAttempt : 0;
        foreach (string nested in new[] { $"{phase}-start-", $"{phase}-poll-" })
        {
            if (source.Request.RetryPhase?.StartsWith(nested, StringComparison.Ordinal) == true &&
                int.TryParse(source.Request.RetryPhase.AsSpan(nested.Length), System.Globalization.CultureInfo.InvariantCulture, out int recorded))
            { firstGeneration = recorded; }
        }
        for (int attempt = firstGeneration; ; attempt++)
        {
            var generation = source with { RecoveryGeneration = attempt };
            IngestionBindingSourceOperation operation = await RetryAsync(
                () => steps.StartAsync(generation), source.Request, $"{phase}-start-{attempt}", steps).ConfigureAwait(true);
            while (true)
            {
                IngestionBindingSourceStatus status = await RetryAsync(
                    () => steps.GetStatusAsync(operation), source.Request, $"{phase}-poll-{attempt}", steps).ConfigureAwait(true);
                if (IsTerminalSuccess(status))
                {
                    if (attempt > 0)
                    {
                        await steps.RecordReviewAsync(new(source.Request, phase, attempt, "recovered", "indexed")).ConfigureAwait(true);
                    }
                    return new IngestionBindingCompletedSource(source.RecordKind, source.Ordinal, status.MemoryUnitId!);
                }
                if (IsTerminalFailure(status))
                {
                    await RecordFailureAsync(source.Request, phase, attempt, steps, "ingestion_failed").ConfigureAwait(true);
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
            && (!status.IsIndexed || string.IsNullOrWhiteSpace(status.MemoryUnitId)));

    private static async Task<T> RetryAsync<T>(Func<Task<T>> operation, IngestionBindingRequest request,
        string phase, IIngestionBindingWorkflowSteps steps, string[]? indexedUnits = null)
    {
        for (int attempt = 0; ; attempt++)
        {
            T value;
            try
            {
                value = await operation().ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is not (OperationCanceledException or OutOfMemoryException or StackOverflowException))
            {
                await RecordFailureAsync(request, phase, (request.RetryPhase == phase ? request.RetryAttempt : 0) + attempt, steps, ex.Message == "ingestion_binding_attachment_manifest_invalid" ? "attachment_manifest_invalid" : "source_unavailable", indexedUnits).ConfigureAwait(true);
                continue;
            }
            if (attempt > 0)
            {
                await steps.RecordReviewAsync(new(request, phase, (request.RetryPhase == phase ? request.RetryAttempt : 0) + attempt, "recovered", "source_confirmed")).ConfigureAwait(true);
            }
            return value;
        }
    }

    private static async Task RecordFailureAsync(IngestionBindingRequest request, string phase, int attempt,
        IIngestionBindingWorkflowSteps steps, string reason, string[]? indexedUnits = null)
    {
        bool exhausted = attempt + 1 >= Retry.RetryFailurePolicy.DefaultMaxAttempts;
        await steps.RecordReviewAsync(new(request, phase, attempt, exhausted ? "exhausted" : "failed", reason, indexedUnits)).ConfigureAwait(true);
        if (exhausted) { throw new InvalidOperationException("ingestion_retries_exhausted"); }
        await steps.DelayAsync(PollDelay).ConfigureAwait(true);
        await steps.RecordReviewAsync(new(request, phase, attempt + 1, "retrying", "retry_started", indexedUnits)).ConfigureAwait(true);
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
