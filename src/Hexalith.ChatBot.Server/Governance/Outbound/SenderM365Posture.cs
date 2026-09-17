using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal sealed record SenderM365Posture(
    string MailboxId,
    bool IsMailboxOwner,
    bool HasOwnMailboxMailSend,
    bool HasSharedMailboxSendPosture,
    bool HasSendOnBehalfPosture,
    bool HasApplicationMailSend,
    string EvidenceRef)
{
    public bool HasAnySendPosture
        => HasOwnMailboxMailSend ||
            HasSharedMailboxSendPosture ||
            HasSendOnBehalfPosture ||
            HasApplicationMailSend;
}
