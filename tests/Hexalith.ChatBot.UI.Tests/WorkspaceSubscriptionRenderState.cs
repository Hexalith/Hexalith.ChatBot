using Fluxor;

using Hexalith.ChatBot.UI.State.ProjectConversation;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Provides mutable conversation snapshots to an actual mounted workspace.</summary>
/// <param name="value">The initial conversation snapshot.</param>
internal sealed class WorkspaceSubscriptionRenderState(ProjectConversationState value) : IState<ProjectConversationState>
{
    /// <summary>Gets or sets the current snapshot; tests explicitly render parameter changes.</summary>
    public ProjectConversationState Value { get; set; } = value;

    /// <inheritdoc/>
    public event EventHandler? StateChanged
    {
        add { }
        remove { }
    }
}
