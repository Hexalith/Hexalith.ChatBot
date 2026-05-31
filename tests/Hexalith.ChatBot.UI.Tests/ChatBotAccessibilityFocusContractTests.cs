using Hexalith.ChatBot.UI.Design;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Proves the accessibility and focus floor is encoded as UI-owned contracts.
/// </summary>
public sealed class ChatBotAccessibilityFocusContractTests
{
    private static readonly string[] RequiredFloorContracts =
    [
        "Keyboard operation",
        "Repeated landmark naming",
        "Visible-order focus sequence",
        "Focus return",
        "Disabled-action explanation",
        "Busy-region focus preservation",
        "Validation error association",
    ];

    [Fact]
    public void AccessibilityFloorShouldEnumerateRequiredGovernedUiContracts()
    {
        ChatBotAccessibilityFloorContract.RequiredContracts
            .Select(static contract => contract.Name)
            .ShouldBe(RequiredFloorContracts, ignoreOrder: false);

        foreach (ChatBotAccessibilityRequirement requirement in ChatBotAccessibilityFloorContract.RequiredContracts)
        {
            requirement.IsComplete.ShouldBeTrue();
            requirement.RequiredBehavior.ShouldNotContain("server", Case.Insensitive);
            requirement.RequiredBehavior.ShouldNotContain("DAPR", Case.Insensitive);
            requirement.RequiredBehavior.ShouldNotContain("MCP", Case.Insensitive);
        }
    }

