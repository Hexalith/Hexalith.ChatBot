using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Contract coverage for the Story 8.3 published-SLO catalog (AC1/AC2/AC5/AC8): stable burn-state wire tokens, the
/// finite-token catalog validator (safe tokens, no duplicate/missing required SLO, defined burn enum), the canonical
/// catalog's documented-vs-calibration-pending targets, metadata-only serialization, and the extended
/// operational-dashboard overview validator now validating the rider catalog.
/// </summary>
public static class OperatingBaselineContractTests
{
    [Theory]
    [InlineData(ErrorBudgetBurnState.Unknown, "unknown")]
    [InlineData(ErrorBudgetBurnState.WithinBudget, "within-budget")]
    [InlineData(ErrorBudgetBurnState.Approaching, "approaching")]
    [InlineData(ErrorBudgetBurnState.Exhausted, "exhausted")]
    public static void BurnStateWireTokensShouldRoundTrip(ErrorBudgetBurnState state, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        ErrorBudgetBurnStates.ToWireValue(state).ShouldBe(token);
        ErrorBudgetBurnStates.TryFromWireValue(token, out ErrorBudgetBurnState parsed).ShouldBeTrue();
        parsed.ShouldBe(state);
    }

    [Fact]
    public static void DefaultBurnStateShouldBeUnknownSoADefaultedValueIsHonestNoData()
        => default(ErrorBudgetBurnState).ShouldBe(ErrorBudgetBurnState.Unknown);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("burned-out")]
    [InlineData("within budget")]
    public static void TryFromWireValueShouldRejectUnknownTokensAndFailSafeToUnknown(string? token)
    {
        ErrorBudgetBurnStates.TryFromWireValue(token, out ErrorBudgetBurnState parsed).ShouldBeFalse();
        parsed.ShouldBe(ErrorBudgetBurnState.Unknown);
    }

    [Fact]
    public static void ToWireValueShouldThrowForAnUndefinedBurnStateRatherThanEmitAFabricatedToken()
        => Should.Throw<ArgumentOutOfRangeException>(() => ErrorBudgetBurnStates.ToWireValue((ErrorBudgetBurnState)999));

    [Fact]
    public static void AllShouldEnumerateExactlyTheDefinedBurnStates()
        => ErrorBudgetBurnStates.All.ShouldBe(Enum.GetValues<ErrorBudgetBurnState>(), ignoreOrder: true);

    [Fact]
    public static void CanonicalCatalogShouldCoverEveryRequiredMetricAndValidate()
    {
        OperatingBaselineContractValidator.IsValid(OperatingBaselineCatalog.Published).ShouldBeTrue();

        string[] names = OperatingBaselineCatalog.Published.Select(static slo => slo.MetricName).ToArray();
        names.ShouldBeUnique();
        foreach (string required in OperatingBaselineMetrics.Required)
        {
            names.ShouldContain(required);
        }
    }

    [Fact]
    public static void CatalogValidatorShouldRejectUnsafeTokensDuplicatesMissingRequiredAndUndefinedBurn()
    {
        PublishedSlo valid = Slo("chatbot.sample.metric");

        // Unsafe / secret-bearing tokens are rejected on every field.
        OperatingBaselineContractValidator.Validate(valid with { Target = "bearer token" }).ShouldContain("target_invalid");
        OperatingBaselineContractValidator.Validate(valid with { MetricName = "p95<=2000ms" }).ShouldContain("metric_name_invalid");
        OperatingBaselineContractValidator.Validate(valid with { CalibrationSource = "secret-source" }).ShouldContain("calibration_source_invalid");

        // Undefined burn enum is rejected.
        OperatingBaselineContractValidator.Validate(valid with { BurnState = (ErrorBudgetBurnState)999 }).ShouldContain("burn_state_invalid");

        // Duplicate metric names are rejected at catalog level.
        OperatingBaselineContractValidator.Validate(new[] { valid, valid }).ShouldContain("slo_duplicate");

        // A catalog missing a required metric is rejected.
        OperatingBaselineContractValidator.Validate(new[] { valid }).ShouldContain("slo_missing");
    }

    [Fact]
    public static void CatalogValidatorShouldRecordANullEntryAsAnErrorRatherThanThrow()
        => OperatingBaselineContractValidator.Validate(new PublishedSlo?[] { null }!).ShouldContain("slo_invalid");

    [Fact]
    public static void OverviewValidatorShouldValidateTheRiderCatalogButPermitItsAbsence()
    {
        OperationalDashboardOverview baseOverview = Overview(publishedSlos: null);

        // Absent catalog is permitted (additive field) — the Story 8.1 overview shape stays valid.
        OperationalDashboardContractValidator.IsValid(baseOverview).ShouldBeTrue();

        // A present, fully-covering catalog is valid.
        OperationalDashboardContractValidator.IsValid(Overview(OperatingBaselineCatalog.Published)).ShouldBeTrue();

        // A present catalog with an unsafe token surfaces through the overview validator.
        PublishedSlo[] tampered = [.. OperatingBaselineCatalog.Published.Skip(1), Slo("chatbot.sample.metric") with { Target = "bearer token" }];
        OperationalDashboardContractValidator.Validate(Overview(tampered)).ShouldContain("target_invalid");
    }

    [Fact]
    public static void PublishedSlosShouldSerializeAsStableTokensAndStayMetadataOnly()
    {
        string json = JsonSerializer.Serialize(OperatingBaselineCatalog.Published, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("chatbot.audit.projection.lag");
        json.ShouldContain("calibration-pending");
        json.ShouldContain("a11-pending");
        json.ShouldContain("unknown"); // default fail-safe burn token

        json.ShouldNotContain("bearer", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("password", Case.Insensitive);
    }

    private static PublishedSlo Slo(string metricName)
        => new(metricName, "calibration-pending", "rolling-24h", "calibration-pending", "budget-burn", "a11-pending", "platform-default", ErrorBudgetBurnState.Unknown);

    private static OperationalDashboardOverview Overview(IReadOnlyList<PublishedSlo>? publishedSlos)
    {
        DateTimeOffset now = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);
        List<OperationalDashboardView> views = [];
        foreach (DashboardObservabilityView view in DashboardObservabilityViews.All)
        {
            views.Add(new OperationalDashboardView(
                view,
                ChatBotHealthStatus.Healthy,
                Depth: view == DashboardObservabilityView.AuditProjectionLag ? null : 3,
                OldestItemAgeSeconds: 120,
                OwnerRole: "operations-admin",
                FreshnessTimestampUtc: now,
                FreshnessState: ChatBotFreshnessState.Fresh,
                DetailLinkState: OperationalDashboardContractValidator.DetailRequestAccess,
                DisabledDetailReasonCodes: ["insufficient-authority"],
                LagIndicator: view == DashboardObservabilityView.AuditProjectionLag ? "lagging" : null));
        }

        return new OperationalDashboardOverview(views, now, ChatBotFreshnessState.Fresh, "chatbot.operational-dashboard.v1", "correlation-alpha", publishedSlos);
    }
}
