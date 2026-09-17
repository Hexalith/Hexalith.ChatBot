namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>
/// Dispatched when an in-flight submission was cancelled (host navigation, component disposal, or an HTTP
/// timeout surfacing as <see cref="TaskCanceledException"/>). Carries no error: cancellation is not a failure,
/// but the slice must not be left with a stuck in-flight flag.
/// </summary>
public sealed record GovernedNoteSubmissionCancelledAction;
