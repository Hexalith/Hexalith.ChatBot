using Fluxor;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Supplies deterministic UI state to actual parent-component render tests.</summary>
/// <typeparam name="T">The state type consumed by the component.</typeparam>
/// <param name="value">The state snapshot to render.</param>
internal sealed class SafeOutcomeRenderState<T>(T value) : IState<T> where T : class
{
    /// <inheritdoc/>
    public T Value { get; } = value;

    /// <inheritdoc/>
    public event EventHandler? StateChanged
    {
        add { }
        remove { }
    }
}
