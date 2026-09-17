using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

internal interface IOutboundMailboxSender
{
    ValueTask<OutboundMailboxSendResult> SendAsync(
        OutboundMailboxSendRequest request,
        CancellationToken cancellationToken = default);
}

