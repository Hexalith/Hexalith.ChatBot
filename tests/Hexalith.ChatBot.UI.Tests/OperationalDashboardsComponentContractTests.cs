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

        // Dense rows reflow to labelled rows on small screens.
        page.ShouldContain("chatbot-labelled-row-list");
        page.ShouldContain("chatbot-definition-list");

        // Stable machine tokens for view/health/freshness exposed as data attributes.
        page.ShouldContain("data-chatbot-dashboard-view");
        page.ShouldContain("data-chatbot-freshness");

        // No restricted detail and no premature completion language.
        page.ShouldNotContain("Project Alpha");
        page.ShouldNotContain("EvidenceContent");
        page.ShouldNotContain("MailboxSubject");
        page.ShouldNotContain("secret", Case.Insensitive);
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
