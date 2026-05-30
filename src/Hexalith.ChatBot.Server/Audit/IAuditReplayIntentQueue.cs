namespace Hexalith.ChatBot.Server.Audit;

internal interface IAuditReplayIntentQueue
{
    ValueTask EnqueueAsync(AuditReplayIntent intent, CancellationToken cancellationToken);
}
