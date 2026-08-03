using Hexalith.ChatBot.Workers.Mailbox;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>Controlled topology-composed Graph boundary used only by the opted-in recovery sandbox.</summary>
internal sealed class ControlledGraphMailboxMessageSource(RecoverySubscriptionSimulatorState state) : IGraphMailboxMessageSource
{
    /// <inheritdoc />
    public ValueTask<GraphMailboxFetchResult> FetchMessageAsync(
        GraphMailboxNotification notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();
        if (state.IsFaulted())
        {
            return ValueTask.FromResult(GraphMailboxFetchResult.RetryableFailure("graph_subscription_expired"));
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string laneKey = notification.ProviderMessageId;
        GraphMailboxMessage message = new(
            notification.MailboxId,
            notification.ProviderMessageId,
            $"recovery-internet-message:{laneKey}",
            $"recovery-conversation:{laneKey}",
            ThreadId: null,
            From: new GraphMailboxParticipant("sender@example.invalid", "Recovery Sender"),
            Sender: null,
            ReplyTo: [],
            Recipients: [new GraphMailboxRecipient("recovery@example.invalid", "Recovery Mailbox", "to")],
            now,
            SentAt: now,
            CreatedAt: now,
            SourceTimezone: "UTC",
            Attachments: [new GraphMailboxAttachment($"attachment:{laneKey}", "recovery.bin", "application/octet-stream", 1)],
            InternetMessageHeaders: []);
        return ValueTask.FromResult(GraphMailboxFetchResult.Found(message));
    }
}
