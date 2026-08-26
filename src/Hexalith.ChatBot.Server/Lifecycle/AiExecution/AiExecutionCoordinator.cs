using System.Collections.Concurrent;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Adapters.AiProvider;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Conversations;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal sealed partial class AiExecutionCoordinator(
    IAiExecutionWorkStore workStore,
    IAiAssistanceProvider provider,
    IEventStoreGatewayClient eventStore,
    ISystemClock clock,
    ILogger<AiExecutionCoordinator> logger) : BackgroundService, IAiExecutionCoordinator
{
    private const int MaximumConcurrentExecutions = 4;
    private const int MaximumTerminalSubmissionAttempts = 3;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan LeaseHeartbeatInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeExecutions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _requestedCancellations = new(StringComparer.Ordinal);
    private readonly string _owner = $"{Environment.MachineName}:{Guid.NewGuid():N}";

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
            // This method runs on the inbound EventStore projection callback. Cancel() may execute provider
            // continuations inline; a continuation that submits the terminal command back to EventStore would then
            // wait on the very projection callback it is blocking. Schedule token callbacks asynchronously so the
            // persisted cancellation request can return before the terminal relay begins.
            _ = active.CancelAsync();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                IReadOnlyList<AiExecutionWorkItem> runnable = await workStore
                    .ListRunnableAsync(clock.UtcNow, MaximumConcurrentExecutions * 4, stoppingToken)
                    .ConfigureAwait(false);
                List<Task> batch = [];
                foreach (AiExecutionWorkItem candidate in runnable)
                {
                    if (batch.Count >= MaximumConcurrentExecutions)
                    {
                        break;
                    }

                    AiExecutionWorkItem? claimed = await workStore
                        .TryClaimAsync(candidate.Key, _owner, clock.UtcNow, LeaseDuration, stoppingToken)
                        .ConfigureAwait(false);
                    if (claimed is not null)
                    {
                        batch.Add(ProcessClaimedAsync(claimed, stoppingToken));
                    }
                }

                if (batch.Count > 0)
                {
                    await Task.WhenAll(batch).ConfigureAwait(false);
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
                record = await provider.ExecuteAsync(ToProviderRequest(work), executionCancellation.Token).ConfigureAwait(false);
                await workStore
                    .MarkCompletionPendingAsync(work.Key, _owner, record, clock.UtcNow, stoppingToken)
                    .ConfigureAwait(false);
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
        catch (Exception ex)
        {
            ExecutionFailed(logger, ex, work.Key, work.AttemptCount);
            await workStore.ReleaseAsync(work.Key, _owner, clock.UtcNow, stoppingToken).ConfigureAwait(false);
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
            command.CompletionId,
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
            command.CompletionId,
            nameof(CompleteAiResponseGenerationCancellation),
            command,
            cancellationToken).ConfigureAwait(false);
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
                SubmitCommandRequest submit = new(
                    MessageId: messageId,
                    Tenant: work.TenantId,
                    Domain: ChatBotEventStore.DomainName,
                    AggregateId: work.ConversationId,
                    CommandType: commandType,
                    Payload: JsonSerializer.SerializeToElement(command),
                    CorrelationId: work.CorrelationId,
                    Extensions: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["surfaceOrigin"] = "ai-execution-coordinator",
                        ["actorType"] = "system",
                    });
                _ = await eventStore.SubmitCommandAsync(submit, cancellationToken).ConfigureAwait(false);
                await workStore.MarkTerminalAsync(work.Key, _owner, clock.UtcNow, cancellationToken).ConfigureAwait(false);
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

        throw new InvalidOperationException(
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

    [LoggerMessage(EventId = 130201, Level = LogLevel.Error, Message = "AI execution coordinator loop failed.")]
    private static partial void CoordinatorLoopFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 130202, Level = LogLevel.Warning, Message = "AI execution work {WorkKey} attempt {AttemptCount} failed and was released for recovery.")]
    private static partial void ExecutionFailed(ILogger logger, Exception exception, string workKey, int attemptCount);

    [LoggerMessage(EventId = 130203, Level = LogLevel.Information, Message = "AI execution work {WorkKey} observed provider cancellation.")]
    private static partial void ProviderCancellationObserved(ILogger logger, string workKey);

    [LoggerMessage(EventId = 130204, Level = LogLevel.Information, Message = "AI execution work {WorkKey} is submitting terminal command {CommandType}, attempt {AttemptCount}.")]
    private static partial void TerminalSubmissionStarted(ILogger logger, string workKey, string commandType, int attemptCount);

    [LoggerMessage(EventId = 130205, Level = LogLevel.Information, Message = "AI execution work {WorkKey} completed terminal command {CommandType}, attempt {AttemptCount}.")]
    private static partial void TerminalSubmissionCompleted(ILogger logger, string workKey, string commandType, int attemptCount);
}
