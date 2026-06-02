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

internal sealed record SenderProjectAuthorityEvidence(
    bool HasProjectAuthority,
    IReadOnlyList<string> Scopes,
    string EvidenceRef)
{
    public bool HasScope(string scope)
        => Scopes.Contains(scope, StringComparer.Ordinal);
}

internal sealed record SenderSharedMailboxMembershipEvidence(
    string SharedMailboxId,
    string MemberRequesterId,
    bool IsMemberAtSendTime,
    string EvidenceRef);

internal sealed record SenderDelegationEvidence(
    string DelegateRequesterId,
    string PrincipalForId,
    bool RevokedSincePolicySnapshot,
    string EvidenceRef);

internal sealed record SenderServiceClientGrantEvidence(
    string ServiceClientId,
    bool HasOutboundGrant,
    string EvidenceRef);

internal sealed record SenderApprovalChainEvidence(
    string? ApprovalId,
    bool HasPairedApprovalRecord,
    string EvidenceRef);
