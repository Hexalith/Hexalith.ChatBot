namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Metadata required for operational queues so loading state is bounded and inspectable.
/// </summary>
/// <param name="Mode">Queue loading mode.</param>
/// <param name="ActiveFilterDescription">Stable active filter description.</param>
/// <param name="ResultCount">Current result count.</param>
/// <param name="PageNumber">Current page number when paginated.</param>
/// <param name="PageSize">Current page size or virtualization window size.</param>
public sealed record ChatBotQueueLoadingContract(
    ChatBotQueueLoadingMode Mode,
    string ActiveFilterDescription,
    int ResultCount,
    int PageNumber,
    int PageSize)
{
    /// <summary>Gets a value indicating whether this is a valid operational queue contract.</summary>
    public bool IsValidOperationalQueueContract
        => ChatBotQueueLoadingPolicy.IsPermittedDefault(Mode)
            && !string.IsNullOrWhiteSpace(ActiveFilterDescription)
            && ResultCount >= 0
            && PageNumber >= 1
            && PageSize >= 1;

    /// <summary>Gets a value indicating whether active filters expose visible summary text and a result count.</summary>
    public bool HasVisibleActiveFilterSummary
        => !string.IsNullOrWhiteSpace(ActiveFilterDescription)
            && ResultCount >= 0;
}
