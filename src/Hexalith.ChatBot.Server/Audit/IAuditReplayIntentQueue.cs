namespace Hexalith.ChatBot.Server.Audit;

internal interface IAuditReplayIntentQueue
{
    ValueTask EnqueueAsync(AuditReplayIntent intent, CancellationToken cancellationToken);

    /// <summary>Returns the intents currently retained for operator or automatic reconciliation.</summary>
    IReadOnlyList<AuditReplayIntent> Snapshot() => [];

    /// <summary>Acknowledges only an intent whose outcome was successfully reconciled.</summary>
    ValueTask AcknowledgeAsync(AuditReplayIntent intent, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
