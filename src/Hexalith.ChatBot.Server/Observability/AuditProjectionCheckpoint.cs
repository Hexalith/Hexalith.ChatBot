namespace Hexalith.ChatBot.Server.Observability;

internal sealed record AuditProjectionCheckpoint(
    string TenantId,
    long? LastProjectedPosition,
    long? LatestCommittedPosition,
    DateTimeOffset SnapshotUtc);
