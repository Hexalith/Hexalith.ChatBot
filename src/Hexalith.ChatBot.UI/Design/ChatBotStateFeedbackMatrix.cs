namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// UI-owned UX-DR35 state-to-feedback matrix for governed ChatBot surfaces.
/// </summary>
public static class ChatBotStateFeedbackMatrix
{
    public const string BusyRegionContractName = nameof(ChatBotBusyRegionContract);
    public const string ValidationErrorContractName = nameof(ChatBotValidationErrorContract);
    public const string DisabledActionContractName = nameof(ChatBotDisabledActionContract);
    public const string FocusReturnContractName = nameof(ChatBotFocusReturnContract);

    public static IReadOnlyList<ChatBotStateFeedbackContract> Entries { get; } =
    [
        new(
            ChatBotFeedbackStateFamily.LoadingColdLoad,
            ChatBotFeedbackPrimitive.BusyRegion,
            ChatBotLiveRegionPoliteness.None,
            ChatBotFeedbackFocusBehavior.MoveToLabelledLandingPoint,
            ChatBotAnnouncementDedupRule.NoLiveAnnouncement,
            "initial-load-region",
            RequiresInlineStatus: true,
            RequiresBackgroundUpdateAffordance: false,
            [BusyRegionContractName]),
        new(
            ChatBotFeedbackStateFamily.CurrentUserAiProposalReady,
            ChatBotFeedbackPrimitive.StatusBanner,
            ChatBotLiveRegionPoliteness.Polite,
            ChatBotFeedbackFocusBehavior.ReturnToComposerOrProposal,
            ChatBotAnnouncementDedupRule.OncePerStableProposalKey,
            "proposal-id",
            RequiresInlineStatus: true,
            RequiresBackgroundUpdateAffordance: false,
            [FocusReturnContractName]),
        new(
            ChatBotFeedbackStateFamily.CurrentUserCommandAcceptedProjectionPending,
            ChatBotFeedbackPrimitive.StatusBanner,
            ChatBotLiveRegionPoliteness.Polite,
            ChatBotFeedbackFocusBehavior.PreserveCurrentFocus,
            ChatBotAnnouncementDedupRule.OncePerStableOperationKey,
            "operation-id",
            RequiresInlineStatus: true,
            RequiresBackgroundUpdateAffordance: false,
            []),
        new(
            ChatBotFeedbackStateFamily.CurrentUserApprovalRejected,
            ChatBotFeedbackPrimitive.AlertBanner,
            ChatBotLiveRegionPoliteness.Assertive,
            ChatBotFeedbackFocusBehavior.MoveToInlineReason,
            ChatBotAnnouncementDedupRule.OncePerStableProposalKey,
            "proposal-id",
            RequiresInlineStatus: true,
            RequiresBackgroundUpdateAffordance: false,
            [DisabledActionContractName]),
        new(
            ChatBotFeedbackStateFamily.ObservedForOthersRejectionOrQueueUpdate,
            ChatBotFeedbackPrimitive.InlineStatus,
            ChatBotLiveRegionPoliteness.None,
            ChatBotFeedbackFocusBehavior.NoForcedFocus,
            ChatBotAnnouncementDedupRule.NoLiveAnnouncement,
            "observed-update-inline-only",
            RequiresInlineStatus: true,
            RequiresBackgroundUpdateAffordance: false,
            []),
        new(
            ChatBotFeedbackStateFamily.ValidationError,
            ChatBotFeedbackPrimitive.ValidationSummary,
            ChatBotLiveRegionPoliteness.Assertive,
            ChatBotFeedbackFocusBehavior.MoveToValidationSummary,
            ChatBotAnnouncementDedupRule.OncePerValidationAttempt,
            "validation-attempt-id",
            RequiresInlineStatus: true,
            RequiresBackgroundUpdateAffordance: false,
            [ValidationErrorContractName]),
        new(
            ChatBotFeedbackStateFamily.BlockedAction,
            ChatBotFeedbackPrimitive.DisabledActionReason,
            ChatBotLiveRegionPoliteness.Assertive,
            ChatBotFeedbackFocusBehavior.MoveToInlineReason,
            ChatBotAnnouncementDedupRule.OncePerFailureKey,
            "blocked-action-id",
            RequiresInlineStatus: true,
            RequiresBackgroundUpdateAffordance: false,
            [DisabledActionContractName]),
        new(
            ChatBotFeedbackStateFamily.RetryableFailure,
            ChatBotFeedbackPrimitive.StatusBanner,
            ChatBotLiveRegionPoliteness.Polite,
            ChatBotFeedbackFocusBehavior.PreserveCurrentFocus,
            ChatBotAnnouncementDedupRule.OncePerFailureKey,
            "failure-id",
            RequiresInlineStatus: true,
            RequiresBackgroundUpdateAffordance: false,
            []),
        new(
            ChatBotFeedbackStateFamily.TerminalPolicyFailure,
            ChatBotFeedbackPrimitive.AlertBanner,
            ChatBotLiveRegionPoliteness.Assertive,
            ChatBotFeedbackFocusBehavior.MoveToInlineReason,
            ChatBotAnnouncementDedupRule.OncePerFailureKey,
            "policy-failure-id",
            RequiresInlineStatus: true,
            RequiresBackgroundUpdateAffordance: false,
            [DisabledActionContractName]),
        new(
            ChatBotFeedbackStateFamily.DependencyDegraded,
            ChatBotFeedbackPrimitive.StatusBanner,
            ChatBotLiveRegionPoliteness.Polite,
            ChatBotFeedbackFocusBehavior.PreserveCurrentFocus,
            ChatBotAnnouncementDedupRule.OncePerFailureKey,
            "dependency-id",
            RequiresInlineStatus: true,
            RequiresBackgroundUpdateAffordance: false,
            []),
        new(
            ChatBotFeedbackStateFamily.BackgroundUpdateWhileReadingHistory,
            ChatBotFeedbackPrimitive.NewUpdatesAffordance,
            ChatBotLiveRegionPoliteness.None,
            ChatBotFeedbackFocusBehavior.NewUpdatesAffordanceReachable,
            ChatBotAnnouncementDedupRule.NoLiveAnnouncement,
            "history-position",
            RequiresInlineStatus: true,
            RequiresBackgroundUpdateAffordance: true,
            []),
    ];

    public static ChatBotStateFeedbackContract For(ChatBotFeedbackStateFamily stateFamily)
        => Entries.Single(entry => entry.StateFamily == stateFamily);
}
