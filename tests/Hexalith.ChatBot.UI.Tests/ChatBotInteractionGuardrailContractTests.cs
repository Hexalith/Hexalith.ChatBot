using Hexalith.ChatBot.UI.Design;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Proves the interaction guardrail foundation is mechanically enforced, not only documented.
/// </summary>
public sealed class ChatBotInteractionGuardrailContractTests
{
    private static readonly string[] RequiredBannedInteractions =
    [
        "NoHiddenAutoAssociationWhenAmbiguous",
        "NoRiskyAiExecutionFromPlainSend",
        "NoHoverOnlyCriticalActions",
        "NoStackedActiveDialogsOrSheets",
        "NoInfiniteScrollQueues",
        "NoCliMcpAdminAuthorizationBypassAffordance",
    ];

    [Fact]
    public void InteractionGuardrailContractShouldEnumerateEveryUxDr33Ban()
    {
        Enum.GetNames<ChatBotInteractionGuardrail>().ShouldBe(RequiredBannedInteractions, ignoreOrder: false);
        ChatBotInteractionGuardrailContract.BannedInteractions
            .Select(guardrail => guardrail.ToString())
            .ShouldBe(RequiredBannedInteractions, ignoreOrder: false);

        foreach (ChatBotInteractionGuardrail guardrail in ChatBotInteractionGuardrailContract.BannedInteractions)
        {
            ChatBotGovernedUiText.GetInteractionGuardrailLabel(guardrail).ShouldNotBeNullOrWhiteSpace();
            ChatBotGovernedUiText.GetInteractionGuardrailResourceKey(guardrail).ShouldStartWith("Guardrail_");
        }
    }

    [Fact]
    public void GovernedActionPrimitiveShouldExposeReachableStateAndReasonSemantics()
    {
        Enum.GetNames<ChatBotGovernedActionState>().ShouldBe(
            ["Enabled", "DisabledWithReason", "NotApplicableHidden"],
            ignoreOrder: false);

        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotGovernedAction.razor");

        component.ShouldContain("<FluentButton");
        component.ShouldContain("data-chatbot-critical-action");
        component.ShouldContain("aria-disabled=\"@AriaDisabled\"");
        component.ShouldContain("aria-describedby=\"@ReasonReferenceId\"");
        component.ShouldContain("tabindex=\"0\"");
        component.ShouldContain("ChatBotUiTextKey.WhyUnavailable");
        component.ShouldContain("DisabledReason");
        component.ShouldNotContain("@onmouseover");
        component.ShouldNotContain("@onmouseenter");
        component.ShouldNotContain("title=");
        component.ShouldNotContain(" Disabled=\"");
    }

    [Fact]
    public void StreamingStopControlShouldBeStableFocusablePoliteAndReturnFocus()
    {
        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor");
        string app = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/App.razor");
        string focusScript = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/js/chatbot.focus.js");

        component.ShouldContain("IsStreaming");
        component.ShouldContain("<FluentButton");
        component.ShouldContain("AccessibleLabel");
        component.ShouldContain("role=\"status\"");
        component.ShouldContain("aria-live=\"polite\"");
        component.ShouldContain("ChatBotUiTextKey.StopResponseAnnouncement");
        component.ShouldContain("FocusReturnTargetId");
        component.ShouldContain("HexalithChatBot.focusElementById");
        component.ShouldContain("LiveRegionMessage = string.Empty");
        component.ShouldContain("InvokeAsync(StateHasChanged)");
        component.ShouldNotContain("@onmouseover");
        app.ShouldContain("js/chatbot.focus.js");
        focusScript.ShouldContain("focusElementById");
        focusScript.ShouldContain("document.getElementById");
    }

    [Fact]
    public void ShortcutGuardrailShouldDisableUnsafeTextEntryDefaultsAndExposePreferences()
    {
        Enum.GetNames<ChatBotShortcutScope>().ShouldBe(
            ["Composer", "Search", "Filter", "ConfigurationForm", "GlobalOperator", "DeveloperOperator"],
            ignoreOrder: false);

        ChatBotShortcutPreferenceContract.EntryLabel.ShouldBe("Keyboard shortcuts");
        ChatBotShortcutPreferenceContract.CanDisableGlobally.ShouldBeTrue();
        ChatBotShortcutPreferenceContract.SupportsRemapping.ShouldBeTrue();

        ChatBotShortcutDefinition composerCharacter = new(
            "composer-character",
            "k",
            ChatBotShortcutScope.Composer,
            RequiresModifier: false,
            IsCharacterKeyShortcut: true);
        ChatBotShortcutDefinition searchModifierFree = new(
            "search-open",
            "Enter",
            ChatBotShortcutScope.Search,
            RequiresModifier: false,
            IsCharacterKeyShortcut: false);
        ChatBotShortcutDefinition operatorChord = new(
            "operator-command",
            "K",
            ChatBotShortcutScope.GlobalOperator,
            RequiresModifier: true,
            IsCharacterKeyShortcut: false);

        composerCharacter.IsAllowedByDefaultInTextEntry.ShouldBeFalse();
        searchModifierFree.IsAllowedByDefaultInTextEntry.ShouldBeFalse();
        operatorChord.IsAllowedByDefaultInTextEntry.ShouldBeTrue();
        composerCharacter.PreferenceEntryLabel.ShouldBe("Keyboard shortcuts");
        composerCharacter.CanBeDisabledGlobally.ShouldBeTrue();
        composerCharacter.CanBeRemapped.ShouldBeTrue();
    }