    [Fact]
    public void KeyboardAndVisibleOrderContractsShouldRequireSkipMainHeadingAndReachableActions()
    {
        ChatBotKeyboardOperationContract keyboard = ChatBotKeyboardOperationContract.CreateGovernedSurface(
            SurfaceName: "Governed operations",
            RequiredKeyboardPaths:
            [
                "Skip link reaches main content.",
                "Primary governed action is keyboard reachable.",
                "Disabled governed action reason is keyboard reachable.",
            ]);

        keyboard.IsComplete.ShouldBeTrue();
        keyboard.RequiresVisibleFocus.ShouldBeTrue();
        keyboard.RequiresNoHoverOnlyCriticalActions.ShouldBeTrue();
        keyboard.RequiresShortcutGovernance.ShouldBeTrue();

        (keyboard with { RequiredKeyboardPaths = [] }).IsComplete.ShouldBeFalse();
        (keyboard with { RequiredKeyboardPaths = null! }).IsComplete.ShouldBeFalse();

        ChatBotFocusSequenceContract focusSequence = new(
            SkipLinkTargetId: "chatbot-main-content",
            MainRegionId: "chatbot-main-content",
            SurfaceHeadingSelector: "h1",
            OrderedLandmarkNames:
            [
                "Project context",
                "Governed command path",
                "Governed operation review context",
                "Operation status summary",
            ]);

        focusSequence.IsComplete.ShouldBeTrue();
        (focusSequence with { SurfaceHeadingSelector = string.Empty }).IsComplete.ShouldBeFalse();
        (focusSequence with { OrderedLandmarkNames = ["Project context", ""] }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void LandmarkContractsShouldRejectDuplicateRepeatedNames()
    {
        ChatBotLandmarkContract main = new("region", "Governed command path", IsRepeatedWithinSurface: true);
        ChatBotLandmarkContract complementary = new("complementary", "Governed operation review context", IsRepeatedWithinSurface: true);
        ChatBotLandmarkContract duplicate = new("region", "Governed command path", IsRepeatedWithinSurface: true);

        main.IsComplete.ShouldBeTrue();
        complementary.IsComplete.ShouldBeTrue();

        ChatBotLandmarkContract.HasUniqueAccessibleNames([main, complementary]).ShouldBeTrue();
        ChatBotLandmarkContract.HasUniqueAccessibleNames([main, duplicate]).ShouldBeFalse();
        ChatBotLandmarkContract.HasUniqueAccessibleNames(null!).ShouldBeFalse();
        (main with { AccessibleName = string.Empty }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void DisabledActionContractShouldRequireDiscoverableReasonAndSuppressedActivation()
    {
        ChatBotDisabledActionContract disabledAction = ChatBotDisabledActionContract.CreateGovernedAction(
            actionName: "Retry quarantined operation",
            disabledReasonId: "retry-disabled-reason",
            disabledReasonLabel: "Why unavailable? Quarantine review is required before retry.");

        disabledAction.IsComplete.ShouldBeTrue();
        disabledAction.UsesAriaDisabled.ShouldBeTrue();
        disabledAction.KeepsKeyboardFocusOrder.ShouldBeTrue();
        disabledAction.ReferencesReachableReason.ShouldBeTrue();
        disabledAction.SuppressesActivationWhenDisabled.ShouldBeTrue();
        disabledAction.UsesTooltipOnlyReason.ShouldBeFalse();

        (disabledAction with { UsesAriaDisabled = false }).IsComplete.ShouldBeFalse();
        (disabledAction with { KeepsKeyboardFocusOrder = false }).IsComplete.ShouldBeFalse();
        (disabledAction with { ReferencesReachableReason = false }).IsComplete.ShouldBeFalse();
        (disabledAction with { SuppressesActivationWhenDisabled = false }).IsComplete.ShouldBeFalse();
        (disabledAction with { UsesTooltipOnlyReason = true }).IsComplete.ShouldBeFalse();
        (disabledAction with { DisabledReasonLabel = string.Empty }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void OverlayFocusPolicyShouldCoverContainmentEscapeReturnAndComplementaryPanels()
    {
        ChatBotOverlayPolicy.RequiresFocusContainment(ChatBotOverlayKind.ModalDialog).ShouldBeTrue();
        ChatBotOverlayPolicy.RequiresFocusContainment(ChatBotOverlayKind.ModalSheet).ShouldBeTrue();
        ChatBotOverlayPolicy.RequiresFocusContainment(ChatBotOverlayKind.EvidenceDrawer).ShouldBeFalse();
        ChatBotOverlayPolicy.AllowsEscapeCloseWhenTopmost(ChatBotOverlayKind.Popover).ShouldBeTrue();
        ChatBotOverlayPolicy.RequiresFocusReturn(ChatBotOverlayKind.ReviewPanel).ShouldBeTrue();
        ChatBotOverlayPolicy.IsComplementaryRegion(ChatBotOverlayKind.ReviewPanel).ShouldBeTrue();

        ChatBotFocusReturnContract modalDialog = ChatBotFocusReturnContract.ForOverlay(ChatBotOverlayKind.ModalDialog);
        ChatBotFocusReturnContract reviewPanel = ChatBotFocusReturnContract.ForOverlay(ChatBotOverlayKind.ReviewPanel);

        modalDialog.IsComplete.ShouldBeTrue();
        modalDialog.ContainsFocusWhenModal.ShouldBeTrue();
        reviewPanel.IsComplete.ShouldBeTrue();
        reviewPanel.UsesComplementaryRegionWhenNonModal.ShouldBeTrue();
        (modalDialog with { ReturnsFocusToInvoker = false }).IsComplete.ShouldBeFalse();
        (modalDialog with { ContainsFocusWhenModal = false }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void BusyAndValidationContractsShouldRequireStableFocusAndErrorAssociations()
    {
        ChatBotBusyRegionContract busy = new(
            RegionId: "operation-status-region",
            AccessibleLabel: "Operation status summary",
            BusyStateElementId: "operation-status-region",
            FocusPreservationTargetId: "record-governed-note",
            LoadedContentLandingTargetId: "operation-outcome-title",
            ClearsAriaBusyOnSameRegion: true,
            AnnouncesHistoricalContent: false);

        busy.IsComplete.ShouldBeTrue();
        (busy with { ClearsAriaBusyOnSameRegion = false }).IsComplete.ShouldBeFalse();
        (busy with { AnnouncesHistoricalContent = true }).IsComplete.ShouldBeFalse();
        (busy with { FocusPreservationTargetId = string.Empty, LoadedContentLandingTargetId = string.Empty }).IsComplete.ShouldBeFalse();

        ChatBotValidationErrorContract validation = new(
            SummaryId: "approval-errors",
            SummaryLabel: "Approval validation summary",
            FocusTargetId: "approval-errors",
            AffectedFieldIds: ["approval-rationale", "approval-decision"],
            FieldMessageIds: new Dictionary<string, string>
            {
                ["approval-rationale"] = "approval-rationale-message",
                ["approval-decision"] = "approval-decision-message",
            },
            SafeNextAction: "Review the highlighted fields before submitting.");

        validation.IsComplete.ShouldBeTrue();
        validation.RequiresInvalidFields.ShouldBeTrue();
        validation.RequiresMessageAssociation.ShouldBeTrue();
        (validation with { FieldMessageIds = new Dictionary<string, string>() }).IsComplete.ShouldBeFalse();
        (validation with { AffectedFieldIds = null! }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void ShellAndPageShouldExposeStableFocusEntryAndUniqueRegionSemantics()
    {
        string layout = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Layout/MainLayout.razor");
        string routes = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Routes.razor");
        string shell = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotConversationShell.razor");
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        layout.ShouldContain("href=\"#chatbot-main-content\"");
        layout.ShouldContain("id=\"chatbot-main-content\"");
        layout.ShouldContain("tabindex=\"-1\"");
        routes.ShouldContain("FocusOnNavigate");
        routes.ShouldContain("Selector=\"h1\"");

        shell.ShouldContain("ResolvedMainLabel");
        shell.ShouldContain("ResolvedComplementaryLabel");
        shell.ShouldContain("role=\"complementary\"");
        page.ShouldContain("MainLabel=\"Governed command path\"");
        page.ShouldContain("ComplementaryLabel=\"Governed operation review context\"");
        page.ShouldContain("<ComplementaryPanel>");
    }

    [Fact]
    public void PackagePinsShouldRemainUnchangedForAccessibilityFloor()
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
}
