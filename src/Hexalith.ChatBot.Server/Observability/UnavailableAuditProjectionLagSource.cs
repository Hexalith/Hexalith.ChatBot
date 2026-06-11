namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Default audit-projection-lag source: reports <em>no</em> readings (Story 8.2, AC8 / Story 8.1 fail-safe
/// doctrine). At M0/M1 there is no wired per-tenant audit checkpoint feed, so the gauge must emit nothing rather
/// than fabricate a healthy <c>0</c> an operator could act on. When Story 8.7b's periodic enforcement runtime is
/// enabled, DI swaps this for <see cref="CheckpointBackedAuditProjectionLagSource"/>.
/// </summary>
internal sealed class UnavailableAuditProjectionLagSource : IAuditProjectionLagSource
{
    public IReadOnlyList<AuditProjectionLagReading> ReadCurrent() => [];
}
