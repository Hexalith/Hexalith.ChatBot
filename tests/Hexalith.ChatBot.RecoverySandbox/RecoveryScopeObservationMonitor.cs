using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Asynchronously records dependency failures at their canonical application scope.</summary>
internal sealed class RecoveryScopeObservationMonitor : BackgroundService
{
    private readonly Channel<RecoveryDependencyFailure> _failures = Channel.CreateUnbounded<RecoveryDependencyFailure>();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<RecoveryScopeObservation>> _pending = new(StringComparer.Ordinal);

    /// <summary>Publishes a failure and waits until the independent monitoring loop records its scope.</summary>
    public async ValueTask<RecoveryScopeObservation> RecordAsync(
        RecoveryDependencyFailure failure,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(failure);
        string key = KeyFor(failure.Dependency, failure.CorrelationId);
        TaskCompletionSource<RecoveryScopeObservation> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(key, completion))
        {
            throw new InvalidOperationException("A scope observation is already pending for this dependency operation.");
        }

        try
        {
            await _failures.Writer.WriteAsync(failure, cancellationToken).ConfigureAwait(false);
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
        await foreach (RecoveryDependencyFailure failure in _failures.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            string key = KeyFor(failure.Dependency, failure.CorrelationId);
            if (_pending.TryGetValue(key, out TaskCompletionSource<RecoveryScopeObservation>? completion))
            {
                _ = completion.TrySetResult(new RecoveryScopeObservation(
                    failure.Dependency,
                    failure.CorrelationId,
                    ScopeFor(failure.Dependency),
                    failure.ObservedAtUtc,
                    DateTimeOffset.UtcNow));
            }
        }
    }

    private static string KeyFor(string dependency, string correlationId) => $"{dependency}:{correlationId}";

    private static string ScopeFor(string dependency)
        => dependency switch
        {
            "graph" => "mailbox",
            "identity" => "service-client",
            "ai-provider" => "operation",
            "command-execution" => "operation",
            "audit-store" => "command-surface",
            "attachment-processing" => "workflow-item",
            _ => throw new InvalidOperationException("The dependency has no monitored scope."),
        };
}
