namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>
/// Fluxor state for the governed-operations page: whether a submit is in flight, the last metadata-only
/// outcome, and a safe (metadata-only) error code when a submission fails.
/// </summary>
/// <param name="IsSubmitting">Whether a submission is currently in flight.</param>
/// <param name="Outcome">The last successful metadata-only outcome, or <see langword="null"/>.</param>
/// <param name="Error">A safe metadata-only error code, or <see langword="null"/>.</param>
public sealed record GovernedOperationsState(
    bool IsSubmitting,
    OperationOutcome? Outcome,
    string? Error);
