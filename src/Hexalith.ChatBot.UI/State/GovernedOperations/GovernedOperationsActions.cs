namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>Dispatched when the user submits the trivial governed note through the spine.</summary>
public sealed record SubmitGovernedNoteAction;

/// <summary>Dispatched when the governed note was accepted and its outcome read back.</summary>
/// <param name="Outcome">The metadata-only operation outcome.</param>
public sealed record GovernedNoteSubmittedAction(OperationOutcome Outcome);

/// <summary>Dispatched when the submission failed; carries a safe metadata-only error code only.</summary>
/// <param name="Error">The safe metadata-only error code.</param>
public sealed record GovernedNoteSubmissionFailedAction(string Error);

/// <summary>
/// Dispatched when an in-flight submission was cancelled (host navigation, component disposal, or an HTTP
/// timeout surfacing as <see cref="TaskCanceledException"/>). Carries no error: cancellation is not a failure,
/// but the slice must not be left with a stuck in-flight flag.
/// </summary>
public sealed record GovernedNoteSubmissionCancelledAction;
