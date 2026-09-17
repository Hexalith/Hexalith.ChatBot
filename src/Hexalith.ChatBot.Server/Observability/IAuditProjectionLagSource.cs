namespace Hexalith.ChatBot.Server.Observability;

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
