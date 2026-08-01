using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Workers.Mailbox;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Provides the one allowlisted validation mailbox to the sandbox Worker.</summary>
internal sealed class RecoveryMailboxConfigurationProvider : IMailboxConfigurationProvider
{
    /// <inheritdoc />
    public ValueTask<ControlledMailboxPattern?> ResolvePatternAsync(
        string tenantId,
        string notificationMailboxId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<ControlledMailboxPattern?>(new(
            "recovery-mailbox-001",
            "recovery-graph-v1",
            MailboxSourceControlState.Active));
}
