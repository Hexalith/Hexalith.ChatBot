using Fluxor;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Records the dashboard effect result.</summary>
/// <param name="record">The action observer.</param>
internal sealed class DashboardFailureDispatcher(Action<object> record) : IDispatcher
{
    /// <inheritdoc/>
    public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched;

    /// <inheritdoc/>
    public void Dispatch(object action)
    {
        record(action);
        ActionDispatched?.Invoke(this, new ActionDispatchedEventArgs(action));
    }
}
