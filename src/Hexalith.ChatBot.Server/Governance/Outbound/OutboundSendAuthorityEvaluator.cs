using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal static class OutboundSendAuthorityEvaluator
{
    public const string ProjectOutboundSendScope = "outbound-send";
    public const string MailboxIdClaim = "chatbot:m365-mailbox";
    public const string MailboxOwnerClaim = "chatbot:m365-mailbox-owner";
    public const string OwnMailboxMailSendClaim = "chatbot:m365-own-mail-send";
    public const string SharedMailboxSendClaim = "chatbot:m365-shared-mailbox-send";
    public const string SharedMailboxMemberClaim = "chatbot:shared-mailbox-member";
    public const string SendOnBehalfClaim = "chatbot:m365-send-on-behalf";
    public const string DelegationPrincipalClaim = "chatbot:send-on-behalf-principal";
    public const string ApplicationMailSendClaim = "chatbot:m365-application-mail-send";
    public const string EvidenceFreshnessClaim = "chatbot:outbound-evidence-freshness";

    public static SenderAuthorityClassificationResult Classify(
        ExecuteApprovedOutboundDraft command,
        ClaimsPrincipal principal,
        string tenantId,
        ServiceClientGrantEvidence? serviceClientGrantEvidence)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        string mailboxId = ClaimValue(principal, MailboxIdClaim) ?? "unavailable";
        SenderAuthorityClassificationRequest request = new(
            IntentFor(command.SenderAuthorityClass),
            tenantId,
            command.RequesterId,
            command.PolicySnapshotId,
            TenantPolicy(principal),
            new SenderM365Posture(
                mailboxId,
                principal.HasClaim(MailboxOwnerClaim, mailboxId),
                principal.HasClaim(OwnMailboxMailSendClaim, "true"),
                principal.HasClaim(SharedMailboxSendClaim, mailboxId),
                principal.HasClaim(SendOnBehalfClaim, mailboxId),
                principal.HasClaim(ApplicationMailSendClaim, "true"),
                $"m365:{mailboxId}"),
            ProjectAuthority(command.ProjectId, principal),
            SharedMailboxMembership(command, principal, mailboxId),
            Delegation(command, principal),
            ServiceClientGrant(serviceClientGrantEvidence),
            new SenderApprovalChainEvidence(command.ApprovalId, HasApprovalRef(command), $"approval:{command.ApprovalId}"));

        SenderAuthorityClassificationResult result = SenderAuthorityClassifier.Classify(request);
        string currentEvidenceFreshness = CurrentEvidenceFreshness(principal);
        return string.Equals(currentEvidenceFreshness, "fresh", StringComparison.Ordinal)
            ? result
            : result with
            {
                EvidenceFreshness = currentEvidenceFreshness,
                DenialReason = ChatBotRefusalReasonCodes.EvidenceExpired,
            };
    }

    public static string SafeDenialReason(ExecuteApprovedOutboundDraft command, ClaimsPrincipal principal, SenderAuthorityClassificationResult result)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(result);

        if (!HasProjectAuthority(command.ProjectId, principal) ||
            !HasProjectScope(command.ProjectId, principal, ProjectOutboundSendScope))
        {
            return ChatBotDisabledActionReasons.InsufficientAuthority;
        }

        return result.DenialReason ?? ChatBotDisabledActionReasons.PolicyBlocked;
    }

    private static string CurrentEvidenceFreshness(ClaimsPrincipal principal)
    {
        string? value = ClaimValue(principal, EvidenceFreshnessClaim);
        return value is "stale" or "expired" ? value : "fresh";
    }

    private static SenderAuthorityIntent IntentFor(SenderAuthorityClass authorityClass)
        => authorityClass switch
        {
            SenderAuthorityClass.AuthenticatedUserSend => SenderAuthorityIntent.AuthenticatedUserSend,
            SenderAuthorityClass.SharedMailboxSend => SenderAuthorityIntent.SharedMailboxSend,
            SenderAuthorityClass.SendOnBehalf => SenderAuthorityIntent.SendOnBehalf,
            SenderAuthorityClass.ApprovedServiceSend => SenderAuthorityIntent.ApprovedServiceSend,
            _ => SenderAuthorityIntent.DraftOnly,
        };

    private static SenderTenantOutboundPolicy TenantPolicy(ClaimsPrincipal principal)
    {
        string[] values = principal.FindAll(OutboundDraftAuthorityEvaluator.TenantOutboundPolicyClaim)
            .Select(static claim => claim.Value)
            .ToArray();
        return new SenderTenantOutboundPolicy(
            values.Contains(OutboundDraftAuthorityEvaluator.TenantPolicyDraftOnlyValue, StringComparer.Ordinal),
            values.Contains("authenticated-user-send", StringComparer.Ordinal),
            values.Contains("shared-mailbox-send", StringComparer.Ordinal),
            values.Contains("send-on-behalf", StringComparer.Ordinal),
            values.Contains("approved-service-send", StringComparer.Ordinal));
    }

    private static SenderProjectAuthorityEvidence ProjectAuthority(string projectId, ClaimsPrincipal principal)
        => new(
            HasProjectAuthority(projectId, principal),
            ProjectScopes(projectId, principal),
            HasProjectScope(projectId, principal, ProjectOutboundSendScope)
                ? $"project-authority:{ProjectOutboundSendScope}"
                : "project-authority:unavailable");

    private static SenderSharedMailboxMembershipEvidence? SharedMailboxMembership(
        ExecuteApprovedOutboundDraft command,
        ClaimsPrincipal principal,
        string mailboxId)
        => principal.HasClaim(SharedMailboxMemberClaim, mailboxId)
            ? new SenderSharedMailboxMembershipEvidence(mailboxId, command.RequesterId, IsMemberAtSendTime: true, $"shared-mailbox-member:{mailboxId}")
            : null;

    private static SenderDelegationEvidence? Delegation(ExecuteApprovedOutboundDraft command, ClaimsPrincipal principal)
    {
        string? principalFor = ClaimValue(principal, DelegationPrincipalClaim);
        return string.IsNullOrWhiteSpace(principalFor)
            ? null
            : new SenderDelegationEvidence(command.RequesterId, principalFor, RevokedSincePolicySnapshot: false, $"delegation:{principalFor}");
    }

    private static SenderServiceClientGrantEvidence? ServiceClientGrant(ServiceClientGrantEvidence? evidence)
        => evidence is null
            ? null
            : new SenderServiceClientGrantEvidence(
                evidence.ServiceClientId,
                evidence.Scopes.Contains(ProjectOutboundSendScope, StringComparer.Ordinal),
                $"grant:{evidence.GrantId}");

    private static bool HasApprovalRef(ExecuteApprovedOutboundDraft command)
        => !string.IsNullOrWhiteSpace(command.ApprovalId) && command.ExpectedApprovalSourceVersion > 0;

    private static bool HasProjectAuthority(string projectId, ClaimsPrincipal principal)
        => principal
            .FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Any(value => string.Equals(value, "*", StringComparison.Ordinal) || string.Equals(value, projectId, StringComparison.Ordinal));

    private static bool HasProjectScope(string projectId, ClaimsPrincipal principal, string scope)
        => ProjectScopes(projectId, principal).Contains(scope, StringComparer.Ordinal);

    private static string[] ProjectScopes(string projectId, ClaimsPrincipal principal)
        => principal
            .FindAll(OutboundDraftAuthorityEvaluator.ProjectScopeClaim)
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Split(':', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2 &&
                (string.Equals(parts[0], "*", StringComparison.Ordinal) || string.Equals(parts[0], projectId, StringComparison.Ordinal)))
            .Select(static parts => parts[1])
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static string? ClaimValue(ClaimsPrincipal principal, string claimType)
        => principal.Claims.FirstOrDefault(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal))?.Value;
}
