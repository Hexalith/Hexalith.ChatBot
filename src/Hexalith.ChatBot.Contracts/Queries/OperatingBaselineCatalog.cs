using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// The finite, bounded NFR42a published-SLO catalog (Story 8.3) — the single source of truth mirrored into
/// addendum §Operating Baselines (AC6). One stable entry per required metric, each carrying the seven addendum
/// fields as low-cardinality ASCII-safe tokens plus a default fail-safe <see cref="ErrorBudgetBurnState.Unknown"/>
/// burn (the dashboard projector layers the live burn over wired SLOs).
/// <para>
/// It lives in <c>.Contracts</c> (not <c>.Server</c>) because it is pure, low-cardinality contract data consumed by
/// both the server-side projector and the UI placeholder — and the UI depends only on the client/contract surface,
/// never on <c>.Server</c>. Initial targets come <b>only</b> from documented MVP defaults (NFR24/NFR25/NFR26/NFR17a
/// and the NFR43 alert thresholds); every SLO without a documented starter number publishes <c>calibration-pending</c>
/// with an <c>a11-pending</c> calibration source — never a fabricated value (A11; Story 8.1/8.2 doctrine).
/// </para>
/// </summary>
public static class OperatingBaselineCatalog
{
    private const string Rolling24h = "rolling-24h";
    private const string Rolling7d = "rolling-7d";
    private const string Pending = OperatingBaselineContractValidator.CalibrationPending;
    private const string A11 = OperatingBaselineContractValidator.A11Pending;
    private const string PlatformDefault = "platform-default";
    private const string BudgetBurn = "budget-burn";

    /// <summary>The published catalog with each SLO's default fail-safe <see cref="ErrorBudgetBurnState.Unknown"/> burn.</summary>
    public static IReadOnlyList<PublishedSlo> Published { get; } =
    [
        // NFR-documented starter targets. Tokens are ASCII-safe (no <, >, =, %): "le" = ≤, "gt" = &gt;.
        Slo(OperatingBaselineMetrics.CommandExecutionLatency, "p95-le-2000ms", Rolling24h, Pending, BudgetBurn, "nfr24"),
        Slo(OperatingBaselineMetrics.AssociationLatency, "p95-le-10000ms", Rolling24h, Pending, BudgetBurn, "nfr25"),
        Slo(OperatingBaselineMetrics.OperationIdentityLatency, "p95-le-5000ms", Rolling24h, Pending, BudgetBurn, "nfr26"),
        Slo(OperatingBaselineMetrics.CorrectionPropagationLatency, "p95-le-10m", Rolling24h, Pending, BudgetBurn, "nfr17a"),
        Slo(OperatingBaselineMetrics.AuditProjectionLag, "p95-le-5m", Rolling24h, "degraded-100ev-failed-1000ev", "lag-gt-5m", "nfr43"),
        Slo(OperatingBaselineMetrics.RetryExhaustionRate, "on-exhaustion", Rolling24h, Pending, "any-exhaustion", "nfr43"),
        Slo(OperatingBaselineMetrics.ApprovalQueueAge, "p95-le-2-business-days", Rolling7d, Pending, "age-gt-2-business-days", "nfr43"),
        Slo(OperatingBaselineMetrics.MailboxSubscriptionExpiry, "expiry-le-7d", Rolling7d, Pending, "expiry-le-7d", "nfr43"),

        // No documented starter number — calibration-pending / a11-pending (never fabricated).
        Slo(OperatingBaselineMetrics.IngestionLatency, Pending, Rolling24h, Pending, BudgetBurn, A11),
        Slo(OperatingBaselineMetrics.AmbiguousResolutionTime, Pending, Rolling7d, Pending, BudgetBurn, A11),
        Slo(OperatingBaselineMetrics.DuplicateSuppressionRate, Pending, Rolling24h, Pending, "spike-baseline", A11),
        Slo(OperatingBaselineMetrics.MailboxFailureRate, Pending, Rolling24h, Pending, BudgetBurn, A11),
        Slo(OperatingBaselineMetrics.AiMediationLatency, Pending, Rolling24h, Pending, BudgetBurn, A11),
    ];

    private static PublishedSlo Slo(
        string metricName,
        string target,
        string window,
        string errorBudget,
        string alertThreshold,
        string calibrationSource)
        => new(metricName, target, window, errorBudget, alertThreshold, calibrationSource, PlatformDefault, ErrorBudgetBurnState.Unknown);
}
