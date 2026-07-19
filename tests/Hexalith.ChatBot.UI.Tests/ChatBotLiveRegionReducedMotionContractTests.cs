using Hexalith.ChatBot.UI.Design;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.Tests;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Proves the governed live-region matrix and reduced-motion policy are non-vacuous UI contracts.
/// </summary>
public sealed class ChatBotLiveRegionReducedMotionContractTests
{
    [Fact]
    public void StateFeedbackMatrixShouldCoverEveryUxStateFamilyExactlyOnce()
    {
        ChatBotFeedbackStateFamily[] expectedFamilies = Enum.GetValues<ChatBotFeedbackStateFamily>();

        ChatBotStateFeedbackMatrix.Entries
            .Select(static entry => entry.StateFamily)
            .ShouldBe(expectedFamilies, ignoreOrder: true);

        ChatBotStateFeedbackMatrix.Entries
            .GroupBy(static entry => entry.StateFamily)
            .ShouldAllBe(static group => group.Count() == 1);

        ChatBotStateFeedbackMatrix.Entries.ShouldAllBe(static entry => entry.IsComplete);
    }

    [Fact]
    public void CurrentUserSuccessStatesShouldAnnouncePolitelyOncePerStableKey()
    {
        ChatBotStateFeedbackContract proposalReady = ChatBotStateFeedbackMatrix.For(ChatBotFeedbackStateFamily.CurrentUserAiProposalReady);
        ChatBotStateFeedbackContract commandAccepted = ChatBotStateFeedbackMatrix.For(ChatBotFeedbackStateFamily.CurrentUserCommandAcceptedProjectionPending);

        proposalReady.Politeness.ShouldBe(ChatBotLiveRegionPoliteness.Polite);
        proposalReady.AriaRole.ShouldBe("status");
        proposalReady.AriaLive.ShouldBe("polite");
        proposalReady.DedupRule.ShouldBe(ChatBotAnnouncementDedupRule.OncePerStableProposalKey);
        proposalReady.AnnouncementKeySource.ShouldBe("proposal-id");

        commandAccepted.Politeness.ShouldBe(ChatBotLiveRegionPoliteness.Polite);
        commandAccepted.AriaRole.ShouldBe("status");
        commandAccepted.AriaLive.ShouldBe("polite");
        commandAccepted.DedupRule.ShouldBe(ChatBotAnnouncementDedupRule.OncePerStableOperationKey);
        commandAccepted.AnnouncementKeySource.ShouldBe("operation-id");
    }

    [Fact]
    public void RejectionsAndTerminalPolicyFailuresShouldBeAssertiveWithInlineReasons()
    {
        ChatBotStateFeedbackContract rejection = ChatBotStateFeedbackMatrix.For(ChatBotFeedbackStateFamily.CurrentUserApprovalRejected);
        ChatBotStateFeedbackContract terminalPolicyFailure = ChatBotStateFeedbackMatrix.For(ChatBotFeedbackStateFamily.TerminalPolicyFailure);

        rejection.Politeness.ShouldBe(ChatBotLiveRegionPoliteness.Assertive);
        rejection.AriaRole.ShouldBe("alert");
        rejection.RequiresInlineStatus.ShouldBeTrue();
        rejection.FocusBehavior.ShouldBe(ChatBotFeedbackFocusBehavior.MoveToInlineReason);
        rejection.RequiredExistingContracts.ShouldContain(ChatBotStateFeedbackMatrix.DisabledActionContractName);

        terminalPolicyFailure.Politeness.ShouldBe(ChatBotLiveRegionPoliteness.Assertive);
        terminalPolicyFailure.AriaRole.ShouldBe("alert");
        terminalPolicyFailure.RequiresInlineStatus.ShouldBeTrue();
        terminalPolicyFailure.RequiredExistingContracts.ShouldContain(ChatBotStateFeedbackMatrix.DisabledActionContractName);
    }

