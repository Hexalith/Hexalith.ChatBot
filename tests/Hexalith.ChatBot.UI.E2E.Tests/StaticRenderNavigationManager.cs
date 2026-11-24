using Microsoft.AspNetCore.Components;

namespace Hexalith.ChatBot.UI.E2E.Tests;

internal sealed class StaticRenderNavigationManager : NavigationManager
{
    public StaticRenderNavigationManager()
        => Initialize("http://localhost/", "http://localhost/governed-operations");

    protected override void NavigateToCore(string uri, bool forceLoad)
        => throw new InvalidOperationException("This rendering fixture does not support route changes.");
}
