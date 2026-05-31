namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// UX-DR35 governed workflow state families that require explicit feedback behavior.
/// </summary>
public enum ChatBotFeedbackStateFamily
{
    LoadingColdLoad,
    CurrentUserAiProposalReady,
    CurrentUserCommandAcceptedProjectionPending,
    CurrentUserApprovalRejected,
    ObservedForOthersRejectionOrQueueUpdate,
    ValidationError,
    BlockedAction,
    RetryableFailure,
    TerminalPolicyFailure,
    DependencyDegraded,
    BackgroundUpdateWhileReadingHistory,
}
