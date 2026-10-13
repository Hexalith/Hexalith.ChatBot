using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.UI.Components.Governed;
using Hexalith.ChatBot.UI.Design;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Hexalith.ChatBot.UI.E2E.Tests;

/// <summary>Mounts one banner while changing the identity of its otherwise identical failure.</summary>
internal sealed class BannerTransitionRenderHost : ComponentBase
{
    /// <summary>Gets the mounted instance for the render test.</summary>
    internal static BannerTransitionRenderHost? Instance { get; private set; }

    private string? _attemptKey;

    /// <summary>Updates the failure identity without replacing the banner instance.</summary>
    /// <param name="attemptKey">The identity of this user attempt.</param>
    internal Task UpdateAsync(string attemptKey)
        => InvokeAsync(() =>
        {
            _attemptKey = attemptKey;
            StateHasChanged();
        });

    /// <summary>Clears the retained test instance.</summary>
    internal static void Clear() => Instance = null;

    /// <inheritdoc/>
    protected override void OnInitialized() => Instance = this;

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_attemptKey is null)
        {
            return;
        }

        builder.OpenComponent<ChatBotStatusBanner>(0);
        builder.AddComponentParameter(1, nameof(ChatBotStatusBanner.Kind), ChatBotFeedbackKind.Danger);
        builder.AddComponentParameter(2, nameof(ChatBotStatusBanner.MessageCatalogCode), ChatBotMessageCodes.AuthorizationDenied);
        builder.AddComponentParameter(3, nameof(ChatBotStatusBanner.StateFamily), ChatBotFeedbackStateFamily.BlockedAction);
        builder.AddComponentParameter(4, nameof(ChatBotStatusBanner.AnnouncementKey), _attemptKey);
        builder.AddComponentParameter(5, nameof(ChatBotStatusBanner.StableId), "mounted-failure");
        // A fragment keeps repeated parent renders observable at the child parameter boundary.
        // Coded failures continue to discard this caller content and show only catalog copy.
        builder.AddComponentParameter(6, nameof(ChatBotStatusBanner.ChildContent),
            (RenderFragment)(child => child.AddContent(0, _attemptKey)));
        builder.CloseComponent();
    }
}
