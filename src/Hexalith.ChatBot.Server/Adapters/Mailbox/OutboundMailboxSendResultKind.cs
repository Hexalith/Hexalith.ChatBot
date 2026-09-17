using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Mailbox;

internal enum OutboundMailboxSendResultKind
{
    Sent,
    Rejected,
    Unavailable,
}
