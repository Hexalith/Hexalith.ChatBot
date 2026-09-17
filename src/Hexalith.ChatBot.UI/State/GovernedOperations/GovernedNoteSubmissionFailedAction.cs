namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>Dispatched when the submission failed; carries a safe metadata-only error code only.</summary>
/// <param name="Error">The safe metadata-only error code.</param>
public sealed record GovernedNoteSubmissionFailedAction(string Error);
