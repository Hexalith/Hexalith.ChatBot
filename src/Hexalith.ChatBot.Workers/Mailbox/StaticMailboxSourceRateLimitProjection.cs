using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Workers.Mailbox;

public sealed class StaticMailboxSourceRateLimitProjection(MailboxRateLimitState? state = null) : IMailboxSourceRateLimitProjection
{
    public ValueTask<MailboxRateLimitState?> GetRateLimitAsync(
        string tenantId,
        string mailboxSourceRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(state);
}
