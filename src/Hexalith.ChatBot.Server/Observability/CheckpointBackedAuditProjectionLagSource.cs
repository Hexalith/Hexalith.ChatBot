namespace Hexalith.ChatBot.Server.Observability;

internal sealed class CheckpointBackedAuditProjectionLagSource : IAuditProjectionLagSource
{
    private readonly Lock _gate = new();
    private IReadOnlyList<AuditProjectionLagReading> _readings = [];

    public IReadOnlyList<AuditProjectionLagReading> ReadCurrent()
    {
        lock (_gate)
        {
            return _readings;
        }
    }

    public void Publish(IReadOnlyList<AuditProjectionCheckpoint> checkpoints)
    {
        ArgumentNullException.ThrowIfNull(checkpoints);
        AuditProjectionLagReading[] readings = checkpoints
            .Where(static checkpoint => checkpoint.LastProjectedPosition is not null && checkpoint.LatestCommittedPosition is not null)
            .Select(static checkpoint => new AuditProjectionLagReading(
                checkpoint.TenantId,
                checkpoint.LastProjectedPosition,
                checkpoint.LatestCommittedPosition,
                checkpoint.SnapshotUtc))
            .OrderBy(static reading => reading.TenantId, StringComparer.Ordinal)
            .ToArray();

        lock (_gate)
        {
            _readings = readings;
        }
    }
}
