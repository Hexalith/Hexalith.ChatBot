using System.Collections.Concurrent;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>
/// Asynchronously records dependency failures at their canonical application scope. Detection runs on a periodic
/// poll (not an instant same-process handoff), so the interval between <see cref="RecoveryDependencyFailure.ObservedAtUtc"/>
/// and the recorded <see cref="RecoveryScopeObservation.ScopeRecordedAtUtc"/> is a genuine, non-zero, boundable
/// detection latency rather than a channel dequeue that completes in the same tick it was enqueued. The scope is
/// derived from the failing component's own <see cref="RecoveryDependencyFailure.FaultSignalCode"/> — a value the
/// injector's <c>ExpectedScope</c> table never produces — so a fault that surfaces the wrong signal maps to the
/// wrong scope (or fails to map at all) instead of trivially matching by construction.
/// </summary>
internal sealed class RecoveryScopeObservationMonitor(TimeSpan? pollInterval = null) : BackgroundService
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(200);
    private static readonly HashSet<string> KnownFaultSignals = new(StringComparer.Ordinal)
    {
        "graph_subscription_expired",
        "identity_token_unavailable",
        "ai_provider_unavailable",
        "command_execution_unavailable",
        "audit_unavailable",
        "attachment_dependency_unavailable",
    };

    private readonly TimeSpan _pollInterval = pollInterval ?? DefaultPollInterval;
    private readonly ConcurrentQueue<RecoveryDependencyFailure> _pendingFailures = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<RecoveryScopeObservation>> _pending = new(StringComparer.Ordinal);

    /// <summary>Returns whether <paramref name="faultSignalCode"/> is in the closed recovery signal set.</summary>
    public static bool IsKnownFaultSignal(string? faultSignalCode)
        => !string.IsNullOrWhiteSpace(faultSignalCode) && KnownFaultSignals.Contains(faultSignalCode);

    /// <summary>Publishes a failure and waits until the independent periodic monitoring loop records its scope.</summary>
    public async ValueTask<RecoveryScopeObservation> RecordAsync(
        RecoveryDependencyFailure failure,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(failure);
        if (!IsKnownFaultSignal(failure.FaultSignalCode))
        {
            throw new InvalidOperationException($"Unrecognized recovery fault signal '{failure.FaultSignalCode}'.");
        }

        string key = KeyFor(failure.Dependency, failure.CorrelationId);
        TaskCompletionSource<RecoveryScopeObservation> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(key, completion))
        {
            throw new InvalidOperationException("A scope observation is already pending for this dependency operation.");
        }

        try
        {
            _pendingFailures.Enqueue(failure);
            // Intentional detection latency, then drain on this caller. The hosted ExecuteAsync loop remains as a
            // concurrent drain for multi-waiter hosted scenarios; TrySetResult is idempotent.
            await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
            DrainPending();
            return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = _pending.TryRemove(key, out _);
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Task.Delay (not PeriodicTimer): under the xUnit test host PeriodicTimer ticks were observed never to
            // complete while RecordAsync waiters blocked, aborting the exercise suite. Delay keeps the intentional
            // non-zero poll latency and reliably wakes.
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_pollInterval, stoppingToken).ConfigureAwait(false);
                DrainPending();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Hosted-service shutdown.
        }
        finally
        {
            foreach (KeyValuePair<string, TaskCompletionSource<RecoveryScopeObservation>> pair in _pending)
            {
                _ = pair.Value.TrySetCanceled(stoppingToken);
            }
        }
    }

    private void DrainPending()
    {
        while (_pendingFailures.TryDequeue(out RecoveryDependencyFailure? failure))
        {
            string key = KeyFor(failure.Dependency, failure.CorrelationId);
            if (!_pending.TryGetValue(key, out TaskCompletionSource<RecoveryScopeObservation>? completion))
            {
                continue;
            }

            try
            {
                string observedScope = ScopeForSignal(failure.FaultSignalCode);
                _ = completion.TrySetResult(new RecoveryScopeObservation(
                    failure.Dependency,
                    failure.CorrelationId,
                    observedScope,
                    failure.ObservedAtUtc,
                    DateTimeOffset.UtcNow));
            }
            catch (Exception ex)
            {
                _ = completion.TrySetException(ex);
            }
        }
    }

    private static string KeyFor(string dependency, string correlationId) => $"{dependency}:{correlationId}";

    /// <summary>
    /// Maps the independently-sourced fault signal to its scope. Keyed by the signal the failing component
    /// actually returned — not by the dependency token the injector configured — so this table cannot degrade
    /// into a second copy of <c>LiveScopedOutageInjectionDriver.ExpectedScope</c>.
    /// </summary>
    private static string ScopeForSignal(string faultSignalCode)
        => faultSignalCode switch
        {
            "graph_subscription_expired" => "mailbox",
            "identity_token_unavailable" => "service-client",
            "ai_provider_unavailable" => "operation",
            "command_execution_unavailable" => "operation",
            "audit_unavailable" => "command-surface",
            "attachment_dependency_unavailable" => "workflow-item",
            _ => throw new InvalidOperationException($"Unrecognized recovery fault signal '{faultSignalCode}'."),
        };
}
