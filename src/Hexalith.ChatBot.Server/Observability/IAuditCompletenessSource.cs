namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// A single coarse, metadata-only audit-completeness reading for one tenant (Story 9.2, AC2/NFR50a). It carries only
/// the tenant ref and the already-computed reconstructable fraction over the rolling 7-day window — never operation
/// ids, counts that could become high-cardinality dimensions, or any item content. When the tenant's measurement
/// could not complete, <see cref="IsMeasurable"/> is <see langword="false"/> and the gauge reports <em>no</em>
/// measurement (fail-safe), never a fabricated 1.0.
/// </summary>
internal sealed record AuditCompletenessReading(
    string TenantId,
    bool IsMeasurable,
    double Fraction);

/// <summary>
/// Read-only seam the completeness observable gauge polls during metric collection. It exposes only the coarse
/// per-tenant fraction; the gauge emits it read-only and never mutates state. The default implementation
/// (<see cref="UnavailableAuditCompletenessSource"/>) reports nothing until Story 8.7b's periodic enforcement runtime
/// swaps in <see cref="SweepBackedAuditCompletenessSource"/> and publishes measured sweeps — honouring the Story 8.1
/// fail-safe doctrine of preferring no-data over a fabricated value.
/// </summary>
internal interface IAuditCompletenessSource
{
    /// <summary>Returns the current per-tenant completeness readings, or an empty list when no measurement is available.</summary>
    IReadOnlyList<AuditCompletenessReading> ReadCurrent();
}