    [Fact]
    public void BlockedRetryableAndDependencyFailureStatesShouldUseExpectedPrimitiveAndRepeatRules()
    {
        ChatBotStateFeedbackContract blocked = ChatBotStateFeedbackMatrix.For(ChatBotFeedbackStateFamily.BlockedAction);
        ChatBotStateFeedbackContract retryable = ChatBotStateFeedbackMatrix.For(ChatBotFeedbackStateFamily.RetryableFailure);
        ChatBotStateFeedbackContract degraded = ChatBotStateFeedbackMatrix.For(ChatBotFeedbackStateFamily.DependencyDegraded);

        blocked.Primitive.ShouldBe(ChatBotFeedbackPrimitive.DisabledActionReason);
        blocked.Politeness.ShouldBe(ChatBotLiveRegionPoliteness.Assertive);
        blocked.AriaRole.ShouldBe("alert");
        blocked.DedupRule.ShouldBe(ChatBotAnnouncementDedupRule.OncePerFailureKey);
        blocked.AnnouncementKeySource.ShouldBe("blocked-action-id");
        blocked.FocusBehavior.ShouldBe(ChatBotFeedbackFocusBehavior.MoveToInlineReason);
        blocked.RequiredExistingContracts.ShouldContain(ChatBotStateFeedbackMatrix.DisabledActionContractName);

        retryable.Primitive.ShouldBe(ChatBotFeedbackPrimitive.StatusBanner);
        retryable.Politeness.ShouldBe(ChatBotLiveRegionPoliteness.Polite);
        retryable.AriaRole.ShouldBe("status");
        retryable.DedupRule.ShouldBe(ChatBotAnnouncementDedupRule.OncePerFailureKey);
        retryable.AnnouncementKeySource.ShouldBe("failure-id");
        retryable.RequiresInlineStatus.ShouldBeTrue();

        degraded.Primitive.ShouldBe(ChatBotFeedbackPrimitive.StatusBanner);
        degraded.Politeness.ShouldBe(ChatBotLiveRegionPoliteness.Polite);
        degraded.AriaRole.ShouldBe("status");
        degraded.DedupRule.ShouldBe(ChatBotAnnouncementDedupRule.OncePerFailureKey);
        degraded.AnnouncementKeySource.ShouldBe("dependency-id");
        degraded.RequiresInlineStatus.ShouldBeTrue();
    }

    [Fact]
    public void ObservedForOthersAndHistoryUpdatesShouldStayInlineOnly()
    {
        ChatBotStateFeedbackContract observed = ChatBotStateFeedbackMatrix.For(ChatBotFeedbackStateFamily.ObservedForOthersRejectionOrQueueUpdate);
        ChatBotStateFeedbackContract background = ChatBotStateFeedbackMatrix.For(ChatBotFeedbackStateFamily.BackgroundUpdateWhileReadingHistory);

        observed.IsInlineOnly.ShouldBeTrue();
        observed.AriaRole.ShouldBeNull();
        observed.AriaLive.ShouldBe("off");
        observed.Primitive.ShouldBe(ChatBotFeedbackPrimitive.InlineStatus);
        observed.RequiresInlineStatus.ShouldBeTrue();

        background.IsInlineOnly.ShouldBeTrue();
        background.RequiresBackgroundUpdateAffordance.ShouldBeTrue();
        background.Primitive.ShouldBe(ChatBotFeedbackPrimitive.NewUpdatesAffordance);
        background.FocusBehavior.ShouldBe(ChatBotFeedbackFocusBehavior.NewUpdatesAffordanceReachable);
    }

    [Fact]
    public void BusyAndValidationEntriesShouldReuseExistingContracts()
    {
        ChatBotStateFeedbackContract loading = ChatBotStateFeedbackMatrix.For(ChatBotFeedbackStateFamily.LoadingColdLoad);
        ChatBotStateFeedbackContract validation = ChatBotStateFeedbackMatrix.For(ChatBotFeedbackStateFamily.ValidationError);

        loading.RequiredExistingContracts.ShouldBe([ChatBotStateFeedbackMatrix.BusyRegionContractName]);
        loading.Primitive.ShouldBe(ChatBotFeedbackPrimitive.BusyRegion);
        loading.Politeness.ShouldBe(ChatBotLiveRegionPoliteness.None);

        validation.RequiredExistingContracts.ShouldBe([ChatBotStateFeedbackMatrix.ValidationErrorContractName]);
        validation.Primitive.ShouldBe(ChatBotFeedbackPrimitive.ValidationSummary);
        validation.Politeness.ShouldBe(ChatBotLiveRegionPoliteness.Assertive);
        validation.FocusBehavior.ShouldBe(ChatBotFeedbackFocusBehavior.MoveToValidationSummary);
    }

    [Fact]
    public void BackgroundUpdateContractShouldRequireReachableAffordanceAndNoForcedScroll()
    {
        ChatBotBackgroundUpdateContract contract = ChatBotBackgroundUpdateContract.GovernedFoundation;

        contract.IsComplete.ShouldBeTrue();
        contract.AffordanceLabel.ShouldBe("New updates");
        contract.IsKeyboardReachable.ShouldBeTrue();
        contract.PreventsForcedScroll.ShouldBeTrue();
        contract.PreservesFocusAndSelection.ShouldBeTrue();
        contract.ObservedForOthersUpdatesAreInlineOnly.ShouldBeTrue();
        contract.UsesMotionOnlyCue.ShouldBeFalse();

        (contract with { IsKeyboardReachable = false }).IsComplete.ShouldBeFalse();
        (contract with { PreventsForcedScroll = false }).IsComplete.ShouldBeFalse();
        (contract with { UsesMotionOnlyCue = true }).IsComplete.ShouldBeFalse();
    }

