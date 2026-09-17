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
