namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// A single coarse, metadata-only audit-projection-lag reading for one tenant (Story 8.2, AC8). It carries only
/// the checkpoint positions the <see cref="Projections.AuditProjectionLagEvaluator"/> already consumes plus the
/// snapshot instant — never audit envelope contents, reasons, hash-chain detail, or redaction keys. When the
/// checkpoint source cannot be trusted (positions unavailable) the positions are null and the evaluator yields a
/// fail-safe <c>Unknown</c>/no-data status, which the gauge reports as *no measurement* rather than a fabricated 0.
/// </summary>
internal sealed record AuditProjectionLagReading(
    string TenantId,
    long? LastProjectedPosition,
    long? LatestCommittedPosition,
    DateTimeOffset SnapshotUtc);

/// <summary>
/// Read-only seam the audit-projection-lag observable gauge polls during metric collection. It exposes only the
/// coarse checkpoint positions per tenant; the gauge derives the lag through
/// <see cref="Projections.AuditProjectionLagEvaluator"/> and never mutates any state. The default production
/// implementation (<see cref="UnavailableAuditProjectionLagSource"/>) reports nothing until a real per-tenant
/// audit checkpoint feed is wired, honouring the Story 8.1 fail-safe doctrine of preferring no-data over a
/// fabricated value.
/// </summary>
internal interface IAuditProjectionLagSource
{
    /// <summary>Returns the current per-tenant lag readings, or an empty list when no checkpoint data is available.</summary>
    IReadOnlyList<AuditProjectionLagReading> ReadCurrent();
}
