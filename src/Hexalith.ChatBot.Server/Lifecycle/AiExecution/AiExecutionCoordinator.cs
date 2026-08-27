using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Ulid = ByteAether.Ulid.Ulid;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal sealed partial class AiExecutionCoordinator(
    IAiExecutionWorkStore workStore,
    IAiAssistanceProvider provider,
    IEventStoreGatewayClient eventStore,
    ISystemClock clock,
    ILogger<AiExecutionCoordinator> logger,
    IChatBotAdmissionMarker? admissionMarker = null,
    TimeSpan? providerExecutionDeadline = null) : BackgroundService, IAiExecutionCoordinator
{
    private const int MaximumConcurrentExecutions = 4;
    private const int MaximumProviderExecutionAttempts = 3;
    private const int MaximumTerminalSubmissionAttempts = 3;
    private const string CoordinatorLoopFailedLog = "AI execution coordinator loop failed.";
    private const string ExecutionFailedLog = "AI execution work {WorkKey} attempt {AttemptCount} failed and was released for recovery.";
    private const string ProviderCancellationObservedLog = "AI execution work {WorkKey} observed provider cancellation.";
    private const string TerminalSubmissionStartedLog = "AI execution work {WorkKey} is submitting terminal command {CommandType}, attempt {AttemptCount}.";
    private const string TerminalSubmissionCompletedLog = "AI execution work {WorkKey} completed terminal command {CommandType}, attempt {AttemptCount}.";
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan LeaseHeartbeatInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan DefaultProviderExecutionDeadline = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeExecutions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _requestedCancellations = new(StringComparer.Ordinal);
    private readonly string _owner = $"{Environment.MachineName}:{Guid.NewGuid():N}";
    private readonly TimeSpan _providerExecutionDeadline = providerExecutionDeadline is { } configured && configured > TimeSpan.Zero
        ? configured
        : DefaultProviderExecutionDeadline;

    public async ValueTask RecordStartedAsync(
        string tenantId,
        string conversationId,
        long sourceVersion,
        LowRiskAiAssistanceExecutionStarted started,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(started);
        if (started.Execution is null)
        {
            // Events written before the persisted-event coordinator did not carry a reconstructable provider request.
            // Never invent context or invoke a provider for those legacy rows.
            return;
        }

        if (sourceVersion <= 0 ||
            !string.Equals(started.Execution.ExecutionId, started.ExecutionId, StringComparison.Ordinal) ||
            !string.Equals(started.Execution.ProposalId, started.ProposalId, StringComparison.Ordinal) ||
            !string.Equals(started.Execution.ProjectId, started.ProjectId, StringComparison.Ordinal) ||
            !string.Equals(started.Execution.CorrelationId, started.CorrelationId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The persisted AI execution start has inconsistent identity metadata.");
        }

        string key = AiExecutionWorkItem.KeyFor(
            tenantId,
            started.ProjectId,
            conversationId,
            started.ProposalId,
            started.ExecutionId);
        await workStore.UpsertStartedAsync(
            new AiExecutionWorkItem(
                key,
                tenantId,
                started.ProjectId,
                conversationId,
                started.ProposalId,
                started.ExecutionId,
                sourceVersion,
                started.Execution,
                AiExecutionWorkStatus.Pending,
                started.CorrelationId,
                started.StartedAtUtc),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask RecordCancellationRequestedAsync(
        AiResponseGenerationCancellationRequested request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await workStore.MarkCancellationRequestedAsync(request, cancellationToken).ConfigureAwait(false);
        string key = AiExecutionWorkItem.KeyFor(
            request.TenantId,
            request.ProjectId,
            request.ConversationId,
            request.ResponseId,
            request.GenerationId);
        _requestedCancellations[key] = request.CancellationId;
        if (_activeExecutions.TryGetValue(key, out CancellationTokenSource? active))
        {
            // This method runs on the inbound EventStore projection callback. Even CancelAsync() is permitted to
            // begin cancellation work before its returned ValueTask is observed. A provider continuation can submit
            // the terminal command back to the same EventStore aggregate and wait on the projection callback that is
            // still recording this request. Put the cancellation signal itself on the pool so this callback never
            // owns any part of that terminal relay call stack. The durable heartbeat remains the cross-replica path.
            ThreadPool.QueueUserWorkItem(
                static execution => execution.Cancel(),
                active,
                preferLocal: false);
        }
    }

    public async ValueTask RecordTerminalObservedAsync(
        string tenantId,
        string stateOwnerAggregateId,
        string projectId,
        string responseId,
        string generationId,
        CancellationToken cancellationToken)
    {
        string key = AiExecutionWorkItem.KeyFor(tenantId, projectId, stateOwnerAggregateId, responseId, generationId);
        await workStore.MarkTerminalObservedAsync(key, clock.UtcNow, cancellationToken).ConfigureAwait(false);
        _ = _requestedCancellations.TryRemove(key, out _);
    }

    public async ValueTask RecordCancellationFailedAsync(
        AiResponseGenerationCancellationFailed failure,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(failure);
        string key = AiExecutionWorkItem.KeyFor(
            failure.TenantId,
            failure.ProjectId,
            failure.ConversationId,
            failure.ResponseId,
            failure.GenerationId);
        await workStore.MarkCancellationFailedAsync(key, failure.FailedAtUtc, cancellationToken).ConfigureAwait(false);
        _ = _requestedCancellations.TryRemove(key, out _);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Dictionary<string, Task> running = new(StringComparer.Ordinal);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach ((string key, Task task) in running.Where(static pair => pair.Value.IsCompleted).ToArray())
                {
                    await task.ConfigureAwait(false);
                    _ = running.Remove(key);
                }

                IReadOnlyList<AiExecutionWorkItem> runnable = await workStore
                    .ListRunnableAsync(clock.UtcNow, MaximumConcurrentExecutions * 4, stoppingToken)
                    .ConfigureAwait(false);
                foreach (AiExecutionWorkItem candidate in runnable)
                {
                    if (running.Count >= MaximumConcurrentExecutions)
                    {
                        break;
                    }

                    if (running.ContainsKey(candidate.Key))
                    {
                        continue;
                    }

                    AiExecutionWorkItem? claimed = await workStore
                        .TryClaimAsync(candidate.Key, _owner, clock.UtcNow, LeaseDuration, stoppingToken)
                        .ConfigureAwait(false);
                    if (claimed is not null)
                    {
                        running[claimed.Key] = ProcessClaimedAsync(claimed, stoppingToken);
                    }
                }

                if (running.Count > 0)
                {
                    _ = await Task.WhenAny(
                            running.Values.Append(Task.Delay(PollInterval, stoppingToken)))
                        .ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                CoordinatorLoopFailed(logger, ex);
                await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessClaimedAsync(AiExecutionWorkItem work, CancellationToken stoppingToken)
    {
        using CancellationTokenSource executionCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        using CancellationTokenSource ownershipMonitoring = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _activeExecutions[work.Key] = executionCancellation;
        if (work.CancellationId is not null || _requestedCancellations.ContainsKey(work.Key))
        {
            executionCancellation.Cancel();
        }

        Task ownershipMonitor = MonitorOwnershipAsync(work.Key, executionCancellation, ownershipMonitoring.Token);
        try
        {
            if (work.Status is AiExecutionWorkStatus.CancellationRequested)
            {
                // A recovered request has no live provider invocation whose cancellation can be observed. Never
                // announce "stopped" from durable intent alone; record a safe failure so the UI can offer retry.
                await SubmitCancellationOutcomeAsync(
                    work,
                    CancellationId(work),
                    confirmed: false,
                    failureReasonCode: "provider-cancellation-unobserved",
                    cancellationToken: stoppingToken).ConfigureAwait(false);
                return;
            }

            LowRiskAiAssistanceExecutionRecord record;
            if (work.Status is AiExecutionWorkStatus.CompletionPending && work.CompletionRecord is not null)
            {
                record = work.CompletionRecord;
            }
            else
            {
                Task<LowRiskAiAssistanceExecutionRecord> providerExecution = provider
                    .ExecuteAsync(ToProviderRequest(work), executionCancellation.Token)
                    .AsTask();
                Task deadline = Task.Delay(_providerExecutionDeadline, stoppingToken);
                if (await Task.WhenAny(providerExecution, deadline).ConfigureAwait(false) != providerExecution)
                {
                    await executionCancellation.CancelAsync().ConfigureAwait(false);
                    _ = providerExecution.ContinueWith(
                        static completed => _ = completed.Exception,
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Default);
                    throw new TimeoutException($"AI provider execution '{work.Key}' exceeded its per-slot deadline.");
                }

                record = await providerExecution.ConfigureAwait(false);
                bool persisted = await workStore
                    .MarkCompletionPendingAsync(work.Key, _owner, record, clock.UtcNow, stoppingToken)
                    .ConfigureAwait(false);
                if (!persisted)
                {
                    return;
                }
            }

            // If the provider completed naturally while Stop raced with it, the natural terminal result wins. The
            // aggregate removes the active generation, making a later cancellation confirmation an idempotent no-op.
            await SubmitCompletionAsync(work, record, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested &&
            (work.CancellationId is not null || _requestedCancellations.ContainsKey(work.Key)))
        {
            // Only this path proves that the provider invocation observed the linked cancellation token.
            ProviderCancellationObserved(logger, work.Key);
            await SubmitCancellationOutcomeAsync(
                work,
                CancellationId(work),
                confirmed: true,
                failureReasonCode: null,
                cancellationToken: stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await workStore.ReleaseAsync(work.Key, _owner, clock.UtcNow, CancellationToken.None).ConfigureAwait(false);
        }
        catch (TerminalSubmissionExhaustedException ex)
        {
            ExecutionFailed(logger, ex, work.Key, work.AttemptCount);
            _ = await workStore.MarkExhaustedAsync(
                work.Key,
                _owner,
                clock.UtcNow,
                "terminal-submission-attempts-exhausted",
                stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ExecutionFailed(logger, ex, work.Key, work.AttemptCount);
            if (work.AttemptCount >= MaximumProviderExecutionAttempts)
            {
                _ = await workStore.MarkExhaustedAsync(
                    work.Key,
                    _owner,
                    clock.UtcNow,
                    "provider-execution-attempts-exhausted",
                    stoppingToken).ConfigureAwait(false);
            }
            else
            {
                _ = await workStore.ReleaseAsync(work.Key, _owner, clock.UtcNow, stoppingToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ownershipMonitoring.Cancel();
            await ObserveMonitorCompletionAsync(ownershipMonitor).ConfigureAwait(false);
            _ = _activeExecutions.TryRemove(work.Key, out _);
        }
    }

    private async Task MonitorOwnershipAsync(
        string key,
        CancellationTokenSource executionCancellation,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(LeaseHeartbeatInterval, cancellationToken).ConfigureAwait(false);
            AiExecutionWorkItem? refreshed = await workStore
                .TryRenewLeaseAsync(key, _owner, clock.UtcNow, LeaseDuration, cancellationToken)
                .ConfigureAwait(false);
            if (refreshed is null)
            {
                executionCancellation.Cancel();
                return;
            }

            if (refreshed.Status is AiExecutionWorkStatus.CancellationRequested &&
                !string.IsNullOrWhiteSpace(refreshed.CancellationId))
            {
                _requestedCancellations[key] = refreshed.CancellationId;
                executionCancellation.Cancel();
                return;
            }
        }
    }

    private static async Task ObserveMonitorCompletionAsync(Task monitor)
    {
        try
        {
            await monitor.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The provider reached a terminal result, so the ownership heartbeat is no longer needed.
        }
    }

    private async Task SubmitCompletionAsync(
        AiExecutionWorkItem work,
        LowRiskAiAssistanceExecutionRecord record,
        CancellationToken cancellationToken)
    {
        CompleteLowRiskAiAssistance command = new(
            work.Execution,
            work.ConversationId,
            record,
            $"ai-completion:{work.GenerationId}");
        await SubmitTerminalWithRetryAsync(
            work,
            TerminalMessageId(work, nameof(CompleteLowRiskAiAssistance), command.CompletionId),
            nameof(CompleteLowRiskAiAssistance),
            command,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task SubmitCancellationOutcomeAsync(
        AiExecutionWorkItem work,
        string cancellationId,
        bool confirmed,
        string? failureReasonCode,
        CancellationToken cancellationToken)
    {
        CompleteAiResponseGenerationCancellation command = new(
            work.ProjectId,
            work.ConversationId,
            work.ResponseId,
            work.GenerationId,
            cancellationId,
            work.CorrelationId,
            Confirmed: confirmed,
            FailureReasonCode: failureReasonCode,
            CompletionId: $"ai-cancellation-completion:{cancellationId}");
        await SubmitTerminalWithRetryAsync(
            work,
            TerminalMessageId(work, nameof(CompleteAiResponseGenerationCancellation), command.CompletionId),
            nameof(CompleteAiResponseGenerationCancellation),
            command,
            cancellationToken).ConfigureAwait(false);
    }

    internal static string TerminalMessageId(
        AiExecutionWorkItem work,
        string commandType,
        string completionId)
    {
        ArgumentNullException.ThrowIfNull(work);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        ArgumentException.ThrowIfNullOrWhiteSpace(completionId);

        // EventStore command message ids permit only alphanumerics and hyphens. The descriptive domain completion
        // ids deliberately contain ':' and therefore cannot be reused as transport ids. Derive a canonical ULID
        // from the persisted work identity and exact terminal outcome instead: it is valid at the gateway, stable
        // across retries/restarts, distinct for a later cancellation attempt, and carries no payload metadata.
        byte[] material = Encoding.UTF8.GetBytes(string.Join(
            '\u001f',
            "chatbot-ai-terminal-v1",
            work.CanonicalKey,
            commandType,
            completionId));
        byte[] digest = SHA256.HashData(material);
        return Ulid.New(digest.AsSpan(0, 16)).ToString();
    }

    private async Task SubmitTerminalWithRetryAsync<TCommand>(
        AiExecutionWorkItem work,
        string messageId,
        string commandType,
        TCommand command,
        CancellationToken cancellationToken)
    {
        Exception? lastFailure = null;
        for (int attempt = 1; attempt <= MaximumTerminalSubmissionAttempts; attempt++)
        {
            try
            {
                TerminalSubmissionStarted(logger, work.Key, commandType, attempt);
                JsonElement payload = JsonSerializer.SerializeToElement(command);
                Dictionary<string, string> extensions = new(StringComparer.Ordinal)
                {
                    ["surfaceOrigin"] = "ai-execution-coordinator",
                    ["actorType"] = "system",
                    ["actorId"] = "ai-execution-coordinator",
                };
                if (admissionMarker is not null)
                {
                    extensions[DataProtectionChatBotAdmissionMarker.ExtensionKey] = admissionMarker.Create(
                        messageId,
                        work.TenantId,
                        work.ConversationId,
                        commandType,
                        payload,
                        work.CorrelationId,
                        "ai-execution-coordinator",
                        "ai-execution-coordinator",
                        null);
                }

                AiExecutionWorkItem? renewed = await workStore
                    .TryRenewLeaseAsync(work.Key, _owner, clock.UtcNow, LeaseDuration, cancellationToken)
                    .ConfigureAwait(false);
                if (renewed is null)
                {
                    return;
                }

                SubmitCommandRequest submit = new(
                    MessageId: messageId,
                    Tenant: work.TenantId,
                    Domain: ChatBotEventStore.DomainName,
                    AggregateId: work.ConversationId,
                    CommandType: commandType,
                    Payload: payload,
                    CorrelationId: work.CorrelationId,
                    Extensions: extensions);
                _ = await eventStore.SubmitCommandAsync(submit, cancellationToken).ConfigureAwait(false);
                // Transport acceptance is not terminal truth. The durable work remains leased/non-terminal until the
                // persisted terminal event returns through the named projection and RecordTerminalObservedAsync fences it.
                _ = _requestedCancellations.TryRemove(work.Key, out _);
                TerminalSubmissionCompleted(logger, work.Key, commandType, attempt);
                return;
            }
            catch (Exception ex) when (attempt < MaximumTerminalSubmissionAttempts)
            {
                lastFailure = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new TerminalSubmissionExhaustedException(
            $"Terminal AI execution command '{messageId}' exceeded its bounded submission retry budget.",
            lastFailure);
    }

    private static AiAssistanceProviderRequest ToProviderRequest(AiExecutionWorkItem work)
    {
        var execution = work.Execution;
        return new AiAssistanceProviderRequest(
            work.TenantId,
            work.ProjectId,
            execution.RequesterId,
            execution.ProposalId,
            execution.ExecutionId,
            AssistanceKindToken(execution.AssistanceKind),
            execution.ContextPackageId,
            execution.ContextPackageVersion,
            execution.ContextPackageRedactionState,
            execution.RetentionClass,
            execution.ProviderReuseSetting,
            execution.SourceEvidenceReferences,
            execution.AuthorizedContextReferences,
            execution.ExcludedContextReasons,
            execution.PolicySnapshotId ?? "unavailable",
            execution.RiskClassification?.ReasonCode ?? "low-risk-execution-allowed",
            execution.CorrelationId,
            $"audit:{execution.ExecutionId}");
    }

    private string CancellationId(AiExecutionWorkItem work)
        => work.CancellationId ??
            (_requestedCancellations.TryGetValue(work.Key, out string? cancellationId)
                ? cancellationId
                : throw new InvalidOperationException("The cancellation request identity is unavailable."));

    private static string AssistanceKindToken(LowRiskAiAssistanceKind kind)
        => kind switch
        {
            LowRiskAiAssistanceKind.SummarizeVisibleContext => "summarize-visible-context",
            LowRiskAiAssistanceKind.ExplainVisibleEvidence => "explain-visible-evidence",
            _ => "unsupported",
        };

    private sealed class TerminalSubmissionExhaustedException(string message, Exception? innerException)
        : InvalidOperationException(message, innerException);

    [LoggerMessage(EventId = 130201, Level = LogLevel.Error, Message = CoordinatorLoopFailedLog)]
    private static partial void CoordinatorLoopFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 130202, Level = LogLevel.Warning, Message = ExecutionFailedLog)]
    private static partial void ExecutionFailed(ILogger logger, Exception exception, string workKey, int attemptCount);

    [LoggerMessage(EventId = 130203, Level = LogLevel.Information, Message = ProviderCancellationObservedLog)]
    private static partial void ProviderCancellationObserved(ILogger logger, string workKey);

    [LoggerMessage(EventId = 130204, Level = LogLevel.Information, Message = TerminalSubmissionStartedLog)]
    private static partial void TerminalSubmissionStarted(ILogger logger, string workKey, string commandType, int attemptCount);

    [LoggerMessage(EventId = 130205, Level = LogLevel.Information, Message = TerminalSubmissionCompletedLog)]
    private static partial void TerminalSubmissionCompleted(ILogger logger, string workKey, string commandType, int attemptCount);
}
