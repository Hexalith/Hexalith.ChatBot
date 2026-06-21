using System.Text.RegularExpressions;

using Hexalith.ChatBot.UI.Design;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Proves the responsive and touch foundation is a typed governed UI contract, not only CSS comments.
/// </summary>
public sealed class ChatBotResponsiveTouchContractTests
{
    private static readonly string[] RequiredViewportTiers = ["Phone", "Tablet", "Desktop"];

    private static readonly string[] SafetyCriticalDenseLabels =
    [
        "Project",
        "Actor",
        "Risk",
        "State",
        "Confidence",
        "Time",
        "Reason",
        "Next action",
    ];

    [Fact]
    public void ViewportContractShouldExposeOrderedWebNativeTiersWithoutCliOrMcpBreakpoints()
    {
        Enum.GetNames<ChatBotViewportTier>().ShouldBe(RequiredViewportTiers, ignoreOrder: false);

        IReadOnlyList<ChatBotResponsiveSurfaceCapability> capabilities = ChatBotResponsiveSurfaceCapabilityContract.All;
        capabilities.Select(static capability => capability.Tier.ToString()).ShouldBe(RequiredViewportTiers, ignoreOrder: false);
        capabilities.Select(static capability => capability.MinimumWidthCssPixels).ShouldBe([0, 600, 900], ignoreOrder: false);

        ChatBotResponsiveSurfaceCapability phone = ChatBotResponsiveSurfaceCapabilityContract.Get(ChatBotViewportTier.Phone);
        phone.SupportsTriage.ShouldBeTrue();
        phone.SupportsFullWorkflow.ShouldBeFalse();
        phone.RequiresSafetyCriticalStateVisible.ShouldBeTrue();
        phone.RequiredBehavior.ShouldContain("triage", Case.Insensitive);

        ChatBotResponsiveSurfaceCapability tablet = ChatBotResponsiveSurfaceCapabilityContract.Get(ChatBotViewportTier.Tablet);
        tablet.AllowsStackedPanels.ShouldBeTrue();
        tablet.SupportsFullWorkflow.ShouldBeTrue();

        ChatBotResponsiveSurfaceCapability desktop = ChatBotResponsiveSurfaceCapabilityContract.Get(ChatBotViewportTier.Desktop);
        desktop.SupportsFullWorkflow.ShouldBeTrue();
        desktop.AllowsTwoColumnShell.ShouldBeTrue();

        string joinedContract = string.Join(' ', capabilities.Select(static capability => capability.RequiredBehavior));
        joinedContract.ShouldNotContain("CLI", Case.Insensitive);
        joinedContract.ShouldNotContain("MCP", Case.Insensitive);
    }

