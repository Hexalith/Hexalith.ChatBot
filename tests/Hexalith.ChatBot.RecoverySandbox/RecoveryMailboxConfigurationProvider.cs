using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Workers.Mailbox;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Provides the one allowlisted validation mailbox to the sandbox Worker.</summary>
internal sealed class RecoveryMailboxConfigurationProvider(string allowlistedMailboxId) : IMailboxConfigurationProvider
{
    /// <inheritdoc />
    public ValueTask<ControlledMailboxPattern?> ResolvePatternAsync(
        string tenantId,
        string notificationMailboxId,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(notificationMailboxId, allowlistedMailboxId, StringComparison.Ordinal))
        {
            return ValueTask.FromResult<ControlledMailboxPattern?>(null);
        }

        return ValueTask.FromResult<ControlledMailboxPattern?>(new(
            allowlistedMailboxId,
            "recovery-graph-v1",
            MailboxSourceControlState.Active));
    }
}
