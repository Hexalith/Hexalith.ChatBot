namespace Hexalith.ChatBot.Server.Audit;

internal interface IAuditReplayIntentQueue
{
    ValueTask EnqueueAsync(AuditReplayIntent intent, CancellationToken cancellationToken);

    /// <summary>Returns the intents currently retained for operator or automatic reconciliation.</summary>
    IReadOnlyList<AuditReplayIntent> Snapshot() => [];
}
