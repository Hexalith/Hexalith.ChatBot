using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Workers.Mailbox;

public interface IMailboxSourceControlProjection
{
    ValueTask<MailboxSourceControlState?> GetControlStateAsync(
        string tenantId,
        string mailboxSourceRef,
        CancellationToken cancellationToken);
}
