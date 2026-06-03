using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Contract coverage for the read-only operational dashboard query/DTOs (Story 8.1, AC1/AC2/AC3/AC9/AC10):
/// stable view + freshness wire tokens, bounded-staleness classification, finite-token validation, full FR67
/// view coverage, status-as-enum (never count-derived), and metadata-only serialization.
/// </summary>
public static class OperationalDashboardContractTests
{
    [Theory]
    [InlineData(DashboardObservabilityView.MailboxProcessing, "mailbox-processing")]
    [InlineData(DashboardObservabilityView.FailedAssociations, "failed-associations")]
    [InlineData(DashboardObservabilityView.ApprovalQueues, "approval-queues")]
    [InlineData(DashboardObservabilityView.DuplicateHandling, "duplicate-handling")]
    [InlineData(DashboardObservabilityView.AiActionOutcomes, "ai-action-outcomes")]
    [InlineData(DashboardObservabilityView.AuditProjectionLag, "audit-projection-lag")]
    public static void DashboardViewWireTokensShouldRoundTrip(DashboardObservabilityView view, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        DashboardObservabilityViews.ToWireValue(view).ShouldBe(token);
        DashboardObservabilityViews.TryFromWireValue(token, out DashboardObservabilityView parsed).ShouldBeTrue();
        parsed.ShouldBe(view);
        DashboardObservabilityViews.TryFromWireValue($" {token.ToUpperInvariant()} ", out parsed).ShouldBeTrue();
        parsed.ShouldBe(view);
    }

    [Theory]
    [InlineData(ChatBotFreshnessState.Fresh, "fresh")]
    [InlineData(ChatBotFreshnessState.Stale, "stale")]
    [InlineData(ChatBotFreshnessState.Expired, "expired")]
    public static void FreshnessStateWireTokensShouldRoundTrip(ChatBotFreshnessState state, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        ChatBotFreshnessStates.ToWireValue(state).ShouldBe(token);
        ChatBotFreshnessStates.TryFromWireValue(token, out ChatBotFreshnessState parsed).ShouldBeTrue();
        parsed.ShouldBe(state);
    }

    [Fact]
    public static void FreshnessPolicyShouldClassifyWithinBoundedStalenessWindows()
    {
        DateTimeOffset now = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);

        OperationalDashboardFreshnessPolicy.Classify(now.AddMinutes(-4), now).ShouldBe(ChatBotFreshnessState.Fresh);
        OperationalDashboardFreshnessPolicy.Classify(now.AddMinutes(-5), now).ShouldBe(ChatBotFreshnessState.Fresh);
        OperationalDashboardFreshnessPolicy.Classify(now.AddMinutes(-10), now).ShouldBe(ChatBotFreshnessState.Stale);
        OperationalDashboardFreshnessPolicy.Classify(now.AddMinutes(-20), now).ShouldBe(ChatBotFreshnessState.Expired);
        OperationalDashboardFreshnessPolicy.Classify(now.AddMinutes(1), now).ShouldBe(ChatBotFreshnessState.Fresh);
    }

    [Fact]
    public static void QueryValidatorShouldAcceptSafeReadAndRejectUnsafeInputs()
    {
        OperationalDashboardContractValidator.IsValid(new GetOperationalDashboard(AdminScope.SeeOnly, "correlation-alpha", 100)).ShouldBeTrue();

        OperationalDashboardContractValidator.Validate(new GetOperationalDashboard(AdminScope.SeeOnly, "bearer token", 100))
            .ShouldContain("correlation_id_invalid");
        OperationalDashboardContractValidator.Validate(new GetOperationalDashboard(AdminScope.SeeOnly, "correlation-alpha", 0))
            .ShouldContain("aggregation_limit_invalid");
        OperationalDashboardContractValidator.Validate(new GetOperationalDashboard(AdminScope.SeeOnly, "correlation-alpha", 5000))
            .ShouldContain("aggregation_limit_invalid");
        OperationalDashboardContractValidator.Validate(new GetOperationalDashboard((AdminScope)999, "correlation-alpha", 100))
            .ShouldContain("scope_invalid");
    }

    [Fact]
    public static void OverviewValidatorShouldRequireFullViewCoverageUtcFreshnessAndSafeTokens()
    {
        OperationalDashboardOverview valid = Overview();
        OperationalDashboardContractValidator.IsValid(valid).ShouldBeTrue();

        OperationalDashboardOverview missingView = valid with { Views = valid.Views.Take(5).ToArray() };
        OperationalDashboardContractValidator.Validate(missingView).ShouldContain("view_missing");

        OperationalDashboardOverview duplicateView = valid with { Views = [.. valid.Views, valid.Views[0]] };
        OperationalDashboardContractValidator.Validate(duplicateView).ShouldContain("view_duplicate");

        OperationalDashboardOverview notUtc = valid with { FreshnessTimestampUtc = new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.FromHours(2)) };
        OperationalDashboardContractValidator.Validate(notUtc).ShouldContain("freshness_not_utc");

        OperationalDashboardView badDetail = valid.Views[0] with { DetailLinkState = "open-everything" };
        OperationalDashboardOverview badDetailOverview = valid with { Views = [badDetail, .. valid.Views.Skip(1)] };
        OperationalDashboardContractValidator.Validate(badDetailOverview).ShouldContain("detail_link_state_invalid");
    }

    [Fact]
    public static void OverviewShouldSerializeStatusAsEnumStringAndStayMetadataOnly()
    {
        string json = JsonSerializer.Serialize(Overview(), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Status is the stable health enum token, never a count-derived label.
        json.ShouldContain("mailbox-processing");
        json.ShouldContain("degraded");
        json.ShouldContain("audit-projection-lag");
        json.ShouldContain("fresh");

        json.ShouldNotContain("bearer", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("password", Case.Insensitive);
    }

    private static OperationalDashboardOverview Overview()
    {
        DateTimeOffset now = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);
        List<OperationalDashboardView> views = [];
        foreach (DashboardObservabilityView view in DashboardObservabilityViews.All)
        {
            views.Add(new OperationalDashboardView(
                view,
                view == DashboardObservabilityView.MailboxProcessing ? ChatBotHealthStatus.Degraded : ChatBotHealthStatus.Healthy,
                Depth: view == DashboardObservabilityView.AuditProjectionLag ? null : 3,
                OldestItemAgeSeconds: 120,
                OwnerRole: "operations-admin",
                FreshnessTimestampUtc: now,
                FreshnessState: ChatBotFreshnessState.Fresh,
                DetailLinkState: OperationalDashboardContractValidator.DetailRequestAccess,
                DisabledDetailReasonCodes: ["insufficient-authority"],
                LagIndicator: view == DashboardObservabilityView.AuditProjectionLag ? "lagging" : null));
        }

        return new OperationalDashboardOverview(views, now, ChatBotFreshnessState.Fresh, "chatbot.operational-dashboard.v1", "correlation-alpha");
    }
}