    [Fact]
    public void ReducedMotionContractShouldSuppressGovernedMotionHooksAndKeepTextCues()
    {
        ChatBotReducedMotionContract contract = ChatBotReducedMotionContract.GovernedFoundation;
        string css = ReadProjectFile("src/Hexalith.ChatBot.UI/wwwroot/css/chatbot.tokens.css");

        contract.IsComplete.ShouldBeTrue();
        contract.SuppressedMotionHooks.ShouldContain(".chatbot-shimmer");
        contract.SuppressedMotionHooks.ShouldContain(".chatbot-skeleton");
        contract.SuppressedMotionHooks.ShouldContain(".chatbot-row-motion");
        contract.SuppressedMotionHooks.ShouldContain(".chatbot-streaming-text");
        contract.SuppressedMotionHooks.ShouldContain(".chatbot-panel-transition");
        contract.StableStatusLabels.ShouldContain("Scanning attachment");
        contract.StableStatusLabels.ShouldContain("Projection pending");
        contract.PreservesFocusVisibility.ShouldBeTrue();
        contract.PreservesForcedColors.ShouldBeTrue();

        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        foreach (string hook in contract.SuppressedMotionHooks)
        {
            css.ShouldContain(hook);
        }

        css.ShouldContain("animation: none !important;");
        css.ShouldContain("transition-duration: 0.01ms !important;");
        css.ShouldContain("scroll-behavior: auto !important;");
        css.ShouldContain("@media (forced-colors: active)");
    }

    [Fact]
    public void StatusBannerShouldExposeMatrixDrivenLiveMetadataAndDedupKeys()
    {
        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStatusBanner.razor");
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/GovernedOperations.razor");

        component.ShouldContain("data-chatbot-feedback-state");
        component.ShouldContain("data-chatbot-live");
        component.ShouldContain("data-chatbot-announcement-key");
        component.ShouldContain("data-chatbot-repeat-rule");
        component.ShouldContain("data-chatbot-live-announced");
        component.ShouldContain("aria-live=\"@AriaLive\"");
        component.ShouldContain("ChatBotAnnouncementDeduplicationState");
        component.ShouldContain("ChatBotStateFeedbackMatrix.For(stateFamily)");

        page.ShouldContain("StateFamily=\"@ChatBotFeedbackStateFamily.CurrentUserCommandAcceptedProjectionPending\"");
        page.ShouldContain("AnnouncementKey=\"@outcome.OperationId\"");
        page.ShouldContain("AnnouncementKey=\"@($\"{outcome.OperationId}-audit\")\"");
        page.ShouldContain("StateFamily=\"@ChatBotFeedbackStateFamily.ObservedForOthersRejectionOrQueueUpdate\"");
        page.ShouldContain("StateFamily=\"@ChatBotFeedbackStateFamily.RetryableFailure\"");
        page.ShouldNotContain("LiveRegionPoliteness=\"@ChatBotLiveRegionPoliteness.Assertive\"");
        page.ShouldNotContain("role=\"status\"");
    }

    [Fact]
    public void AnnouncementDeduplicationStateShouldSuppressStableKeyRepeatsPerCircuit()
    {
        ChatBotAnnouncementDeduplicationState state = new();

        state.ShouldAnnounce("operation-1", ChatBotAnnouncementDedupRule.OncePerStableOperationKey).ShouldBeTrue();
        state.ShouldAnnounce("operation-1", ChatBotAnnouncementDedupRule.OncePerStableOperationKey).ShouldBeFalse();
        state.ShouldAnnounce("operation-1", ChatBotAnnouncementDedupRule.OncePerFailureKey).ShouldBeTrue();
        state.ShouldAnnounce("operation-2", ChatBotAnnouncementDedupRule.OncePerStableOperationKey).ShouldBeTrue();
        state.ShouldAnnounce("activation", ChatBotAnnouncementDedupRule.OncePerActivation).ShouldBeTrue();
        state.ShouldAnnounce("activation", ChatBotAnnouncementDedupRule.OncePerActivation).ShouldBeTrue();
        state.ShouldAnnounce("inline-only", ChatBotAnnouncementDedupRule.NoLiveAnnouncement).ShouldBeFalse();
    }

    [Fact]
    public void BlockedStateShouldConsumeMatrixContractInsteadOfAdHocRoles()
    {
        string component = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotBlockedState.razor");

        component.ShouldContain("ChatBotStateFeedbackMatrix.For(FeedbackState)");
        component.ShouldContain("data-chatbot-feedback-state");
        component.ShouldContain("data-chatbot-announcement-key");
        component.ShouldContain("data-chatbot-repeat-rule");
        component.ShouldContain("aria-live=\"@FeedbackContract.AriaLive\"");
        component.ShouldContain("ChatBotFeedbackStateFamily.BlockedAction");
        component.ShouldContain("ChatBotFeedbackStateFamily.TerminalPolicyFailure");
        component.ShouldNotContain("IsTerminalForCurrentUser ? \"alert\" : \"status\"");
    }

    [Fact]
    public void PackagePinsShouldMatchApprovedSharedCatalogForLiveRegionAndMotionFoundation()
    {
        PackageCatalogTestHelper.AssertUiFoundationPins();
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
