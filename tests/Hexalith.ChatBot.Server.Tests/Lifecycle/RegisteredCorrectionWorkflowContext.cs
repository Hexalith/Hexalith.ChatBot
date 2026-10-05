using Dapr.Workflow;
using Dapr.Workflow.Abstractions;
using Dapr.Workflow.Worker;

using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

/// <summary>Controls time and failures while executing the registered production workflow/activity bridge.</summary>
internal sealed class RegisteredCorrectionWorkflowContext : WorkflowContext, ISystemClock, IMemoriesCaseResolver, ICorrectionPropagationWorkflowRuntime
{
    public IServiceProvider Services { get; set; } = null!;
    public IWorkflowsFactory Factory { get; set; } = null!;
    public TaskCompletionSource Timer { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ScheduledStatus { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ActiveAttempt { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<string> ResolvedCase { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 9, 10, 0, 0, TimeSpan.Zero);
    public DateTimeOffset? TimerDueAt { get; private set; }
    public CorrectionPropagationWorkflowProgress? Progress { get; private set; }
    public int ResolutionCalls { get; private set; }
    public bool AlwaysFailResolution { get; init; }
    public bool AutoAdvanceTimers { get; init; }
    public WorkflowTaskOptions? ResolverRetryOptions { get; private set; }
    public bool FailScheduledPublication { get; init; }
    public bool FailActivePublication { get; init; }
    public bool PendingStoreCycles { get; init; }
    public bool FailLaterScheduledPublication { get; init; }
    public TaskCompletionSource SecondSchedule { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ThirdStore { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource StoreCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _scheduledPublications;
    private int _storeCalls;
    public override string Name => nameof(CorrectionPropagationWorkflow);
    public override string InstanceId => "wf-production-bridge";
    public override DateTime CurrentUtcDateTime => UtcNow.AddHours(-3).UtcDateTime;
    public override bool IsReplaying => false;
    public bool IsAvailable => true;
    public bool HasAuthoritativeProgress => true;

    public ValueTask ScheduleAsync(CorrectionPropagationRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    public ValueTask<CorrectionPropagationWorkflowRuntimeStatus> CheckAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    public ValueTask<CorrectionPropagationWorkflowProgress?> ReadProgressAsync(string instanceId, CancellationToken cancellationToken) => ValueTask.FromResult(Progress);

    public ValueTask<string> ResolveCaseIdAsync(string tenantId, string projectId, string correlationId, CancellationToken cancellationToken = default)
    {
        ResolutionCalls++;
        if (AlwaysFailResolution || ResolutionCalls == 1)
        {
            throw new InvalidOperationException("Injected owner unavailable.");
        }

        ActiveAttempt.TrySetResult();
        return new ValueTask<string>(ResolvedCase.Task);
    }

    public override async Task<T> CallActivityAsync<T>(string name, object? input = null, WorkflowTaskOptions? options = null)
    {
        if (name == nameof(CorrectionPropagationResolveCaseActivity)) { ResolverRetryOptions = options; }
        if (name == nameof(CorrectionPropagationRetryStatusActivity) && input is CorrectionPropagationRetryStatusInput status)
        {
            if (status.WorkflowStatus == CorrectionPropagationWorkflowStatuses.Retrying)
            {
                _scheduledPublications++;
                if (_scheduledPublications == 2)
                {
                    SecondSchedule.TrySetResult();
                }
            }
            if (status.WorkflowStatus == CorrectionPropagationWorkflowStatuses.Retrying &&
                (FailScheduledPublication || FailLaterScheduledPublication && _scheduledPublications > 1) ||
                status.WorkflowStatus == CorrectionPropagationWorkflowStatuses.Started && FailActivePublication)
            {
                ScheduledStatus.TrySetResult();
                throw new IOException("Injected status activity exhaustion.");
            }
        }

        if (PendingStoreCycles && name == nameof(CorrectionPropagationRunStoreActivity) && input is CorrectionPropagationStoreActivityInput store)
        {
            _storeCalls++;
            if (_storeCalls <= 2)
            {
                return (T)(object)new CorrectionPropagationActivityResult(store.StoreKey, "awaiting-completion",
                    CorrectionPropagationWorkflowFailureCodes.StoreUnavailable, UtcNow, "remote-operation");
            }
            ThirdStore.TrySetResult();
            await StoreCompletion.Task.ConfigureAwait(false);
            return (T)(object)new CorrectionPropagationActivityResult(store.StoreKey, "success", null, UtcNow);
        }

        if (name is nameof(CorrectionPropagationRetryStatusActivity) or nameof(CorrectionPropagationClockActivity) or nameof(CorrectionPropagationResolveCaseActivity))
        {
            if (!Factory.TryCreateActivity(new TaskIdentifier(name), Services, out var activity, out Exception? activationFailure))
            {
                throw new InvalidOperationException("Production activity registration or activation is missing.", activationFailure);
            }

            object? result = await activity!.RunAsync(null!, input).ConfigureAwait(false);
            if (name == nameof(CorrectionPropagationRetryStatusActivity))
            {
                ScheduledStatus.TrySetResult();
            }
            return (T)result!;
        }

        return name == nameof(CorrectionPropagationScopeActivity)
            ? (T)(object)(PendingStoreCycles ? new[] { "store-1" } : Array.Empty<string>())
            : (T)(object)true;
    }

    public override Task CreateTimer(DateTime fireAt, CancellationToken cancellationToken)
    {
        if (Timer.Task.IsCompleted)
        {
            Timer = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        TimerDueAt = new DateTimeOffset(fireAt, TimeSpan.Zero);
        if (AutoAdvanceTimers) { UtcNow = TimerDueAt.Value; Timer.TrySetResult(); }
        return Timer.Task;
    }

    public override void SetCustomStatus(object? customStatus) => Progress = (CorrectionPropagationWorkflowProgress?)customStatus;
    public override Task<T> CallChildWorkflowAsync<T>(string workflowName, object? input = null, ChildWorkflowTaskOptions? options = null) => throw new NotSupportedException();
    public override Task<T> WaitForExternalEventAsync<T>(string eventName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public override void ContinueAsNew(object? newInput = null, bool preserveUnprocessedEvents = true) => throw new NotSupportedException();
    public override Guid NewGuid() => throw new NotSupportedException();
    public override ILogger CreateReplaySafeLogger(string categoryName) => NullLogger.Instance;
    public override ILogger CreateReplaySafeLogger<T>() => NullLogger.Instance;
    public override ILogger CreateReplaySafeLogger(Type type) => NullLogger.Instance;
    public override bool IsPatched(string patchId) => false;
    public override PropagatedHistory? GetPropagatedHistory() => null;
    public override void SendEvent(string instanceId, string eventName, object eventData) => throw new NotSupportedException();
}
