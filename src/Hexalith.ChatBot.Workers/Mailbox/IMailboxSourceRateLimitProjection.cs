using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Workers.Mailbox;

public interface IMailboxSourceRateLimitProjection
{
    ValueTask<MailboxRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string mailboxSourceRef,
        CancellationToken cancellationToken);
}
