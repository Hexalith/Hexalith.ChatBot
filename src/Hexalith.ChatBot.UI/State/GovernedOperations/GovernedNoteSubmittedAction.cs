namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>Dispatched when the governed note was accepted and its outcome read back.</summary>
/// <param name="Outcome">The metadata-only operation outcome.</param>
public sealed record GovernedNoteSubmittedAction(OperationOutcome Outcome);
