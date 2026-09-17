using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Workers.Mailbox;

public sealed class StaticMailboxSourceControlProjection(MailboxSourceControlState? state = null) : IMailboxSourceControlProjection
{
    public ValueTask<MailboxSourceControlState?> GetControlStateAsync(
        string tenantId,
        string mailboxSourceRef,
        CancellationToken cancellationToken)
        => ValueTask.FromResult(state);
}
