using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal sealed record SenderSharedMailboxMembershipEvidence(
    string SharedMailboxId,
    string MemberRequesterId,
    bool IsMemberAtSendTime,
    string EvidenceRef);
