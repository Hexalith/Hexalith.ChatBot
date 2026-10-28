using Hexalith.ChatBot.UI.Components.Governed;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Captures the actual mounted workspace for deterministic callback and disposal tests.</summary>
internal sealed class WorkspaceLifecycleRenderHost : ComponentBase
{
    /// <summary>Gets or sets the callback receiving the mounted workspace.</summary>
    [Parameter]
    public Action<ChatBotProjectConversationWorkspace>? Capture { get; set; }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<ChatBotProjectConversationWorkspace>(0);
        builder.AddComponentParameter(1, nameof(ChatBotProjectConversationWorkspace.HeadingText), "Project conversation");
        builder.AddComponentReferenceCapture(2, instance => Capture?.Invoke((ChatBotProjectConversationWorkspace)instance));
        builder.CloseComponent();
    }
}
