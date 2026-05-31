namespace Hexalith.ChatBot.Workers.Mailbox;

/// <summary>
/// Port for Microsoft Graph notification/delta fetches. Implementations must request only <c>Mail.Read</c>
/// for the controlled mailbox and fetch selected message fields needed by Story 2.1.
/// </summary>
public interface IGraphMailboxMessageSource
{
    ValueTask<GraphMailboxFetchResult> FetchMessageAsync(GraphMailboxNotification notification, CancellationToken cancellationToken);
}
