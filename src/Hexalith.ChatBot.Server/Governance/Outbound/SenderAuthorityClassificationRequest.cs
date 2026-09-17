using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal sealed record SenderAuthorityClassificationRequest(
    SenderAuthorityIntent Intent,
    string TenantId,
    string RequesterId,
    string PolicySnapshotId,
    SenderTenantOutboundPolicy TenantPolicy,
    SenderM365Posture M365Posture,
    SenderProjectAuthorityEvidence ProjectAuthority,
    SenderSharedMailboxMembershipEvidence? SharedMailboxMembership,
    SenderDelegationEvidence? Delegation,
    SenderServiceClientGrantEvidence? ServiceClientGrant,
    SenderApprovalChainEvidence? ApprovalChain);
