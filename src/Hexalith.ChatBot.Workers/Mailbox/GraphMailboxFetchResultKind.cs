namespace Hexalith.ChatBot.Workers.Mailbox;

public enum GraphMailboxFetchResultKind
{
    Found,
    RetryableFailure,
    PermissionRevoked,
}