    [Fact]
    public void SmallScreenFallbackContractShouldRequireSummaryStatusSafeActionsAndReachableHandoff()
    {
        ChatBotSmallScreenFallbackContract valid = ChatBotSmallScreenFallbackContract.CreatePhoneLimited(
            ReadOnlySummary: "Ambiguous association review summary",
            CurrentStatus: "Blocked until reviewer decides",
            SafeActions: ["Approve", "Reject", "Defer", "Confirm"],
            HandoffLinkLabel: "Copy handoff link",
            LargerScreenGuidance: "Open on a larger screen to edit dense fields.",
            PreservedStateMarker: "draft-filter-state-preserved",
            ReachableExplanation: "Editing is disabled on phone because the table is too dense.");

        valid.IsComplete.ShouldBeTrue();
        valid.SafeActions.ShouldContain("Approve");
        valid.HandoffLinkLabel.ShouldBe("Copy handoff link");
        valid.PreservedStateMarker.ShouldContain("filter", Case.Insensitive);
        valid.ReachableExplanation.ShouldNotContain("tooltip", Case.Insensitive);

        (valid with { ReadOnlySummary = string.Empty }).IsComplete.ShouldBeFalse();
        (valid with { CurrentStatus = string.Empty }).IsComplete.ShouldBeFalse();
        (valid with { SafeActions = [] }).IsComplete.ShouldBeFalse();
        (valid with { SafeActions = null! }).IsComplete.ShouldBeFalse();
        (valid with { HandoffLinkLabel = string.Empty }).IsComplete.ShouldBeFalse();
        (valid with { LargerScreenGuidance = string.Empty }).IsComplete.ShouldBeFalse();
        (valid with { PreservedStateMarker = string.Empty }).IsComplete.ShouldBeFalse();
        (valid with { ReachableExplanation = "Tooltip only" }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void TouchTargetContractShouldEncodeProductMinimumsAndCriticalActionRestrictions()
    {
        ChatBotTouchTarget.PrimaryMinimumCssPixels.ShouldBe(44);
        ChatBotTouchTarget.DenseSecondaryMinimumCssPixels.ShouldBe(24);

        ChatBotTouchTarget.MinimumSizeFor(ChatBotTouchTargetClass.Primary).ShouldBe(44);
        ChatBotTouchTarget.MinimumSizeFor(ChatBotTouchTargetClass.DenseSecondary).ShouldBe(24);

        foreach (ChatBotViewportTier tier in new[] { ChatBotViewportTier.Phone, ChatBotViewportTier.Tablet })
        {
            ChatBotTouchTarget.CanUseDenseSecondarySizing(ChatBotResponsiveActionKind.Approval, tier)
                .ShouldBeFalse($"Approval actions must not use compact-only sizing at {tier} width.");
            ChatBotTouchTarget.CanUseDenseSecondarySizing(ChatBotResponsiveActionKind.Destructive, tier)
                .ShouldBeFalse($"Destructive actions must not use compact-only sizing at {tier} width.");
        }

        foreach (ChatBotViewportTier tier in Enum.GetValues<ChatBotViewportTier>())
        {
            ChatBotTouchTarget.CanUseDenseSecondarySizing(ChatBotResponsiveActionKind.Standard, tier)
                .ShouldBeTrue($"Standard dense secondary actions may use the WCAG floor at {tier} width.");
        }

        ChatBotTouchTarget.CanUseDenseSecondarySizing(
            ChatBotResponsiveActionKind.Approval,
            ChatBotViewportTier.Desktop).ShouldBeTrue();
        ChatBotTouchTarget.CanUseDenseSecondarySizing(
            ChatBotResponsiveActionKind.Destructive,
            ChatBotViewportTier.Desktop).ShouldBeTrue();
    }

    [Fact]
    public void DenseRowCollapseContractShouldKeepSafetyLabelsAndCollapseRawIdsFirst()
    {
        ChatBotDenseRowCollapseContract.RequiredSafetyLabels.ShouldBe(SafetyCriticalDenseLabels, ignoreOrder: false);

        ChatBotDenseRowField project = ChatBotDenseRowCollapseContract.DefaultFields.Single(static field => field.Label == "Project");
        project.Retention.ShouldBe(ChatBotDenseRowFieldRetention.MustKeepVisible);

        ChatBotDenseRowField reason = ChatBotDenseRowCollapseContract.DefaultFields.Single(static field => field.Label == "Reason");
        reason.Retention.ShouldBe(ChatBotDenseRowFieldRetention.MustKeepVisible);

        ChatBotDenseRowField rawId = ChatBotDenseRowCollapseContract.DefaultFields.Single(static field => field.Label == "Raw ID");
        rawId.Retention.ShouldBe(ChatBotDenseRowFieldRetention.CollapseFirst);

        ChatBotDenseRowField secondaryTimestamp = ChatBotDenseRowCollapseContract.DefaultFields.Single(static field => field.Label == "Secondary timestamp");
        secondaryTimestamp.Retention.ShouldBe(ChatBotDenseRowFieldRetention.CollapseFirst);

        foreach (string safetyLabel in SafetyCriticalDenseLabels)
        {
            ChatBotDenseRowField field = ChatBotDenseRowCollapseContract.DefaultFields.Single(item => item.Label == safetyLabel);
            field.Retention.ShouldNotBe(
                ChatBotDenseRowFieldRetention.CollapseFirst,
                $"{safetyLabel} is safety-critical and must stay visible or move to reachable detail.");
            ChatBotDenseRowCollapseContract.CanDropFromPhoneRow(field)
                .ShouldBeFalse($"{safetyLabel} must not disappear from collapsed phone rows.");
        }

        ChatBotDenseRowCollapseContract.CanDropFromPhoneRow(rawId).ShouldBeTrue();
    }

    [Fact]
    public void StylesAndPageShouldExposeResponsiveTouchHooksWithoutViewportZoomLock()
    {
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");
        string app = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/App.razor");
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");
        string governedAction = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor");
        string streamingStop = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor");

        CssVariable(css, "--chatbot-touch-target-primary").ShouldBe("44px");
        CssVariable(css, "--chatbot-touch-target-dense-secondary").ShouldBe("24px");
        css.ShouldContain("--chatbot-responsive-phone-max");
        css.ShouldContain("--chatbot-responsive-tablet-min");
        css.ShouldContain("--chatbot-responsive-desktop-min");
        css.ShouldContain(".chatbot-touch-target-primary");
        css.ShouldContain(".chatbot-labelled-row");
        css.ShouldContain("overflow-wrap: anywhere;");
        css.ShouldContain("@media (max-width: 599px)");
        css.ShouldContain("@media (min-width: 900px)");
        css.ShouldNotContain("user-scalable=no", Case.Insensitive);

        app.ShouldContain("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        app.ShouldNotContain("maximum-scale", Case.Insensitive);
        app.ShouldNotContain("user-scalable", Case.Insensitive);

        page.ShouldContain("data-chatbot-responsive-fixture=\"governed-operations\"");
        page.ShouldContain("chatbot-labelled-row");
        governedAction.ShouldContain("data-chatbot-touch-target=\"primary\"");
        streamingStop.ShouldContain("data-chatbot-touch-target=\"primary\"");
    }

    [Fact]
    public void MigratedFluentActionSurfacesShouldKeepPrimaryAndDenseTouchTargetHooks()
    {
        (string Path, string[] Markers)[] surfaces =
        [
            ("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor", ["<FluentButton", "Class=\"chatbot-touch-target-primary\"", "data-chatbot-touch-target=\"primary\""]),
            ("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor", ["<FluentButton", "Class=\"chatbot-touch-target-primary\"", "data-chatbot-touch-target=\"primary\""]),
            ("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotActorBadge.razor", ["<FluentButton", "Class=\"chatbot-touch-target-dense-secondary\""]),
            ("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor", ["<FluentButton", "data-chatbot-operational-queue=\"true\"", "role=\"row\""]),
            ("src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor", ["<FluentButton", "data-chatbot-stable-id=\"compliance-request-access\"", "compliance-phone-fallback"]),
        ];

        foreach ((string path, string[] markers) in surfaces)
        {
            string source = ReadProjectFile(path);
            foreach (string marker in markers)
            {
                source.ShouldContain(marker, Case.Sensitive);
            }

            source.ShouldNotContain("<button", Case.Sensitive);
            source.ShouldNotContain("<input", Case.Sensitive);
            source.ShouldNotContain("<select", Case.Sensitive);
            source.ShouldNotContain("<textarea", Case.Sensitive);
        }
    }

    [Fact]
    public void PackagePinsShouldRemainUnchanged()
    {
        string packages = ReadProjectFile("Directory.Packages.props");

        packages.ShouldContain("Include=\"Microsoft.FluentUI.AspNetCore.Components\" Version=\"5.0.0-rc.3-26138.1\"");
        packages.ShouldContain("Include=\"Fluxor\" Version=\"6.9.0\"");
        packages.ShouldContain("Include=\"Microsoft.Playwright\" Version=\"1.60.0\"");
        packages.ShouldContain("Include=\"xunit.v3\" Version=\"3.2.2\"");
        packages.ShouldContain("Include=\"bunit\" Version=\"2.7.2\"");
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

    private static string CssVariable(string css, string alias)
    {
        Match match = Regex.Match(
            css,
            $@"^\s*{Regex.Escape(alias)}:\s*(?<value>[^;]+);",
            RegexOptions.CultureInvariant | RegexOptions.Multiline);

        match.Success.ShouldBeTrue($"CSS variable {alias} should be declared exactly once.");
        return match.Groups["value"].Value.Trim();
    }
}
