namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Queue loading modes allowed or explicitly rejected by the governed UI foundation.
/// </summary>
public enum ChatBotQueueLoadingMode
{
    Pagination,
    VirtualizedListWithStableFilters,
    InfiniteScroll,
}
