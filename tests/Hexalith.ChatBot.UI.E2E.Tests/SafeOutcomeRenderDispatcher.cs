using Fluxor;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Keeps parent render tests at the UI state seam without executing effects.</summary>
internal sealed class SafeOutcomeRenderDispatcher : IDispatcher
{
    /// <inheritdoc/>
    public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public void Dispatch(object action) { }
}
