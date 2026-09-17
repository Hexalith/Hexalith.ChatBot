using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

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
