using Fluxor;

namespace Hexalith.ChatBot.UI.State.GovernedOperations;

/// <summary>Pure reducers for the governed-operations slice.</summary>
public static class GovernedOperationsReducers
{
    /// <summary>Marks a submission as in flight and clears any prior error.</summary>
    /// <param name="state">The current state.</param>
    /// <returns>The next state.</returns>
    [ReducerMethod(typeof(SubmitGovernedNoteAction))]
    public static GovernedOperationsState ReduceSubmit(GovernedOperationsState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with { IsSubmitting = true, Error = null };
    }

    /// <summary>Records a successful outcome.</summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The submitted action carrying the outcome.</param>
    /// <returns>The next state.</returns>
    [ReducerMethod]
    public static GovernedOperationsState ReduceSubmitted(GovernedOperationsState state, GovernedNoteSubmittedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { IsSubmitting = false, Outcome = action.Outcome, Error = null };
    }

    /// <summary>Records a safe metadata-only failure code.</summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The failure action carrying the error code.</param>
    /// <returns>The next state.</returns>
    [ReducerMethod]
    public static GovernedOperationsState ReduceFailed(GovernedOperationsState state, GovernedNoteSubmissionFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        // Clear the previous outcome: leaving it in place renders the failure banner alongside the prior
        // success's outcome section, audit history and "projection complete" banner, which reads as though the
        // failed submission had itself succeeded.
        return state with { IsSubmitting = false, Outcome = null, Error = action.Error };
    }

    /// <summary>Clears the in-flight flag when a submission was cancelled rather than completed or failed.</summary>
    /// <param name="state">The current state.</param>
    /// <returns>The next state.</returns>
    [ReducerMethod(typeof(GovernedNoteSubmissionCancelledAction))]
    public static GovernedOperationsState ReduceCancelled(GovernedOperationsState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with { IsSubmitting = false };
    }
}
