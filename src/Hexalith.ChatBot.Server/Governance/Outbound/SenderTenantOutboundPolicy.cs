using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal sealed record SenderTenantOutboundPolicy(
    bool AllowDraftOnly,
    bool AllowAuthenticatedUserSend,
    bool AllowSharedMailboxSend,
    bool AllowSendOnBehalf,
    bool AllowApprovedServiceSend)
{
    public bool Allows(SenderAuthorityClass authorityClass)
        => authorityClass switch
        {
            SenderAuthorityClass.DraftOnly => AllowDraftOnly,
            SenderAuthorityClass.AuthenticatedUserSend => AllowAuthenticatedUserSend,
            SenderAuthorityClass.SharedMailboxSend => AllowSharedMailboxSend,
            SenderAuthorityClass.SendOnBehalf => AllowSendOnBehalf,
            SenderAuthorityClass.ApprovedServiceSend => AllowApprovedServiceSend,
            _ => false,
        };
}
