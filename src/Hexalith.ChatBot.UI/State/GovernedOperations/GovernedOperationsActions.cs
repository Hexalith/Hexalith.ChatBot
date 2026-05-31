namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>Dispatched when the user submits the trivial governed note through the spine.</summary>
public sealed record SubmitGovernedNoteAction;

/// <summary>Dispatched when the governed note was accepted and its outcome read back.</summary>
/// <param name="Outcome">The metadata-only operation outcome.</param>
public sealed record GovernedNoteSubmittedAction(OperationOutcome Outcome);

/// <summary>Dispatched when the submission failed; carries a safe metadata-only error code only.</summary>
/// <param name="Error">The safe metadata-only error code.</param>
public sealed record GovernedNoteSubmissionFailedAction(string Error);
