using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Component-composition contract for the read-only operational dashboards page (Story 8.1, AC1–AC6): it hosts in
/// the governed shell, renders non-color status + freshness through governed primitives, keeps detail links
/// reachable with an explainable restricted/disabled state, reflows dense rows to labelled rows, localizes visible
/// text, refreshes within the staleness window, and never surfaces restricted detail or a premature "Done".
/// </summary>
public sealed class OperationalDashboardsComponentContractTests
{
    [Fact]
    public void DashboardPageShouldUseGovernedPrimitivesNonColorStatusFreshnessAndReachableDetail()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor");

        page.ShouldContain("@page \"/operational-dashboards\"");
        page.ShouldContain("ChatBotConversationShell");
        page.ShouldContain("ChatBotProjectContextHeader");
        page.ShouldContain("ChatBotStatusBanner");
        page.ShouldContain("ChatBotGovernedAction");

        // Refresh-within-staleness affordance + load on init.
        page.ShouldContain("LoadOperationalDashboardAction");
        page.ShouldContain("OperationalDashboardsRefreshAction");

        // Non-color status: a localized health label and freshness label accompany the semantic color.
        page.ShouldContain("HealthLabel");
        page.ShouldContain("FreshnessLabel");
        page.ShouldContain("OperationalDashboardsFreshnessTimestampLabel");

        // Detail link stays reachable with an explainable restricted/disabled reason (no silent disappearance).
        page.ShouldContain("ChatBotGovernedActionState.DisabledWithReason");
        page.ShouldContain("OperationalDashboardsDetailRestrictedReason");

        // Story 13.5: primary observability data renders through a Fluent data grid + FluentCard KPI/status tiles
        // (with non-color FluentBadge status cues), not a monospace <dl class="chatbot-definition-list"> dump.
        page.ShouldContain("<FluentDataGrid");
        page.ShouldContain("<FluentCard");
        page.ShouldContain("<FluentBadge");
        page.ShouldNotContain("chatbot-definition-list");

        // Stable machine tokens for view/health/freshness exposed as data attributes (carried on the sibling
        // per-row markers that accompany the grid).
        page.ShouldContain("data-chatbot-dashboard-view");
        page.ShouldContain("data-chatbot-freshness");

        // No restricted detail and no premature completion language.
        page.ShouldNotContain("Project Alpha");
        page.ShouldNotContain("EvidenceContent");
        page.ShouldNotContain("MailboxSubject");
        page.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public void DashboardPageShouldRenderTheDegradedFourElementSurfaceWithLocalizedLabelsAndWcagParity()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor");

        // Story 8.5 AC4: a degraded/failed view surfaces the two NFR42 elements beyond state+owner — the affected
        // scope and the next safe action — through localized keys, in the same labelled-row (WCAG-parity) markup.
        page.ShouldContain("OperationalDashboardsAffectedScopeLabel");
        page.ShouldContain("OperationalDashboardsNextSafeActionLabel");
        page.ShouldContain("view.AffectedScope is { } affectedScope");
        page.ShouldContain("view.NextSafeAction is { } nextSafeAction");

        // Stable machine tokens for the two new elements exposed as data attributes.
        page.ShouldContain("data-chatbot-affected-scope");
        page.ShouldContain("data-chatbot-next-safe-action");

        // The new labels exist in both localization resources (English + French), like the OwnerRole label.
        string english = ReadProjectFile("src/Hexalith.ChatBot.UI/Localization/SharedResource.resx");
        string french = ReadProjectFile("src/Hexalith.ChatBot.UI/Localization/SharedResource.fr.resx");
        english.ShouldContain("OperationalDashboards_AffectedScope_Label");
        english.ShouldContain("OperationalDashboards_NextSafeAction_Label");
        french.ShouldContain("OperationalDashboards_AffectedScope_Label");
        french.ShouldContain("OperationalDashboards_NextSafeAction_Label");
    }

    [Fact]
    public void DashboardPageShouldRenderTheMetadataOnlyPublishedSloSectionWithCoarseBurnTokens()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor");

        // Story 8.3 AC3: a metadata-only "Published SLOs / Error budgets" section iterates the rider catalog.
        page.ShouldContain("OperationalDashboardsSlosTitle");
        page.ShouldContain("overview.PublishedSlos");
        page.ShouldContain("PublishedSlo slo in publishedSlos");

        // Each SLO renders its seven addendum fields plus the coarse burn through localized keys.
        page.ShouldContain("OperationalDashboardsSloMetricNameLabel");
        page.ShouldContain("OperationalDashboardsSloTargetLabel");
        page.ShouldContain("OperationalDashboardsSloCalibrationSourceLabel");
        page.ShouldContain("OperationalDashboardsSloBurnLabel");
        page.ShouldContain("BurnLabel");

        // Stable machine tokens for metric/burn exposed as data attributes; no raw percentile/count surfaced.
        page.ShouldContain("data-chatbot-slo-metric");
        page.ShouldContain("data-chatbot-slo-burn");
        page.ShouldContain("ErrorBudgetBurnStates.ToWireValue");
    }

    [Fact]
    public void DashboardPageShouldLocalizeEveryVisibleStringThroughTypedKeys()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/OperationalDashboards.razor");

        page.ShouldContain("ChatBotUiTextKey.OperationalDashboardsTitle");
        page.ShouldContain("ChatBotUiTextKey.OperationalDashboardsViewMailboxProcessing");
        page.ShouldContain("ChatBotUiTextKey.OperationalDashboardsViewAuditProjectionLag");
        page.ShouldContain("ChatBotUiTextKey.OperationalDashboardsHealthHealthy");
        page.ShouldContain("ChatBotUiTextKey.OperationalDashboardsFreshnessExpired");

        // Visible text comes from the localizer, not hard-coded English.
        page.ShouldNotContain(">Operational dashboards<");
        page.ShouldNotContain(">Mailbox processing<");
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(ProjectPath(relativePath));

    private static string ProjectPath(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return Path.Combine(directory.FullName, relativePath);
    }
}
