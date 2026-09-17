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
