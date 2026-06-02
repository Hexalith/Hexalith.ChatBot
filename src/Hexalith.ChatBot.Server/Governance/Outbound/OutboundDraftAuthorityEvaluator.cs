using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal static class OutboundDraftAuthorityEvaluator
{
    public const string ProjectScopeClaim = "chatbot:project-scope";
    public const string TenantOutboundPolicyClaim = "chatbot:tenant-outbound-policy";
    public const string TenantPolicyDraftOnlyValue = "draft-only";
    public const string ProjectOutboundDraftScope = "outbound-draft";
    public const string M365SendPostureEvidenceRef = "m365:send-posture-present";
    public const string NoM365SendPostureEvidenceRef = "m365:none";

    public static SenderAuthorityClassificationResult Classify(
        CreateOutboundDraft command,
        ClaimsPrincipal principal,
        string tenantId)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        SenderAuthorityClassificationRequest request = new(
            SenderAuthorityIntent.DraftOnly,
            tenantId,
            command.RequesterId,
            command.PolicySnapshotId,
            TenantPolicy(principal),
            new SenderM365Posture(
                MailboxId: "none",
                IsMailboxOwner: false,
                HasOwnMailboxMailSend: command.HasM365SendPosture,
                HasSharedMailboxSendPosture: false,
                HasSendOnBehalfPosture: false,
                HasApplicationMailSend: false,
                EvidenceRef: command.HasM365SendPosture ? M365SendPostureEvidenceRef : NoM365SendPostureEvidenceRef),
            ProjectAuthority(command.ProjectId, principal),
            SharedMailboxMembership: null,
            Delegation: null,
            ServiceClientGrant: null,
            ApprovalChain: null);

        return SenderAuthorityClassifier.Classify(request);
    }

    public static string SafeDenialReason(CreateOutboundDraft command, ClaimsPrincipal principal, SenderAuthorityClassificationResult result)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(result);

        if (!HasProjectAuthority(command.ProjectId, principal) ||
            !HasProjectScope(command.ProjectId, principal, ProjectOutboundDraftScope) ||
            command.SenderAuthorityClass is not SenderAuthorityClass.DraftOnly)
        {
            return ChatBotDisabledActionReasons.InsufficientAuthority;
        }

        return ChatBotDisabledActionReasons.PolicyBlocked;
    }

    private static SenderTenantOutboundPolicy TenantPolicy(ClaimsPrincipal principal)
    {
        bool allowsDraftOnly = principal
            .FindAll(TenantOutboundPolicyClaim)
            .Any(static claim => string.Equals(claim.Value, TenantPolicyDraftOnlyValue, StringComparison.Ordinal));
        return new SenderTenantOutboundPolicy(
            allowsDraftOnly,
            AllowAuthenticatedUserSend: false,
            AllowSharedMailboxSend: false,
            AllowSendOnBehalf: false,
            AllowApprovedServiceSend: false);
    }

    private static SenderProjectAuthorityEvidence ProjectAuthority(string projectId, ClaimsPrincipal principal)
        => new(
            HasProjectAuthority(projectId, principal),
            ProjectScopes(projectId, principal),
            HasProjectScope(projectId, principal, ProjectOutboundDraftScope)
                ? $"project-authority:{ProjectOutboundDraftScope}"
                : "project-authority:unavailable");

    private static bool HasProjectAuthority(string projectId, ClaimsPrincipal principal)
        => principal
            .FindAll(ParticipantAuthorizationStage.ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Any(value => string.Equals(value, "*", StringComparison.Ordinal) || string.Equals(value, projectId, StringComparison.Ordinal));

    private static bool HasProjectScope(string projectId, ClaimsPrincipal principal, string scope)
        => ProjectScopes(projectId, principal).Contains(scope, StringComparer.Ordinal);

    private static string[] ProjectScopes(string projectId, ClaimsPrincipal principal)
        => principal
            .FindAll(ProjectScopeClaim)
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Split(':', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2 &&
                (string.Equals(parts[0], "*", StringComparison.Ordinal) || string.Equals(parts[0], projectId, StringComparison.Ordinal)))
            .Select(static parts => parts[1])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
