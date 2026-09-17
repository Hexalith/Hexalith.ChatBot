namespace Hexalith.ChatBot.Server.Observability;

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
