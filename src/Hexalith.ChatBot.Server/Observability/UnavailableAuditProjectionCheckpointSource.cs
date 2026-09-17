namespace Hexalith.ChatBot.Server.Observability;

internal sealed class UnavailableAuditProjectionCheckpointSource : IAuditProjectionCheckpointSource
{
    public ValueTask<IReadOnlyList<AuditProjectionCheckpoint>> ReadCheckpointsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<AuditProjectionCheckpoint>>([]);
    }
}
