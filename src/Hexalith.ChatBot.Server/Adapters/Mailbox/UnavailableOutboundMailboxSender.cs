namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

internal sealed class UnavailableOutboundMailboxSender : IOutboundMailboxSender
{
    public ValueTask<OutboundMailboxSendResult> SendAsync(
        OutboundMailboxSendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(OutboundMailboxSendResult.Unavailable("outbound_adapter_unavailable"));
    }
}

