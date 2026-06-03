using Fluxor;

namespace Hexalith.ChatBot.UI.State.OperationalDashboards;

/// <summary>Pure reducers for the operational-dashboards slice.</summary>
public static class OperationalDashboardsReducers
{
    /// <summary>Marks a load/refresh as in flight and clears any prior error.</summary>
    /// <param name="state">The current state.</param>
    /// <returns>The next state.</returns>
    [ReducerMethod(typeof(LoadOperationalDashboardAction))]
    public static OperationalDashboardsState ReduceLoad(OperationalDashboardsState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state with { IsLoading = true, Error = null };
    }

    /// <summary>Records a loaded overview.</summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The loaded action carrying the overview.</param>
    /// <returns>The next state.</returns>
    [ReducerMethod]
    public static OperationalDashboardsState ReduceLoaded(OperationalDashboardsState state, OperationalDashboardLoadedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { IsLoading = false, Overview = action.Overview, Error = null };
    }

    /// <summary>Records a safe metadata-only failure code while preserving any previously loaded overview.</summary>
    /// <param name="state">The current state.</param>
    /// <param name="action">The failure action carrying the error code.</param>
    /// <returns>The next state.</returns>
    [ReducerMethod]
    public static OperationalDashboardsState ReduceFailed(OperationalDashboardsState state, OperationalDashboardLoadFailedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { IsLoading = false, Error = action.Error };
    }
}
