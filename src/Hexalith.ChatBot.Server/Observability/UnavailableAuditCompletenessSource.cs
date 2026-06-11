namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Default audit-completeness source: reports <em>no</em> readings (Story 9.2, AC2 / Story 8.1 fail-safe doctrine).
/// When Story 8.7b's periodic enforcement runtime is disabled, the gauge must emit nothing rather than fabricate a
/// healthy <c>1.0</c> an operator could mistake for a measured pass. The enabled runtime swaps this for
/// <see cref="SweepBackedAuditCompletenessSource"/>.
/// </summary>
internal sealed class UnavailableAuditCompletenessSource : IAuditCompletenessSource
{
    public IReadOnlyList<AuditCompletenessReading> ReadCurrent() => [];
}
