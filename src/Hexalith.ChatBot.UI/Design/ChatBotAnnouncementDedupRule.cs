namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Repeat policy for live-region announcements.
/// </summary>
public enum ChatBotAnnouncementDedupRule
{
    NoLiveAnnouncement,
    OncePerStableOperationKey,
    OncePerStableProposalKey,
    OncePerValidationAttempt,
    OncePerActivation,
    OncePerFailureKey,
}
