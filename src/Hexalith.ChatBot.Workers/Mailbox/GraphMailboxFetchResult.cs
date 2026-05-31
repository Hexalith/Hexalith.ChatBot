namespace Hexalith.ChatBot.Workers.Mailbox;

public sealed record GraphMailboxFetchResult(
    GraphMailboxFetchResultKind Kind,
    GraphMailboxMessage? Message,
    string ReasonCode)
{
    public static GraphMailboxFetchResult Found(GraphMailboxMessage message)
        => new(GraphMailboxFetchResultKind.Found, message, "found");

    public static GraphMailboxFetchResult RetryableFailure(string reasonCode)
        => new(GraphMailboxFetchResultKind.RetryableFailure, null, reasonCode);

    public static GraphMailboxFetchResult PermissionRevoked()
        => new(GraphMailboxFetchResultKind.PermissionRevoked, null, "graph_permission_revoked");
}
