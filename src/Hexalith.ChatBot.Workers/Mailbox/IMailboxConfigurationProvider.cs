namespace Hexalith.ChatBot.Workers.Mailbox;

/// <summary>
/// Tenant-scoped mailbox configuration lookup used before Graph fetches.
/// </summary>
public interface IMailboxConfigurationProvider
{
    ValueTask<ControlledMailboxPattern?> ResolvePatternAsync(
        string tenantId,
        string notificationMailboxId,
        CancellationToken cancellationToken);
}
