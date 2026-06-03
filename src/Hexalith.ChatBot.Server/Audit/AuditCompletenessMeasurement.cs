namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The metadata-only result of one per-tenant audit-completeness measurement over a rolling 7-day window (Story 9.2,
/// AC2/NFR50a). It carries only safe bounded tokens — the tenant ref, the coarse reconstructable fraction, the window
/// bounds, and a safe first-diverging-operation locator — never raw item content, prompts, recipient PII, or payloads.
/// <para>
/// Fail-safe (Epic 8 no-fabrication spine): when the measurement <b>cannot complete</b> — the chain or projection is
/// unavailable, or enumeration/diff throws — <see cref="IsMeasurable"/> is <see langword="false"/> and the fraction is
/// meaningless; this is reported as a breach signal (mirroring <see cref="WormAuditChainVerificationResult"/> Unknown),
/// never a fabricated 1.0. A completed window with zero in-scope operations is genuinely vacuously complete
/// (<see cref="IsMeasurable"/> true, fraction 1.0) — distinct from "cannot complete".
/// </para>
/// </summary>
internal sealed record AuditCompletenessMeasurement(
    string TenantRef,
    bool IsMeasurable,
    int ReconstructableCount,
    int TotalCount,
    double Fraction,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string? FirstDivergingOperationLocator,
    string ReasonCode)
{
    /// <summary>The NFR50a target: ≥ 99.5% of in-scope state-mutating operations must be reconstructable per rolling 7-day window per tenant.</summary>
    public const double CompletenessTargetFraction = 0.995;

    /// <summary>The rolling window NFR50a measures over.</summary>
    public static readonly TimeSpan RollingWindow = TimeSpan.FromDays(7);

    /// <summary>Reason code for a completed measurement.</summary>
    public const string MeasuredReasonCode = "completeness_measured";

    /// <summary>Reason code for a measurement that could not complete (chain/projection unavailable or threw) — a breach.</summary>
    public const string UnmeasurableReasonCode = "completeness_unmeasurable";

    /// <summary>True when the measurement completed (even if below target). A false value is a fail-safe breach signal.</summary>
    public bool IsBreach => !IsMeasurable;

    /// <summary>Builds the fail-safe unmeasurable result for a tenant whose chain/projection could not be read.</summary>
    public static AuditCompletenessMeasurement Unmeasurable(string tenantRef, DateTimeOffset windowStartUtc, DateTimeOffset windowEndUtc)
        => new(
            tenantRef,
            IsMeasurable: false,
            ReconstructableCount: 0,
            TotalCount: 0,
            Fraction: 0.0,
            windowStartUtc,
            windowEndUtc,
            FirstDivergingOperationLocator: null,
            UnmeasurableReasonCode);
}

/// <summary>The result of a completeness sweep across every tenant chain: how many tenants were measured, breached the budget, and were unmeasurable.</summary>
internal sealed record AuditCompletenessSweepOutcome(int TenantsMeasured, int Breaches, int Unmeasurable);
