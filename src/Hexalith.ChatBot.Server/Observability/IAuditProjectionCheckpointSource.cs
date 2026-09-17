namespace Hexalith.ChatBot.Server.Observability;

internal interface IAuditProjectionCheckpointSource
{
    ValueTask<IReadOnlyList<AuditProjectionCheckpoint>> ReadCheckpointsAsync(CancellationToken cancellationToken);
}
