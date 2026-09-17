using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal enum SenderAuthorityIntent
{
    DraftOnly,
    AuthenticatedUserSend,
    SharedMailboxSend,
    SendOnBehalf,
    ApprovedServiceSend,
}
