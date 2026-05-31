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
        return state with { IsSubmitting = false, Error = action.Error };
    }
}
