using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal static class SenderAuthorityClassifier
{
    private const string OutboundDraftScope = "outbound-draft";
    private const string OutboundSendScope = "outbound-send";

    public static SenderAuthorityClassificationResult Classify(SenderAuthorityClassificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Intent switch
        {
            SenderAuthorityIntent.DraftOnly => ClassifyDraftOnly(request),
            SenderAuthorityIntent.AuthenticatedUserSend => ClassifyAuthenticatedUserSend(request),
            SenderAuthorityIntent.SharedMailboxSend => ClassifySharedMailboxSend(request),
            SenderAuthorityIntent.SendOnBehalf => ClassifySendOnBehalf(request),
            SenderAuthorityIntent.ApprovedServiceSend => ClassifyApprovedServiceSend(request),
            _ => Denied(request, SenderAuthorityClass.DraftOnly, ChatBotDisabledActionReasons.PolicyBlocked),
        };
    }

    private static SenderAuthorityClassificationResult ClassifyDraftOnly(SenderAuthorityClassificationRequest request)
    {
        const SenderAuthorityClass authorityClass = SenderAuthorityClass.DraftOnly;
        if (!request.TenantPolicy.Allows(authorityClass) ||
            request.M365Posture.HasAnySendPosture ||
            !HasProjectScope(request, OutboundDraftScope))
        {
            return Denied(request, authorityClass, ChatBotDisabledActionReasons.PolicyBlocked);
        }

        return Allowed(request, authorityClass);
    }

    private static SenderAuthorityClassificationResult ClassifyAuthenticatedUserSend(SenderAuthorityClassificationRequest request)
    {
        const SenderAuthorityClass authorityClass = SenderAuthorityClass.AuthenticatedUserSend;
        if (!request.TenantPolicy.Allows(authorityClass) ||
            !request.M365Posture.IsMailboxOwner ||
            !request.M365Posture.HasOwnMailboxMailSend ||
            request.Delegation is not null ||
            !HasProjectScope(request, OutboundSendScope))
        {
            return Denied(request, authorityClass, ChatBotDisabledActionReasons.PolicyBlocked);
        }

        return Allowed(request, authorityClass, mailboxId: request.M365Posture.MailboxId);
    }

    private static SenderAuthorityClassificationResult ClassifySharedMailboxSend(SenderAuthorityClassificationRequest request)
    {
        const SenderAuthorityClass authorityClass = SenderAuthorityClass.SharedMailboxSend;
        SenderSharedMailboxMembershipEvidence? membership = request.SharedMailboxMembership;
        if (membership is not null &&
            string.Equals(membership.MemberRequesterId, request.RequesterId, StringComparison.Ordinal) &&
            !membership.IsMemberAtSendTime)
        {
            return Denied(
                request,
                authorityClass,
                SenderAuthorityConflictReasons.MembershipRevoked,
                mailboxId: membership.SharedMailboxId,
                extraRefs: [membership.EvidenceRef]);
        }

        if (!request.TenantPolicy.Allows(authorityClass) ||
            !request.M365Posture.HasSharedMailboxSendPosture ||
            membership is null ||
            !string.Equals(membership.MemberRequesterId, request.RequesterId, StringComparison.Ordinal) ||
            !string.Equals(membership.SharedMailboxId, request.M365Posture.MailboxId, StringComparison.Ordinal) ||
            !membership.IsMemberAtSendTime ||
            !HasProjectScope(request, OutboundSendScope))
        {
            return Denied(request, authorityClass, ChatBotDisabledActionReasons.PolicyBlocked);
        }

        return Allowed(
            request,
            authorityClass,
            mailboxId: membership.SharedMailboxId,
            extraRefs: [membership.EvidenceRef]);
    }

    private static SenderAuthorityClassificationResult ClassifySendOnBehalf(SenderAuthorityClassificationRequest request)
    {
        const SenderAuthorityClass authorityClass = SenderAuthorityClass.SendOnBehalf;
        SenderDelegationEvidence? delegation = request.Delegation;
        if (delegation is not null &&
            !string.Equals(delegation.DelegateRequesterId, request.RequesterId, StringComparison.Ordinal))
        {
            return Denied(
                request,
                authorityClass,
                SenderAuthorityConflictReasons.DelegationMismatch,
                principalForId: delegation.PrincipalForId,
                extraRefs: [delegation.EvidenceRef]);
        }

        if (!request.TenantPolicy.Allows(authorityClass) ||
            !request.M365Posture.HasSendOnBehalfPosture ||
            delegation is null ||
            delegation.RevokedSincePolicySnapshot ||
            !HasProjectScope(request, OutboundSendScope))
        {
            return Denied(
                request,
                authorityClass,
                ChatBotDisabledActionReasons.PolicyBlocked,
                principalForId: delegation?.PrincipalForId,
                extraRefs: EvidenceRefs(delegation?.EvidenceRef));
        }

        return Allowed(
            request,
            authorityClass,
            mailboxId: request.M365Posture.MailboxId,
            principalForId: delegation.PrincipalForId,
            extraRefs: [delegation.EvidenceRef]);
    }

    private static SenderAuthorityClassificationResult ClassifyApprovedServiceSend(SenderAuthorityClassificationRequest request)
    {
        const SenderAuthorityClass authorityClass = SenderAuthorityClass.ApprovedServiceSend;
        SenderServiceClientGrantEvidence? grant = request.ServiceClientGrant;
        SenderApprovalChainEvidence? approval = request.ApprovalChain;
        if (!request.TenantPolicy.Allows(authorityClass) ||
            !request.M365Posture.HasApplicationMailSend ||
            !HasProjectScope(request, OutboundSendScope))
        {
            return Denied(
                request,
                authorityClass,
                ChatBotDisabledActionReasons.PolicyBlocked,
                serviceClientId: grant?.ServiceClientId,
                approvalId: approval?.ApprovalId,
                extraRefs: EvidenceRefs(grant?.EvidenceRef, approval?.EvidenceRef));
        }

        if (grant is null ||
            !grant.HasOutboundGrant)
        {
            return Denied(
                request,
                authorityClass,
                ChatBotDisabledActionReasons.PolicyBlocked,
                serviceClientId: grant?.ServiceClientId,
                extraRefs: EvidenceRefs(grant?.EvidenceRef));
        }

        if (approval is null ||
            !approval.HasPairedApprovalRecord ||
            string.IsNullOrWhiteSpace(approval.ApprovalId))
        {
            return Denied(
                request,
                authorityClass,
                SenderAuthorityConflictReasons.ApprovalMissing,
                serviceClientId: grant.ServiceClientId,
                approvalId: approval?.HasPairedApprovalRecord == true ? approval.ApprovalId : null,
                extraRefs: EvidenceRefs(grant.EvidenceRef, approval?.HasPairedApprovalRecord == true ? approval.EvidenceRef : null));
        }

        return Allowed(
            request,
            authorityClass,
            serviceClientId: grant.ServiceClientId,
            approvalId: approval.ApprovalId,
            extraRefs: [grant.EvidenceRef, approval.EvidenceRef]);
    }

    private static bool HasProjectScope(SenderAuthorityClassificationRequest request, string scope)
        => request.ProjectAuthority.HasProjectAuthority && request.ProjectAuthority.HasScope(scope);

    private static SenderAuthorityClassificationResult Allowed(
        SenderAuthorityClassificationRequest request,
        SenderAuthorityClass authorityClass,
        string? mailboxId = null,
        string? serviceClientId = null,
        string? principalForId = null,
        string? approvalId = null,
        IReadOnlyList<string>? extraRefs = null)
        => Result(
            request,
            authorityClass,
            denialReason: null,
            mailboxId,
            serviceClientId,
            principalForId,
            approvalId,
            extraRefs);

    private static SenderAuthorityClassificationResult Denied(
        SenderAuthorityClassificationRequest request,
        SenderAuthorityClass authorityClass,
        string denialReason,
        string? mailboxId = null,
        string? serviceClientId = null,
        string? principalForId = null,
        string? approvalId = null,
        IReadOnlyList<string>? extraRefs = null)
        => Result(
            request,
            authorityClass,
            denialReason,
            mailboxId,
            serviceClientId,
            principalForId,
            approvalId,
            extraRefs);

    private static SenderAuthorityClassificationResult Result(
        SenderAuthorityClassificationRequest request,
        SenderAuthorityClass authorityClass,
        string? denialReason,
        string? mailboxId,
        string? serviceClientId,
        string? principalForId,
        string? approvalId,
        IReadOnlyList<string>? extraRefs)
    {
        string? mailboxRef = string.IsNullOrWhiteSpace(mailboxId) ? null : $"mailbox:{mailboxId}";
        string? serviceClientRef = string.IsNullOrWhiteSpace(serviceClientId) ? null : $"service-client:{serviceClientId}";
        string? principalForRef = string.IsNullOrWhiteSpace(principalForId) ? null : $"principal-for:{principalForId}";
        string? approvalRef = string.IsNullOrWhiteSpace(approvalId) ? null : $"approval:{approvalId}";
        string policySnapshotRef = $"policy-snapshot:{request.PolicySnapshotId}";

        List<string> refs =
        [
            $"sender-authority:{SenderAuthorityClasses.ToWireValue(authorityClass)}",
            policySnapshotRef,
            request.M365Posture.EvidenceRef,
            request.ProjectAuthority.EvidenceRef,
        ];
        AddIfPresent(refs, mailboxRef);
        AddIfPresent(refs, serviceClientRef);
        AddIfPresent(refs, principalForRef);
        AddIfPresent(refs, approvalRef);
        if (extraRefs is not null)
        {
            foreach (string evidenceRef in extraRefs)
            {
                AddIfPresent(refs, evidenceRef);
            }
        }

        return new SenderAuthorityClassificationResult(
            authorityClass,
            $"requester:{request.RequesterId}",
            mailboxRef,
            serviceClientRef,
            principalForRef,
            approvalRef,
            policySnapshotRef,
            "fresh",
            refs.Distinct(StringComparer.Ordinal).ToArray(),
            denialReason);
    }

    private static IReadOnlyList<string> EvidenceRefs(params string?[] evidenceRefs)
        => evidenceRefs.Where(static evidenceRef => !string.IsNullOrWhiteSpace(evidenceRef)).Select(static evidenceRef => evidenceRef!).ToArray();

    private static void AddIfPresent(List<string> refs, string? evidenceRef)
    {
        if (!string.IsNullOrWhiteSpace(evidenceRef))
        {
            refs.Add(evidenceRef);
        }
    }
}
