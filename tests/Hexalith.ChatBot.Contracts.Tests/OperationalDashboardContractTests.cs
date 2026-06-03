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

    [Fact]
    public static void DegradedViewMustCarryAffectedScopeAndNextSafeActionWhileHealthyViewMayLeaveThemNull()
    {
        OperationalDashboardOverview valid = Overview();

        // Story 8.5 AC4: a healthy view with null scope/next-action passes (the Overview() helper leaves all but
        // the degraded MailboxProcessing view null) — proving the requirement is degraded-only.
        OperationalDashboardContractValidator.IsValid(valid).ShouldBeTrue();

        OperationalDashboardView degraded = valid.Views.Single(v => v.View == DashboardObservabilityView.MailboxProcessing);

        // Missing affected scope on a degraded view fails overview validation (the synthetic-check observable).
        OperationalDashboardView missingScope = degraded with { AffectedScope = null };
        OperationalDashboardContractValidator
            .Validate(valid with { Views = [missingScope, .. valid.Views.Skip(1)] })
            .ShouldContain("degraded_affected_scope_missing");

        // Missing next safe action on a degraded view fails too.
        OperationalDashboardView missingAction = degraded with { NextSafeAction = null };
        OperationalDashboardContractValidator
            .Validate(valid with { Views = [missingAction, .. valid.Views.Skip(1)] })
            .ShouldContain("degraded_next_safe_action_missing");

        // A present-but-unsafe scope/next-action token fails the safe-token guard on any view.
        OperationalDashboardView unsafeScope = degraded with { AffectedScope = "bearer token" };
        OperationalDashboardContractValidator
            .Validate(valid with { Views = [unsafeScope, .. valid.Views.Skip(1)] })
            .ShouldContain("affected_scope_invalid");

        OperationalDashboardView unsafeAction = degraded with { NextSafeAction = "drop database" };
        OperationalDashboardContractValidator
            .Validate(valid with { Views = [unsafeAction, .. valid.Views.Skip(1)] })
            .ShouldContain("next_safe_action_invalid");

        // A failed view carries the same requirement as a degraded one.
        OperationalDashboardView failedMissing = degraded with { Health = ChatBotHealthStatus.Failed, AffectedScope = null };
        OperationalDashboardContractValidator
            .Validate(valid with { Views = [failedMissing, .. valid.Views.Skip(1)] })
            .ShouldContain("degraded_affected_scope_missing");
    }

    private static OperationalDashboardOverview Overview()
    {
        DateTimeOffset now = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);
        List<OperationalDashboardView> views = [];
        foreach (DashboardObservabilityView view in DashboardObservabilityViews.All)
        {
            bool degraded = view == DashboardObservabilityView.MailboxProcessing;
            views.Add(new OperationalDashboardView(
                view,
                degraded ? ChatBotHealthStatus.Degraded : ChatBotHealthStatus.Healthy,
                Depth: view == DashboardObservabilityView.AuditProjectionLag ? null : 3,
                OldestItemAgeSeconds: 120,
                OwnerRole: "operations-admin",
                FreshnessTimestampUtc: now,
                FreshnessState: ChatBotFreshnessState.Fresh,
                DetailLinkState: OperationalDashboardContractValidator.DetailRequestAccess,
                DisabledDetailReasonCodes: ["insufficient-authority"],
                LagIndicator: view == DashboardObservabilityView.AuditProjectionLag ? "lagging" : null,
                // NFR42: a degraded view must surface the affected scope + next safe action elements.
                AffectedScope: degraded ? "mailbox:mb-01" : null,
                ScopeKind: degraded ? "mailbox" : null,
                NextSafeAction: degraded ? "renew-graph-subscription" : null));
        }

        return new OperationalDashboardOverview(views, now, ChatBotFreshnessState.Fresh, "chatbot.operational-dashboard.v1", "correlation-alpha");
    }
}