    [Fact]
    public void OverlayAndQueuePoliciesShouldRejectStackedModalsAndInfiniteScrollDefaults()
    {
        ChatBotOverlayPolicy.AllowsActivation(
            [ChatBotOverlayKind.ModalDialog],
            ChatBotOverlayKind.ModalSheet).ShouldBeFalse();
        ChatBotOverlayPolicy.AllowsActivation(
            [ChatBotOverlayKind.ModalDialog],
            ChatBotOverlayKind.EvidenceDrawer).ShouldBeTrue();
        ChatBotOverlayPolicy.IsModal(ChatBotOverlayKind.ModalDialog).ShouldBeTrue();
        ChatBotOverlayPolicy.RequiresEscapeAndFocusReturn(ChatBotOverlayKind.Popover).ShouldBeTrue();
        ChatBotOverlayPolicy.RequiresEscapeAndFocusReturn(ChatBotOverlayKind.ReviewPanel).ShouldBeTrue();
        ChatBotOverlayPolicy.IsComplementaryRegion(ChatBotOverlayKind.EvidenceDrawer).ShouldBeTrue();
        ChatBotOverlayPolicy.IsComplementaryRegion(ChatBotOverlayKind.ModalSheet).ShouldBeFalse();

        ChatBotQueueLoadingPolicy.IsPermittedDefault(ChatBotQueueLoadingMode.InfiniteScroll).ShouldBeFalse();
        ChatBotQueueLoadingPolicy.IsPermittedDefault(ChatBotQueueLoadingMode.Pagination).ShouldBeTrue();
        ChatBotQueueLoadingPolicy.IsPermittedDefault(ChatBotQueueLoadingMode.VirtualizedListWithStableFilters).ShouldBeTrue();

        ChatBotQueueLoadingContract valid = new(
            ChatBotQueueLoadingMode.Pagination,
            ActiveFilterDescription: "Status: pending review",
            ResultCount: 42,
            PageNumber: 1,
            PageSize: 25);
        ChatBotQueueLoadingContract infinite = valid with { Mode = ChatBotQueueLoadingMode.InfiniteScroll };
        ChatBotQueueLoadingContract missingState = valid with { ActiveFilterDescription = string.Empty };

        valid.IsValidOperationalQueueContract.ShouldBeTrue();
        infinite.IsValidOperationalQueueContract.ShouldBeFalse();
        missingState.IsValidOperationalQueueContract.ShouldBeFalse();
    }

    [Fact]
    public void GovernedOperationsShouldUseGuardedActionWithoutChangingUiOriginPath()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");
        string service = ReadProjectFile("src/Hexalith.ChatBot.UI/Services/GovernedOperationService.cs");

        page.ShouldContain("<ChatBotGovernedAction");
        page.ShouldContain("SubmitGovernedNoteAction");
        page.ShouldContain("<ChatBotStatusBanner");
        page.ShouldContain("metadata-only");
        page.ShouldNotContain("ChatBotStreamingStopControl");
        service.ShouldContain("ChatBotSurfaceOrigin.Ui");
    }

    [Fact]
    public void GovernedOperationsShouldExposeOperationalQueueSurfaceWithoutInfiniteScroll()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        page.ShouldContain("data-chatbot-operational-queue=\"true\"");
        page.ShouldContain("ChatBotQueueLoadingMode.Pagination");
        page.ShouldNotContain("ChatBotQueueLoadingMode.InfiniteScroll");
        page.ShouldContain("OperationalQueueFamily.AmbiguousAssociation");
        page.ShouldContain("OperationalQueueFamily.UnresolvedParticipant");
        page.ShouldContain("OperationalQueueFamily.PendingApproval");
        page.ShouldContain("OperationalQueueFamily.FailedIngestion");
        page.ShouldContain("OperationalQueueFamily.FailedAttachment");
        page.ShouldContain("OperationalQueueFamily.RetryableOperation");
        page.ShouldContain("GovernedOperationsQueuePrimaryAction");
        page.ShouldContain("GovernedOperationsQueueSecondaryActions");
        page.ShouldContain("GovernedOperationsQueueDetailUnavailable");
        page.ShouldContain("DisabledWithReason");
        page.ShouldContain("data-chatbot-source-version");
        page.ShouldContain("page-size:100");
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
