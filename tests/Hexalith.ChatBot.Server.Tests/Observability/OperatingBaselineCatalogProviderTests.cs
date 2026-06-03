using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Observability;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Observability;

/// <summary>
/// Story 8.3 AC1/AC2/AC8: the published-SLO catalog provider returns one entry per required NFR42a metric with all
/// seven addendum fields populated as safe tokens, carries the NFR-documented initial targets, uses
/// <c>calibration-pending</c>/<c>a11-pending</c> wherever no starter number exists (never a fabricated value), and
/// the whole catalog passes the contract validator with a default fail-safe Unknown burn.
/// </summary>
public sealed class OperatingBaselineCatalogProviderTests
{
    [Fact]
    public void CatalogShouldCoverEveryRequiredNfr42aMetricExactlyOnce()
    {
        IReadOnlyList<PublishedSlo> catalog = OperatingBaselineCatalogProvider.GetCatalog();

        string[] metricNames = catalog.Select(static slo => slo.MetricName).ToArray();
        metricNames.ShouldBeUnique();
        foreach (string required in OperatingBaselineMetrics.Required)
        {
            metricNames.ShouldContain(required);
        }
    }

    [Fact]
    public void EveryEntryShouldPopulateAllSevenFieldsWithSafeTokensAndPassTheValidator()
    {
        IReadOnlyList<PublishedSlo> catalog = OperatingBaselineCatalogProvider.GetCatalog();

        OperatingBaselineContractValidator.IsValid(catalog).ShouldBeTrue();
        catalog.ShouldAllBe(slo => OperationalDashboardContractValidator.IsRequiredSafeToken(slo.MetricName));
        catalog.ShouldAllBe(slo => OperationalDashboardContractValidator.IsRequiredSafeToken(slo.Target));
        catalog.ShouldAllBe(slo => OperationalDashboardContractValidator.IsRequiredSafeToken(slo.MeasurementWindow));
        catalog.ShouldAllBe(slo => OperationalDashboardContractValidator.IsRequiredSafeToken(slo.ErrorBudget));
        catalog.ShouldAllBe(slo => OperationalDashboardContractValidator.IsRequiredSafeToken(slo.AlertThreshold));
        catalog.ShouldAllBe(slo => OperationalDashboardContractValidator.IsRequiredSafeToken(slo.CalibrationSource));
        catalog.ShouldAllBe(slo => OperationalDashboardContractValidator.IsRequiredSafeToken(slo.TenantScope));
        // Static catalog default is the fail-safe Unknown burn — the projector layers live burn over wired SLOs.
        catalog.ShouldAllBe(slo => slo.BurnState == ErrorBudgetBurnState.Unknown);
    }

    [Theory]
    [InlineData(OperatingBaselineMetrics.CommandExecutionLatency, "p95-le-2000ms", "nfr24")]
    [InlineData(OperatingBaselineMetrics.AssociationLatency, "p95-le-10000ms", "nfr25")]
    [InlineData(OperatingBaselineMetrics.OperationIdentityLatency, "p95-le-5000ms", "nfr26")]
    [InlineData(OperatingBaselineMetrics.CorrectionPropagationLatency, "p95-le-10m", "nfr17a")]
    [InlineData(OperatingBaselineMetrics.AuditProjectionLag, "p95-le-5m", "nfr43")]
    [InlineData(OperatingBaselineMetrics.ApprovalQueueAge, "p95-le-2-business-days", "nfr43")]
    [InlineData(OperatingBaselineMetrics.MailboxSubscriptionExpiry, "expiry-le-7d", "nfr43")]
    public void DocumentedDefaultsShouldCarryTheNfrTargetAndCalibrationSource(string metric, string target, string calibrationSource)
    {
        PublishedSlo slo = SloFor(metric);
        slo.Target.ShouldBe(target);
        slo.CalibrationSource.ShouldBe(calibrationSource);
        slo.Target.ShouldNotBe(OperatingBaselineContractValidator.CalibrationPending);
    }

    [Theory]
    [InlineData(OperatingBaselineMetrics.IngestionLatency)]
    [InlineData(OperatingBaselineMetrics.AmbiguousResolutionTime)]
    [InlineData(OperatingBaselineMetrics.DuplicateSuppressionRate)]
    [InlineData(OperatingBaselineMetrics.MailboxFailureRate)]
    [InlineData(OperatingBaselineMetrics.AiMediationLatency)]
    public void UndocumentedSlosShouldUseCalibrationPendingAndA11PendingNeverAFabricatedValue(string metric)
    {
        PublishedSlo slo = SloFor(metric);
        slo.Target.ShouldBe(OperatingBaselineContractValidator.CalibrationPending);
        slo.CalibrationSource.ShouldBe(OperatingBaselineContractValidator.A11Pending);
    }

    // AC2: the NFR43 alert thresholds (audit-projection-lag > 5 min, retry exhaustion, approval items older than 2
    // business days, subscription expiry within 7 days) must be published as the alert-threshold field — not just the
    // target/calibration-source. Each NFR43 SLO also carries its documented measurement window.
    [Theory]
    [InlineData(OperatingBaselineMetrics.AuditProjectionLag, "lag-gt-5m", "rolling-24h")]
    [InlineData(OperatingBaselineMetrics.RetryExhaustionRate, "any-exhaustion", "rolling-24h")]
    [InlineData(OperatingBaselineMetrics.ApprovalQueueAge, "age-gt-2-business-days", "rolling-7d")]
    [InlineData(OperatingBaselineMetrics.MailboxSubscriptionExpiry, "expiry-le-7d", "rolling-7d")]
    public void Nfr43SlosShouldPublishTheDocumentedAlertThresholdAndMeasurementWindow(string metric, string alertThreshold, string window)
    {
        PublishedSlo slo = SloFor(metric);
        slo.AlertThreshold.ShouldBe(alertThreshold);
        slo.MeasurementWindow.ShouldBe(window);
        slo.CalibrationSource.ShouldBe("nfr43");
    }

    // AC2: the audit-projection-lag SLO is the only catalog entry with a documented error budget — the evaluator's
    // degraded@100 / failed@1000 event bands. Every other SLO publishes calibration-pending (never a fabricated
    // budget fraction such as 0.1%) because the PRD documents no starter budget.
    [Fact]
    public void AuditProjectionLagShouldPublishTheDocumentedErrorBudgetBandsWhileOthersStayCalibrationPending()
    {
        SloFor(OperatingBaselineMetrics.AuditProjectionLag).ErrorBudget.ShouldBe("degraded-100ev-failed-1000ev");

        OperatingBaselineCatalogProvider.GetCatalog()
            .Where(slo => slo.MetricName != OperatingBaselineMetrics.AuditProjectionLag)
            .ShouldAllBe(slo => slo.ErrorBudget == OperatingBaselineContractValidator.CalibrationPending);
    }

    // AC1: every published SLO carries platform-default tenant scope (no per-tenant override fabricated at M2).
    [Fact]
    public void EverySloShouldPublishThePlatformDefaultTenantScope()
        => OperatingBaselineCatalogProvider.GetCatalog().ShouldAllBe(slo => slo.TenantScope == "platform-default");

    private static PublishedSlo SloFor(string metric)
        => OperatingBaselineCatalogProvider.GetCatalog().Single(slo => slo.MetricName == metric);
}
