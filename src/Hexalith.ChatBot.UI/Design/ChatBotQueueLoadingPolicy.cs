namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Queue loading policy that rejects infinite-scroll operational defaults.
/// </summary>
public static class ChatBotQueueLoadingPolicy
{
    /// <summary>Returns whether the queue mode is permitted as an operational queue default.</summary>
    /// <param name="mode">Queue loading mode.</param>
    /// <returns><see langword="true" /> for pagination or virtualization with stable state.</returns>
    public static bool IsPermittedDefault(ChatBotQueueLoadingMode mode)
        => mode is ChatBotQueueLoadingMode.Pagination or ChatBotQueueLoadingMode.VirtualizedListWithStableFilters;
}
