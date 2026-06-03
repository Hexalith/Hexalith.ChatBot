using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.UI.State.OperationalDashboards;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Pure-reducer coverage for the operational-dashboards slice (Story 8.1).</summary>
public sealed class OperationalDashboardsReducersTests
{
    [Fact]
    public void ReduceLoadShouldMarkLoadingAndClearError()
    {
        OperationalDashboardsState state = new(IsLoading: false, Overview: null, Error: "dashboard-load-failed");

        OperationalDashboardsState next = OperationalDashboardsReducers.ReduceLoad(state);

        next.IsLoading.ShouldBeTrue();
        next.Error.ShouldBeNull();
    }

    [Fact]
    public void ReduceLoadedShouldStoreOverviewAndClearLoadingAndError()
    {
        OperationalDashboardsState state = new(IsLoading: true, Overview: null, Error: "dashboard-load-failed");
        OperationalDashboardOverview overview = Overview();

        OperationalDashboardsState next = OperationalDashboardsReducers.ReduceLoaded(state, new OperationalDashboardLoadedAction(overview));

        next.IsLoading.ShouldBeFalse();
        next.Overview.ShouldBe(overview);
        next.Error.ShouldBeNull();
    }

    [Fact]
    public void ReduceFailedShouldStoreSafeErrorAndPreserveAnyPriorOverview()
    {
        OperationalDashboardOverview overview = Overview();
        OperationalDashboardsState state = new(IsLoading: true, Overview: overview, Error: null);

        OperationalDashboardsState next = OperationalDashboardsReducers.ReduceFailed(state, new OperationalDashboardLoadFailedAction("authorization_denied"));

        next.IsLoading.ShouldBeFalse();
        next.Error.ShouldBe("authorization_denied");
        next.Overview.ShouldBe(overview);
    }

    private static OperationalDashboardOverview Overview()
    {
        DateTimeOffset now = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);
        List<OperationalDashboardView> views = [];
        foreach (DashboardObservabilityView view in DashboardObservabilityViews.All)
        {
            views.Add(new OperationalDashboardView(
                view,
                ChatBotHealthStatus.Healthy,
                Depth: 1,
                OldestItemAgeSeconds: 10,
                OwnerRole: "operations-admin",
                FreshnessTimestampUtc: now,
                FreshnessState: ChatBotFreshnessState.Fresh,
                DetailLinkState: OperationalDashboardContractValidator.DetailRequestAccess,
                DisabledDetailReasonCodes: ["insufficient-authority"]));
        }

        return new OperationalDashboardOverview(views, now, ChatBotFreshnessState.Fresh, "chatbot.operational-dashboard.v1", "correlation-alpha");
    }
}
