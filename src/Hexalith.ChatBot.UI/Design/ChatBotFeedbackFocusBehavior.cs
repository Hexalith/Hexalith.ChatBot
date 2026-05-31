namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Focus behavior required when workflow feedback becomes visible.
/// </summary>
public enum ChatBotFeedbackFocusBehavior
{
    PreserveCurrentFocus,
    MoveToLabelledLandingPoint,
    MoveToInlineReason,
    MoveToValidationSummary,
    ReturnToComposerOrProposal,
    NoForcedFocus,
    NewUpdatesAffordanceReachable,
}
