using Fluxor;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Keeps parent render tests at the UI state seam without executing effects.</summary>
internal sealed class SafeOutcomeRenderDispatcher : IDispatcher
{
    /// <summary>Gets actions dispatched by the mounted UI.</summary>
    internal List<object> Actions { get; } = [];
    /// <inheritdoc/>
    public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public void Dispatch(object action) => Actions.Add(action);
}
