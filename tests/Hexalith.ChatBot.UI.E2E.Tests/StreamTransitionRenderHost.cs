using Hexalith.ChatBot.UI.Components.Governed;
using Hexalith.ChatBot.UI.State.ProjectConversation;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Provides a mounted stream whose parameters can change during a render test.</summary>
internal sealed class StreamTransitionRenderHost : ComponentBase
{
    internal static StreamTransitionRenderHost? Instance { get; private set; }

    private IReadOnlyList<ProjectConversationItemModel> _items = [];

    private IReadOnlySet<string>? _currentPageItemIds;

    internal Task UpdateAsync(IReadOnlyList<ProjectConversationItemModel> items, IReadOnlySet<string> currentPageItemIds)
        => InvokeAsync(() =>
        {
            _items = items;
            _currentPageItemIds = currentPageItemIds;
            StateHasChanged();
        });

    internal static void Clear() => Instance = null;

    protected override void OnInitialized() => Instance = this;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<ChatBotConversationStream>(0);
        builder.AddComponentParameter(1, nameof(ChatBotConversationStream.Items), _items);
        builder.AddComponentParameter(2, nameof(ChatBotConversationStream.CurrentPageItemIds), _currentPageItemIds);
        builder.CloseComponent();
    }
}
