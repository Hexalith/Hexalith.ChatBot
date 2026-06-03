namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Default audit-completeness source: reports <em>no</em> readings (Story 9.2, AC2 / Story 8.1 fail-safe doctrine).
/// The periodic completeness sweep (<see cref="Audit.AuditCompletenessMeasurer.MeasureAllTenantsAsync"/>) is not wired
/// to a runtime cadence yet (the inert-control-floor deferral), so the gauge must emit nothing rather than fabricate a
/// healthy <c>1.0</c> an operator could mistake for a measured pass. A real sweep-backed source is the follow-up swap
/// registered in DI; until then this keeps the gauge wired and observable while exporting no fabricated value.
/// </summary>
internal sealed class UnavailableAuditCompletenessSource : IAuditCompletenessSource
{
    public IReadOnlyList<AuditCompletenessReading> ReadCurrent() => [];
}
