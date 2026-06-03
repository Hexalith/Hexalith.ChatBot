using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// A single published operational SLO from the NFR42a catalog (Story 8.3, addendum §Operating Baselines). Every
/// field is a bounded, low-cardinality, ASCII-safe stable token — never a raw percentile, event count, restricted
/// tenant/project detail, secret, or PII. Where the PRD documents no starter number the token is
/// <c>calibration-pending</c> (target/error-budget) with a <c>a11-pending</c> <see cref="CalibrationSource"/>,
/// never a fabricated value (A11; mirrors the Story 8.1/8.2 prefer-no-data doctrine). <see cref="BurnState"/> is the
/// current coarse, fail-safe error-budget burn — <see cref="ErrorBudgetBurnState.Unknown"/> when its live signal is
/// not wired.
/// </summary>
/// <param name="MetricName">The stable SLO metric identifier (aligned to the Story 8.2 instrument name where one exists).</param>
/// <param name="Target">The target token (e.g. <c>p95-le-2000ms</c>) or <c>calibration-pending</c>.</param>
/// <param name="MeasurementWindow">The measurement-window token (e.g. <c>rolling-24h</c>).</param>
/// <param name="ErrorBudget">The error-budget token (e.g. <c>degraded-100ev-failed-1000ev</c>) or <c>calibration-pending</c>.</param>
/// <param name="AlertThreshold">The token for the alert threshold that consumes the budget (e.g. <c>lag-gt-5m</c>).</param>
/// <param name="CalibrationSource">The NFR/A11 origin of the target (e.g. <c>nfr24</c>, <c>a11-pending</c>).</param>
/// <param name="TenantScope">The tenant scope token (<c>platform-default</c> or a per-tenant override token).</param>
/// <param name="BurnState">The current coarse, fail-safe error-budget burn state.</param>
public sealed record PublishedSlo(
    string MetricName,
    string Target,
    string MeasurementWindow,
    string ErrorBudget,
    string AlertThreshold,
    string CalibrationSource,
    string TenantScope,
    ErrorBudgetBurnState BurnState);

/// <summary>
/// Stable metric-name tokens for the NFR42a published-SLO catalog (Story 8.3). Names are aligned to the Story 8.2
/// instrument names where one exists and use a stable, unique dotted token where no 8.2 instrument is yet emitted.
/// <see cref="Required"/> is the closed at-minimum set the catalog must publish (AC1) — every entry must be present
/// with no duplicates.
/// </summary>
public static class OperatingBaselineMetrics
{
    public const string IngestionLatency = "chatbot.ingestion.latency";
    public const string AssociationLatency = "chatbot.association.latency";
    public const string AmbiguousResolutionTime = "chatbot.ambiguous.resolution.time";
    public const string CommandExecutionLatency = "chatbot.command.execution.latency";
    public const string OperationIdentityLatency = "chatbot.operation.identity.latency";
    public const string AuditProjectionLag = "chatbot.audit.projection.lag";
    public const string RetryExhaustionRate = "chatbot.retry.exhausted";
    public const string DuplicateSuppressionRate = "chatbot.duplicate.suppressed";
    public const string MailboxFailureRate = "chatbot.mailbox.failure.rate";
    public const string ApprovalQueueAge = "chatbot.approval.queue.age";
    public const string AiMediationLatency = "chatbot.ai.mediation.latency";
    public const string CorrectionPropagationLatency = "chatbot.correction.propagation.latency";
    public const string MailboxSubscriptionExpiry = "chatbot.mailbox.subscription.expiry";

    /// <summary>The closed at-minimum set of metric names the published catalog must cover (NFR42a / AC1).</summary>
    public static IReadOnlyList<string> Required { get; } =
    [
        IngestionLatency,
        AssociationLatency,
        AmbiguousResolutionTime,
        CommandExecutionLatency,
        OperationIdentityLatency,
        AuditProjectionLag,
        RetryExhaustionRate,
        DuplicateSuppressionRate,
        MailboxFailureRate,
        ApprovalQueueAge,
        AiMediationLatency,
        CorrectionPropagationLatency,
        MailboxSubscriptionExpiry,
    ];
}

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

/// <summary>
/// Finite-token validator for the published-SLO catalog (Story 8.3). It enforces required safe tokens on every
/// field (reusing the operational-dashboard ASCII/marker-ban posture), a defined <see cref="ErrorBudgetBurnState"/>,
/// no duplicate metric names, and full coverage of the <see cref="OperatingBaselineMetrics.Required"/> set. It
/// carries no business logic and never inspects restricted detail.
/// </summary>
public static class OperatingBaselineContractValidator
{
    /// <summary>The sentinel token published wherever the PRD documents no starter number (A11).</summary>
    public const string CalibrationPending = "calibration-pending";

    /// <summary>The calibration-source token published wherever the target awaits the A11 baseline run.</summary>
    public const string A11Pending = "a11-pending";

    public static IReadOnlyList<string> Validate(PublishedSlo slo)
    {
        ArgumentNullException.ThrowIfNull(slo);

        List<string> errors = [];
        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(slo.MetricName))
        {
            errors.Add("metric_name_invalid");
        }

        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(slo.Target))
        {
            errors.Add("target_invalid");
        }

        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(slo.MeasurementWindow))
        {
            errors.Add("measurement_window_invalid");
        }

        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(slo.ErrorBudget))
        {
            errors.Add("error_budget_invalid");
        }

        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(slo.AlertThreshold))
        {
            errors.Add("alert_threshold_invalid");
        }

        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(slo.CalibrationSource))
        {
            errors.Add("calibration_source_invalid");
        }

        if (!OperationalDashboardContractValidator.IsRequiredSafeToken(slo.TenantScope))
        {
            errors.Add("tenant_scope_invalid");
        }

        if (!Enum.IsDefined(slo.BurnState))
        {
            errors.Add("burn_state_invalid");
        }

        return errors;
    }

    public static IReadOnlyList<string> Validate(IReadOnlyList<PublishedSlo> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        List<string> errors = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (PublishedSlo slo in catalog)
        {
            if (slo is null)
            {
                errors.Add("slo_invalid");
                continue;
            }

            errors.AddRange(Validate(slo));
            if (!seen.Add(slo.MetricName))
            {
                errors.Add("slo_duplicate");
            }
        }

        foreach (string required in OperatingBaselineMetrics.Required)
        {
            if (!seen.Contains(required))
            {
                errors.Add("slo_missing");
            }
        }

        return errors;
    }

    public static bool IsValid(PublishedSlo slo)
        => Validate(slo).Count == 0;

    public static bool IsValid(IReadOnlyList<PublishedSlo> catalog)
        => Validate(catalog).Count == 0;
}
