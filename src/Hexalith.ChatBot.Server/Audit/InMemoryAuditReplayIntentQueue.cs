namespace Hexalith.ChatBot.Server.Audit;

internal sealed class InMemoryAuditReplayIntentQueue : IAuditReplayIntentQueue
{
    private readonly Lock _gate = new();
    private readonly List<AuditReplayIntent> _intents = [];

    public IReadOnlyList<AuditReplayIntent> Intents
    {
        get
        {
            lock (_gate)
            {
                return [.. _intents];
            }
        }
    }

    public IReadOnlyList<AuditReplayIntent> Snapshot() => Intents;

    public ValueTask AcknowledgeAsync(AuditReplayIntent intent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _intents.Remove(intent);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask EnqueueAsync(AuditReplayIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        lock (_gate)
        {
            _intents.Add(intent);
        }

        return ValueTask.CompletedTask;
    }
}
