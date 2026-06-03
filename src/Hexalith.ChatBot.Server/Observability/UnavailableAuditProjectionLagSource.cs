namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Default audit-projection-lag source: reports <em>no</em> readings (Story 8.2, AC8 / Story 8.1 fail-safe
/// doctrine). At M0/M1 there is no wired per-tenant audit checkpoint feed, so the gauge must emit nothing rather
/// than fabricate a healthy <c>0</c> an operator could act on. A real checkpoint-backed source is a follow-up swap
/// registered in DI; until then this keeps the gauge wired and observable while exporting no fabricated value.
/// </summary>
internal sealed class UnavailableAuditProjectionLagSource : IAuditProjectionLagSource
{
    public IReadOnlyList<AuditProjectionLagReading> ReadCurrent() => [];
}
