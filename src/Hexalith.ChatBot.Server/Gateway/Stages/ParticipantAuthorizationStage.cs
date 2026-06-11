using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Governance.Admin;
using Hexalith.ChatBot.Server.Governance.Outbound;
using Hexalith.ChatBot.Server.Notifications;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ParticipantAuthorizationStage(
    IAssociationCorrectionDependencyReadiness? correctionDependencyReadiness = null,
    IServiceClientGrantValidator? serviceClientGrantValidator = null,
    ICommandCapabilityControlStateProvider? commandCapabilityControlStateProvider = null,
    ISystemClock? clock = null,
    ICommandCapabilityRateLimitProvider? rateLimitProvider = null,
    ICommandCapabilityCommandHistory? commandHistory = null) : IAuthorizationStage
{
    private readonly IAssociationCorrectionDependencyReadiness _correctionDependencyReadiness =
        correctionDependencyReadiness ?? new StaticAssociationCorrectionDependencyReadiness(AssociationCorrectionDependencyReadinessStatus.Ready);
    private readonly IServiceClientGrantValidator _serviceClientGrantValidator =
        serviceClientGrantValidator ?? new PassThroughServiceClientGrantValidator();
    private readonly ICommandCapabilityControlStateProvider _commandCapabilityControlStateProvider =
        commandCapabilityControlStateProvider ?? new AlwaysActiveCommandCapabilityControlStateProvider();

    // Story 7.23: the command-capability rate-limit is enforced at THIS actor-agnostic seam (not the
    // service/AI-only ServiceClientGrantValidator), because the subject is a command TYPE submitted by any actor.
    // This stage had no clock before; inject one (DI provides ISystemClock) plus the two dedicated read-side seams.
    // All three default to no-op so existing call sites/tests keep compiling and behave identically until a tenant
    // configures a limit.
    private readonly ISystemClock _clock = clock ?? new SystemClock();
    private readonly ICommandCapabilityRateLimitProvider _rateLimitProvider =
        rateLimitProvider ?? new AlwaysUnlimitedCommandCapabilityRateLimitProvider();
    private readonly ICommandCapabilityCommandHistory _commandHistory =
        commandHistory ?? new EmptyCommandCapabilityCommandHistory();

    // FR74 two-person governance/control commands (Submit*/Approve* across mailbox-source / service-client /
    // AI-actor / command-capability subject classes). These are the commands needed to govern and reverse a
    // disable, so (a) the AC2 self-lockout guard rejects a SubmitCommandCapabilityDisable whose CommandCapabilityRef
    // names one of these, and (b) the AC4 actor-agnostic enforcement seam exempts them from the disabled-capability
    // block — defense-in-depth so a disabled capability can never lock the tenant out of governance/reversal.
    private static readonly HashSet<string> Fr74GovernanceCommandTypes =
        new(StringComparer.Ordinal)
        {
            nameof(SubmitMailboxSourceDisable),
            nameof(ApproveMailboxSourceDisable),
            nameof(SubmitMailboxSourceQuarantine),
            nameof(ApproveMailboxSourceQuarantine),
            nameof(SubmitServiceClientDisable),
            nameof(ApproveServiceClientDisable),
            nameof(SubmitServiceClientQuarantine),
            nameof(ApproveServiceClientQuarantine),
            nameof(SubmitAiActorDisable),
            nameof(ApproveAiActorDisable),
            nameof(SubmitAiActorQuarantine),
            nameof(ApproveAiActorQuarantine),
            nameof(SubmitCommandCapabilityDisable),
            nameof(ApproveCommandCapabilityDisable),
            nameof(SubmitCommandCapabilityQuarantine),
            nameof(ApproveCommandCapabilityQuarantine),
            // Story 7.23: the single-actor rate-limit command is itself a governance command — so (a) it cannot be
            // rate-limited (self-lockout guard in IsValidCommandCapabilityRateLimit) and (b) the final-gate
            // rate-limit enforcement exempts it (a rate-limited tenant can still govern/reverse).
            nameof(SubmitCommandCapabilityRateLimit),
        };
    public const string ParticipantAuthorityClaim = "chatbot:participant-authority";
    public const string UnresolvedValue = "unresolved";
    public const string EmailOnlyValue = "email-only";
    public const string UnauthorizedValue = "unauthorized";
    public const string DirectoryDegradedValue = "directory-degraded";
    public const string ActorTypeClaim = "chatbot:actor-type";
    public const string TenantRoleClaim = "chatbot:tenant-role";
    public const string ProjectOwnerClaim = "chatbot:project-owner";
    public const string HumanActorValue = "human";
    public const string ServiceActorValue = "service";
    public const string AiActorValue = "ai";
    public const string TenantAdminValue = AdminRoles.TenantAdmin;

    public async ValueTask<ChatBotAuthorizationResult> AuthorizeAsync(
        ChatBotCommandSubmission submission,
        ChatBotAuthenticatedActor actor,
        ChatBotTenantBinding tenantBinding,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(tenantBinding);
        cancellationToken.ThrowIfCancellationRequested();

        // Story 7.21/7.22 (FR74): a disabled OR quarantined command CAPABILITY (command TYPE) fails closed for EVERY
        // actor — human, service, and AI — at this actor-agnostic seam, BEFORE the grant validator (which runs only
        // for service/AI actors via RequiresGrant and would let a human submission of the controlled type slip
        // through), the per-command admin-scope branches, and the downstream spine-allowlist / risk / approval gates —
        // so the submission is denied with the precise `command_capability_disabled` / `command_capability_quarantined`
        // reason and no restricted work runs. The control state is fetched ONCE and switched on (Disabled vs the
        // Story 7.22 Quarantined "contained for review" value). The FR74 governance/two-person control commands are
        // EXEMPT so a controlled capability can still be governed/reversed (defense-in-depth with the AC2 self-lockout
        // guard). Tenant scope is the authenticated binding, never the command body; each tenant's controlled set is
        // independent (isolation). This check reads only the safe command type name + tenant — never
        // credentials/OAuth fingerprints/model prompts — and mutates no committed record.
        if (!string.IsNullOrWhiteSpace(submission.Request.CommandType) &&
            !Fr74GovernanceCommandTypes.Contains(submission.Request.CommandType))
        {
            CommandCapabilityControlState capabilityState = await _commandCapabilityControlStateProvider
                .GetControlStateAsync(tenantBinding.TenantId, submission.Request.CommandType, cancellationToken)
                .ConfigureAwait(false);
            switch (capabilityState)
            {
                case CommandCapabilityControlState.Disabled:
                    return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.CommandCapabilityDisabled);
                case CommandCapabilityControlState.Quarantined:
                    return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.CommandCapabilityQuarantined);
                default:
                    break;
            }
        }

        ChatBotAuthorizationResult grantResult = await _serviceClientGrantValidator
            .ValidateAsync(submission, actor, tenantBinding, cancellationToken)
            .ConfigureAwait(false);
        if (!grantResult.IsAllowed)
        {
            return grantResult;
        }

        string[] authorities = actor.Principal
            .FindAll(ParticipantAuthorityClaim)
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        if (authorities.Contains(DirectoryDegradedValue, StringComparer.Ordinal))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ParticipantDirectoryDegraded);
        }

        if (authorities.Contains(UnresolvedValue, StringComparer.Ordinal))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.UnresolvedParticipant);
        }

        if (authorities.Contains(EmailOnlyValue, StringComparer.Ordinal) ||
            authorities.Contains(UnauthorizedValue, StringComparer.Ordinal))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.UnauthorizedParticipant);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SetAssociationConfidenceThresholds), StringComparison.Ordinal) &&
            !AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitTenantPolicyChange), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidTenantPolicyChange(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveTenantPolicyChange), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidTenantPolicyApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitMailboxConfigurationChange), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Mailbox) ||
                !IsValidMailboxConfigurationChange(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitMailboxSourceDisable), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Mailbox) ||
                !IsValidMailboxSourceDisable(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveMailboxSourceDisable), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Mailbox) ||
                !IsValidMailboxSourceDisableApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitServiceClientDisable), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanTenantAdmin(actor.Principal) ||
                !IsValidServiceClientDisable(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveServiceClientDisable), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanTenantAdmin(actor.Principal) ||
                !IsValidServiceClientDisableApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // FR74 AI-actor disable is gated on the policy-admin scope (not tenant-admin): AI-action governance is the
        // policy-admin's domain (Story 7.2). A tenant-admin still passes via the FR75a scope union. Service/AI
        // actors are denied by HasHumanAdminScope's human-actor gate.
        if (string.Equals(submission.Request.CommandType, nameof(SubmitAiActorDisable), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidAiActorDisable(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveAiActorDisable), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidAiActorDisableApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // Story 7.21 FR74 command-capability disable is gated on the policy-admin scope (the "security engineer"
        // persona maps to AdminScope.Policy — there is no AdminScope.Security). A tenant-admin still passes via the
        // FR75a scope union. Service/AI actors are denied by HasHumanAdminScope's human-actor gate. The validators
        // enforce safe tokens, the Active->Disabled state shape, the distinct-approver rule on the approval, and the
        // self-lockout guard (reject a ref naming an FR74 governance command).
        if (string.Equals(submission.Request.CommandType, nameof(SubmitCommandCapabilityDisable), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidCommandCapabilityDisable(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveCommandCapabilityDisable), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidCommandCapabilityDisableApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // Story 7.24 FR74 outbound-channel disable is gated on the policy-admin scope identically to the
        // command-capability disable pair above (the "policy administrator" persona maps to AdminScope.Policy — there
        // is no AdminScope.Security). A tenant-admin still passes via the FR75a scope union. Service/AI actors are
        // denied by HasHumanAdminScope's human-actor gate. The validators enforce safe tokens (the channel ref is a
        // SafeStableIdentifier), the Active->Disabled state shape, and the distinct-approver rule on the approval.
        // Divergence from 7.21: there is NO self-lockout guard / Fr74GovernanceCommandTypes membership check — the
        // subject is an outbound channel, not a governance command type, so disabling it cannot lock out governance.
        if (string.Equals(submission.Request.CommandType, nameof(SubmitOutboundChannelDisable), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidOutboundChannelDisable(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveOutboundChannelDisable), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidOutboundChannelDisableApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // Story 7.25 FR74 outbound-channel quarantine is gated identically to the 7.24 disable pair above — the same
        // policy-admin scope, the same distinct-approver rule, and the same no-self-lockout divergence (the subject is
        // an outbound channel, not a governance command type, so quarantining it cannot lock out governance; do NOT add
        // these command names to Fr74GovernanceCommandTypes). This applies the Story 7.22 disable→quarantine
        // substitution to the outbound-channel row. The validators enforce safe tokens (the channel ref is a
        // SafeStableIdentifier) and the Active->Quarantined state shape.
        if (string.Equals(submission.Request.CommandType, nameof(SubmitOutboundChannelQuarantine), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidOutboundChannelQuarantine(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveOutboundChannelQuarantine), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidOutboundChannelQuarantineApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // Story 7.26: outbound-channel rate-limit is a SINGLE-ACTOR standard policy mutation (mirror the
        // SubmitCommandCapabilityRateLimit gating shape and the 7.24/7.25 outbound-channel subject). Outbound-channel
        // governance is a security-sensitive policy concern, so gate on HasHumanAdminScope(AdminScope.Policy) — the same
        // scope as the disable/quarantine pairs (the "policy administrator" persona maps to AdminScope.Policy; there is
        // no AdminScope.Security). A tenant-admin still passes via the FR75a scope union. Service/AI actors are denied by
        // HasHumanAdminScope's human-actor gate. There is NO approver/distinct-approver guard (single actor). Unlike the
        // 7.23 command-capability rate-limit, there is NO self-lockout guard and the command is NOT added to
        // Fr74GovernanceCommandTypes — the subject is an outbound channel, not a governance command type, so rate-limiting
        // it cannot lock out governance (the 7.24/7.25 outbound divergence). The enforcement of the configured budget is
        // the outbound send seam in AcceptedCommandDispatcher (NOT a final gate here), because the channel ref only meets
        // the authenticated tenant binding at the send seam. The validator enforces safe tokens, the bounds, and a known
        // schema version.
        if (string.Equals(submission.Request.CommandType, nameof(SubmitOutboundChannelRateLimit), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidOutboundChannelRateLimit(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // Story 7.22 FR74 command-capability quarantine is gated on the policy-admin scope identically to the disable
        // pair above (the "security engineer" persona maps to AdminScope.Policy — there is no AdminScope.Security). A
        // tenant-admin still passes via the FR75a scope union. Service/AI actors are denied by HasHumanAdminScope's
        // human-actor gate. The validators enforce safe tokens, the Active->Quarantined state shape, the
        // distinct-approver rule on the approval, and the self-lockout guard (reject a ref naming an FR74 governance
        // command — including the two quarantine commands themselves).
        if (string.Equals(submission.Request.CommandType, nameof(SubmitCommandCapabilityQuarantine), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidCommandCapabilityQuarantine(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveCommandCapabilityQuarantine), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidCommandCapabilityQuarantineApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // FR74 AI-actor quarantine is gated on the policy-admin scope (not tenant-admin), exactly like the disable
        // pair above and unlike the 7.16 service-client quarantine: AI-action governance is the policy-admin's domain
        // (Story 7.2). A tenant-admin still passes via the FR75a scope union. Service/AI actors are denied by
        // HasHumanAdminScope's human-actor gate.
        if (string.Equals(submission.Request.CommandType, nameof(SubmitAiActorQuarantine), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidAiActorQuarantine(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveAiActorQuarantine), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidAiActorQuarantineApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitServiceClientQuarantine), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanTenantAdmin(actor.Principal) ||
                !IsValidServiceClientQuarantine(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveServiceClientQuarantine), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanTenantAdmin(actor.Principal) ||
                !IsValidServiceClientQuarantineApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // Story 7.17: rate-limit is a single-actor standard policy mutation (mirror SubmitMailboxSourceRateLimit) —
        // but service-client governance is a TenantAdmin responsibility, so gate on HasHumanTenantAdmin (no service-client
        // AdminScope, no mailbox scope) with no approver/distinct-approver guard. Service/AI actors are denied via the
        // human-actor gate inside HasHumanTenantAdmin.
        if (string.Equals(submission.Request.CommandType, nameof(SubmitServiceClientRateLimit), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanTenantAdmin(actor.Principal) ||
                !IsValidServiceClientRateLimit(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // Story 7.20: AI-actor rate-limit is a single-actor standard policy mutation (mirror SubmitServiceClientRateLimit) —
        // but AI-action governance is the policy-admin's domain (Story 7.2), so gate on HasHumanAdminScope(AdminScope.Policy)
        // (the AI-actor disable/quarantine divergence), NOT HasHumanTenantAdmin. A tenant-admin still passes via the FR75a
        // scope union. No approver/distinct-approver guard. Service/AI actors are denied via the human-actor gate inside
        // HasHumanAdminScope.
        if (string.Equals(submission.Request.CommandType, nameof(SubmitAiActorRateLimit), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidAiActorRateLimit(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // Story 7.23: command-capability rate-limit is a single-actor standard policy mutation (mirror
        // SubmitAiActorRateLimit) — command-capability/command-allowlist governance is a security-sensitive policy
        // concern, so gate on HasHumanAdminScope(AdminScope.Policy) (the same scope as the 7.21/7.22 disable/quarantine
        // pairs). A tenant-admin still passes via the FR75a scope union. No approver/distinct-approver guard. Service/AI
        // actors are denied via the human-actor gate inside HasHumanAdminScope. The validator enforces safe tokens,
        // bounds, a known schema version, AND the self-lockout guard (reject a CommandCapabilityRef naming an FR74
        // governance command, including the rate-limit command itself).
        if (string.Equals(submission.Request.CommandType, nameof(SubmitCommandCapabilityRateLimit), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidCommandCapabilityRateLimit(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitMailboxSourceQuarantine), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Mailbox) ||
                !IsValidMailboxSourceQuarantine(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ApproveMailboxSourceQuarantine), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Mailbox) ||
                !IsValidMailboxSourceQuarantineApproval(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // Story 7.14: rate-limit is a single-actor standard policy mutation (mirror SubmitMailboxConfigurationChange) —
        // one human mailbox-admin scope check, no approver/distinct-approver guard.
        if (string.Equals(submission.Request.CommandType, nameof(SubmitMailboxSourceRateLimit), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Mailbox) ||
                !IsValidMailboxSourceRateLimit(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitNotificationRoutingChange), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidNotificationRoutingChange(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.NotificationRoutingUnauthorized);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitEscalationPolicyChange), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Policy) ||
                !IsValidEscalationPolicyChange(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.EscalationPolicyUnauthorized);
        }

        if (string.Equals(submission.Request.CommandType, nameof(RecordMailboxProviderConnection), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Mailbox) ||
                !IsValidMailboxProviderConnection(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(RequestComplianceInvestigation), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Compliance) ||
                !IsValidComplianceInvestigation(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(RequestComplianceEscalation), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Compliance) ||
                !IsValidComplianceEscalation(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitRetentionConfigurationChange), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Compliance) ||
                !IsValidRetentionConfigurationChange(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitDataClassInventoryChange), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Compliance) ||
                !IsValidDataClassInventoryChange(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitTenantExportRequest), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Compliance) ||
                !IsValidTenantExportRequest(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitDeletionErasureRequest), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Compliance) ||
                !IsValidDeletionErasureRequest(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(SubmitConsentLawfulBasisRecord), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Compliance) ||
                !IsValidConsentLawfulBasisRecord(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(AssignTenantAdminRole), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanTenantAdmin(actor.Principal) ||
                !IsValidAdminAssignment(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }

        if (string.Equals(submission.Request.CommandType, nameof(ExecuteAdminQueueOperation), StringComparison.Ordinal) &&
            (!AdminAuthorityEvaluator.HasHumanAdminScope(actor.Principal, AdminScope.Operate) ||
                !IsValidAdminQueueOperation(submission.Request.Command)))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(CorrectEmailProjectAssociation), StringComparison.Ordinal) &&
            CorrectionDependencyUnavailableReason(_correctionDependencyReadiness.Status) is { } correctionDependencyReason)
        {
            return ChatBotAuthorizationResult.Denied(correctionDependencyReason);
        }

        if (string.Equals(submission.Request.CommandType, nameof(CorrectEmailProjectAssociation), StringComparison.Ordinal) &&
            !CanCorrectAssociation(actor.Principal, submission.Request.Command))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AssociationCorrectionTargetUnauthorized);
        }

        if ((string.Equals(submission.Request.CommandType, nameof(ExecuteLowRiskAIAssistance), StringComparison.Ordinal) ||
                string.Equals(submission.Request.CommandType, nameof(ExecuteApprovedAIAction), StringComparison.Ordinal) ||
                string.Equals(submission.Request.CommandType, nameof(RequestOutboundSendApproval), StringComparison.Ordinal) ||
                string.Equals(submission.Request.CommandType, nameof(DecideOutboundApproval), StringComparison.Ordinal) ||
                string.Equals(submission.Request.CommandType, nameof(ExecuteApprovedOutboundDraft), StringComparison.Ordinal)) &&
            !CanReadProject(actor.Principal, submission.Request.Command))
        {
            return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (string.Equals(submission.Request.CommandType, nameof(CreateOutboundDraft), StringComparison.Ordinal))
        {
            CreateOutboundDraft? command = ReadCreateOutboundDraft(submission.Request.Command);
            if (command is null)
            {
                return ChatBotAuthorizationResult.Denied(ChatBotDisabledActionReasons.InsufficientAuthority);
            }

            if (!HasTrustedOutboundDraftOrigin(command, actor, grantResult.ServiceClientGrantEvidence))
            {
                return ChatBotAuthorizationResult.Denied(ChatBotDisabledActionReasons.InsufficientAuthority);
            }

            var classification = OutboundDraftAuthorityEvaluator.Classify(command, actor.Principal, tenantBinding.TenantId);
            if (classification.DenialReason is not null ||
                command.SenderAuthorityClass is not SenderAuthorityClass.DraftOnly)
            {
                return ChatBotAuthorizationResult.Denied(OutboundDraftAuthorityEvaluator.SafeDenialReason(command, actor.Principal, classification));
            }
        }

        if (string.Equals(submission.Request.CommandType, nameof(ExecuteApprovedOutboundDraft), StringComparison.Ordinal))
        {
            ExecuteApprovedOutboundDraft? command = ReadExecuteApprovedOutboundDraft(submission.Request.Command);
            if (command is null)
            {
                return ChatBotAuthorizationResult.Denied(ChatBotDisabledActionReasons.InsufficientAuthority);
            }

            if (!string.Equals(command.SendActorId, actor.ActorId, StringComparison.Ordinal))
            {
                return ChatBotAuthorizationResult.Denied(ChatBotDisabledActionReasons.InsufficientAuthority);
            }

            var classification = OutboundSendAuthorityEvaluator.Classify(command, actor.Principal, tenantBinding.TenantId, grantResult.ServiceClientGrantEvidence);
            if (classification.DenialReason is not null ||
                classification.AuthorityClass != command.SenderAuthorityClass)
            {
                return ChatBotAuthorizationResult.Denied(OutboundSendAuthorityEvaluator.SafeDenialReason(command, actor.Principal, classification));
            }
        }

        // Story 7.23: command-capability rate-limit is the FINAL admission gate — placed after EVERY prior check
        // (the top-of-stage Disabled/Quarantined control-state switch, the grant validator, and the
        // participant-authority + per-command admin-scope branches) so only otherwise-fully-admissible commands count
        // against the budget and a disabled/quarantined/under-scoped/unauthorized command keeps its precise reason
        // (rate-limit never masks a security denial). It is the 7.20 "final gate" doctrine relocated to this
        // actor-agnostic stage so a HUMAN submission of the rate-limited command TYPE is throttled too. The FR74
        // governance/two-person commands are EXEMPT so a rate-limited tenant can still govern/reverse. Each
        // (tenant × command-type) budget + trailing-window counter is independent (NFR30 isolation); the count is
        // server-measured UTC age against the injected clock. Reads only the safe command type name + tenant — never
        // credentials/OAuth fingerprints/model prompts — and mutates no committed record. Out-of-bounds configured
        // budgets fall back to the safe default at the seam (EffectiveBudget), never raising the cap.
        if (!string.IsNullOrWhiteSpace(submission.Request.CommandType) &&
            !Fr74GovernanceCommandTypes.Contains(submission.Request.CommandType))
        {
            CommandCapabilityRateLimitState? rateLimit = await _rateLimitProvider
                .GetRateLimitAsync(tenantBinding.TenantId, submission.Request.CommandType, cancellationToken)
                .ConfigureAwait(false);
            if (rateLimit is not null)
            {
                IReadOnlyList<DateTimeOffset> recentAdmitted = await _commandHistory
                    .GetRecentAdmittedAsync(tenantBinding.TenantId, submission.Request.CommandType, cancellationToken)
                    .ConfigureAwait(false);
                int windowCount = NotificationThrottleEvaluator.CountInTrailingWindow(
                    recentAdmitted, _clock.UtcNow, rateLimit.WindowDuration);
                if (windowCount >= rateLimit.EffectiveBudget)
                {
                    return ChatBotAuthorizationResult.Denied(ChatBotAuthorizationReasonCodes.CommandCapabilityRateLimited);
                }
            }
        }

        return ChatBotAuthorizationResult.Allowed(grantResult.ServiceClientGrantEvidence);
    }

    private static string? CorrectionDependencyUnavailableReason(AssociationCorrectionDependencyReadinessStatus status)
    {
        if (!status.IsWorkflowRuntimeReady)
        {
            return ChatBotAuthorizationReasonCodes.AssociationCorrectionWorkflowUnavailable;
        }

        if (!status.IsProjectionInvalidationReady)
        {
            return ChatBotAuthorizationReasonCodes.AssociationCorrectionProjectionUnavailable;
        }

        if (!status.IsAuditWriterReady)
        {
            return ChatBotMessageCodes.AssociationCorrectionAuditUnavailable;
        }

        if (!status.IsIdempotencyStoreReady)
        {
            return ChatBotMessageCodes.DependencyDegraded;
        }

        return null;
    }

    private static CreateOutboundDraft? ReadCreateOutboundDraft(object? command)
    {
        if (command is CreateOutboundDraft typed)
        {
            return typed;
        }

        JsonElement element = command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return element.ValueKind == JsonValueKind.Object
            ? element.Deserialize<CreateOutboundDraft>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            : null;
    }

    private static ExecuteApprovedOutboundDraft? ReadExecuteApprovedOutboundDraft(object? command)
    {
        if (command is ExecuteApprovedOutboundDraft typed)
        {
            return typed;
        }

        JsonElement element = command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return element.ValueKind == JsonValueKind.Object
            ? element.Deserialize<ExecuteApprovedOutboundDraft>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            : null;
    }

    private static bool IsValidAdminAssignment(object? command)
    {
        AssignTenantAdminRole? assignment = ReadAssignTenantAdminRole(command);
        return assignment is not null &&
            AdminRoles.All.Contains(assignment.Role) &&
            IsSafeAdminToken(assignment.AssignmentId) &&
            IsSafeAdminToken(assignment.TargetActorId) &&
            IsSafeAdminToken(assignment.ReasonCode) &&
            IsSafeAdminToken(assignment.PolicySnapshotId) &&
            assignment.SourceVersion >= 0;
    }

    private static bool IsValidTenantPolicyChange(object? command)
    {
        SubmitTenantPolicyChange? change = ReadSubmitTenantPolicyChange(command);
        return change is not null &&
            change.SourceVersion >= 0 &&
            IsSafeAdminToken(change.PolicyChangeId) &&
            IsSafeAdminToken(change.SourcePolicySnapshotId) &&
            IsSafeAdminToken(change.ProposedPolicySnapshotId) &&
            IsSafeAdminToken(change.ReasonCode) &&
            IsSafeAdminToken(change.RequesterRef) &&
            TenantPolicySchemaVersions.IsKnown(change.SchemaVersion) &&
            IsSafeAdminToken(change.CorrelationId) &&
            IsSafeAdminToken(change.OldValueFingerprint) &&
            IsSafeAdminToken(change.NewValueFingerprint) &&
            IsValidChangedKnobs(change.ChangedKnobIds, change.ChangeSet) &&
            TenantPolicySchema.Validate(change.ChangeSet).IsValid;
    }

    private static bool IsValidTenantPolicyApproval(object? command)
    {
        ApproveTenantPolicyChange? approval = ReadApproveTenantPolicyChange(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.PolicyChangeId) &&
            IsSafeAdminToken(approval.PendingPolicySnapshotId) &&
            IsSafeAdminToken(approval.ActivatedPolicySnapshotId) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            TenantPolicySchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.ChangedKnobIds is { Count: > 0 } &&
            approval.ChangedKnobIds.All(static knob => TenantPolicySchema.TryGetDefinition(knob, out _));
    }

    private static bool IsValidMailboxConfigurationChange(object? command)
    {
        SubmitMailboxConfigurationChange? change = ReadSubmitMailboxConfigurationChange(command);
        return change is not null &&
            change.SourceVersion >= 0 &&
            IsSafeAdminToken(change.ConfigurationChangeId) &&
            IsSafeAdminToken(change.SourceConfigurationSnapshotId) &&
            IsSafeAdminToken(change.ProposedConfigurationSnapshotId) &&
            IsSafeAdminToken(change.ReasonCode) &&
            IsSafeAdminToken(change.RequesterRef) &&
            MailboxConfigurationSchemaVersions.IsKnown(change.SchemaVersion) &&
            IsSafeAdminToken(change.CorrelationId) &&
            MailboxConfigurationSchema.IsSafeFingerprint(change.OldConfigurationFingerprint) &&
            MailboxConfigurationSchema.IsSafeFingerprint(change.NewConfigurationFingerprint) &&
            MailboxConfigurationSchema.Validate(change.ChangeSet).IsValid;
    }

    private static bool IsValidMailboxSourceDisable(object? command)
    {
        SubmitMailboxSourceDisable? disable = ReadSubmitMailboxSourceDisable(command);
        return disable is not null &&
            disable.SourceVersion >= 0 &&
            IsSafeAdminToken(disable.DisableChangeId) &&
            IsSafeAdminToken(disable.MailboxSourceRef) &&
            IsSafeAdminToken(disable.ReasonCode) &&
            IsSafeAdminToken(disable.PolicySnapshotId) &&
            IsSafeAdminToken(disable.RequesterRef) &&
            disable.OldState == MailboxSourceControlState.Active &&
            disable.NewState == MailboxSourceControlState.Disabled &&
            MailboxSourceControlSchemaVersions.IsKnown(disable.SchemaVersion) &&
            IsSafeAdminToken(disable.CorrelationId);
    }

    private static bool IsValidMailboxSourceDisableApproval(object? command)
    {
        ApproveMailboxSourceDisable? approval = ReadApproveMailboxSourceDisable(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.DisableChangeId) &&
            IsSafeAdminToken(approval.MailboxSourceRef) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.PolicySnapshotId) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.OldState == MailboxSourceControlState.Active &&
            approval.NewState == MailboxSourceControlState.Disabled &&
            MailboxSourceControlSchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId);
    }

    private static bool IsValidServiceClientDisable(object? command)
    {
        SubmitServiceClientDisable? disable = ReadSubmitServiceClientDisable(command);
        return disable is not null &&
            disable.SourceVersion >= 0 &&
            IsSafeAdminToken(disable.DisableChangeId) &&
            IsSafeAdminToken(disable.ServiceClientRef) &&
            IsSafeAdminToken(disable.ReasonCode) &&
            IsSafeAdminToken(disable.PolicySnapshotId) &&
            IsSafeAdminToken(disable.RequesterRef) &&
            disable.OldState == ServiceClientControlState.Active &&
            disable.NewState == ServiceClientControlState.Disabled &&
            ServiceClientControlSchemaVersions.IsKnown(disable.SchemaVersion) &&
            IsSafeAdminToken(disable.CorrelationId);
    }

    private static bool IsValidServiceClientDisableApproval(object? command)
    {
        ApproveServiceClientDisable? approval = ReadApproveServiceClientDisable(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.DisableChangeId) &&
            IsSafeAdminToken(approval.ServiceClientRef) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.PolicySnapshotId) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.OldState == ServiceClientControlState.Active &&
            approval.NewState == ServiceClientControlState.Disabled &&
            ServiceClientControlSchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId);
    }

    private static bool IsValidAiActorDisable(object? command)
    {
        SubmitAiActorDisable? disable = ReadSubmitAiActorDisable(command);
        return disable is not null &&
            disable.SourceVersion >= 0 &&
            IsSafeAdminToken(disable.DisableChangeId) &&
            IsSafeAdminToken(disable.AiActorRef) &&
            IsSafeAdminToken(disable.ReasonCode) &&
            IsSafeAdminToken(disable.PolicySnapshotId) &&
            IsSafeAdminToken(disable.RequesterRef) &&
            disable.OldState == AiActorControlState.Active &&
            disable.NewState == AiActorControlState.Disabled &&
            AiActorControlSchemaVersions.IsKnown(disable.SchemaVersion) &&
            IsSafeAdminToken(disable.CorrelationId);
    }

    private static bool IsValidAiActorDisableApproval(object? command)
    {
        ApproveAiActorDisable? approval = ReadApproveAiActorDisable(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.DisableChangeId) &&
            IsSafeAdminToken(approval.AiActorRef) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.PolicySnapshotId) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.OldState == AiActorControlState.Active &&
            approval.NewState == AiActorControlState.Disabled &&
            AiActorControlSchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId);
    }

    private static bool IsValidCommandCapabilityDisable(object? command)
    {
        SubmitCommandCapabilityDisable? disable = ReadSubmitCommandCapabilityDisable(command);
        return disable is not null &&
            disable.SourceVersion >= 0 &&
            IsSafeAdminToken(disable.DisableChangeId) &&
            IsSafeAdminToken(disable.CommandCapabilityRef) &&
            !Fr74GovernanceCommandTypes.Contains(disable.CommandCapabilityRef) &&
            IsSafeAdminToken(disable.ReasonCode) &&
            IsSafeAdminToken(disable.PolicySnapshotId) &&
            IsSafeAdminToken(disable.RequesterRef) &&
            disable.OldState == CommandCapabilityControlState.Active &&
            disable.NewState == CommandCapabilityControlState.Disabled &&
            CommandCapabilityControlSchemaVersions.IsKnown(disable.SchemaVersion) &&
            IsSafeAdminToken(disable.CorrelationId);
    }

    private static bool IsValidCommandCapabilityDisableApproval(object? command)
    {
        ApproveCommandCapabilityDisable? approval = ReadApproveCommandCapabilityDisable(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.DisableChangeId) &&
            IsSafeAdminToken(approval.CommandCapabilityRef) &&
            !Fr74GovernanceCommandTypes.Contains(approval.CommandCapabilityRef) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.PolicySnapshotId) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.OldState == CommandCapabilityControlState.Active &&
            approval.NewState == CommandCapabilityControlState.Disabled &&
            CommandCapabilityControlSchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId);
    }

    private static SubmitCommandCapabilityDisable? ReadSubmitCommandCapabilityDisable(object? command)
    {
        if (command is SubmitCommandCapabilityDisable typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitCommandCapabilityDisable>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveCommandCapabilityDisable? ReadApproveCommandCapabilityDisable(object? command)
    {
        if (command is ApproveCommandCapabilityDisable typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveCommandCapabilityDisable>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidOutboundChannelDisable(object? command)
    {
        SubmitOutboundChannelDisable? disable = ReadSubmitOutboundChannelDisable(command);
        return disable is not null &&
            disable.SourceVersion >= 0 &&
            IsSafeAdminToken(disable.DisableChangeId) &&
            AuditMetadata.IsSafeStableIdentifier(disable.OutboundChannelRef) &&
            IsSafeAdminToken(disable.ReasonCode) &&
            IsSafeAdminToken(disable.PolicySnapshotId) &&
            IsSafeAdminToken(disable.RequesterRef) &&
            disable.OldState == OutboundChannelControlState.Active &&
            disable.NewState == OutboundChannelControlState.Disabled &&
            OutboundChannelControlSchemaVersions.IsKnown(disable.SchemaVersion) &&
            IsSafeAdminToken(disable.CorrelationId);
    }

    private static bool IsValidOutboundChannelDisableApproval(object? command)
    {
        ApproveOutboundChannelDisable? approval = ReadApproveOutboundChannelDisable(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.DisableChangeId) &&
            AuditMetadata.IsSafeStableIdentifier(approval.OutboundChannelRef) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.PolicySnapshotId) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.OldState == OutboundChannelControlState.Active &&
            approval.NewState == OutboundChannelControlState.Disabled &&
            OutboundChannelControlSchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId);
    }

    private static SubmitOutboundChannelDisable? ReadSubmitOutboundChannelDisable(object? command)
    {
        if (command is SubmitOutboundChannelDisable typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitOutboundChannelDisable>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveOutboundChannelDisable? ReadApproveOutboundChannelDisable(object? command)
    {
        if (command is ApproveOutboundChannelDisable typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveOutboundChannelDisable>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidOutboundChannelQuarantine(object? command)
    {
        SubmitOutboundChannelQuarantine? quarantine = ReadSubmitOutboundChannelQuarantine(command);
        return quarantine is not null &&
            quarantine.SourceVersion >= 0 &&
            IsSafeAdminToken(quarantine.QuarantineChangeId) &&
            AuditMetadata.IsSafeStableIdentifier(quarantine.OutboundChannelRef) &&
            IsSafeAdminToken(quarantine.ReasonCode) &&
            IsSafeAdminToken(quarantine.PolicySnapshotId) &&
            IsSafeAdminToken(quarantine.RequesterRef) &&
            quarantine.OldState == OutboundChannelControlState.Active &&
            quarantine.NewState == OutboundChannelControlState.Quarantined &&
            OutboundChannelControlSchemaVersions.IsKnown(quarantine.SchemaVersion) &&
            IsSafeAdminToken(quarantine.CorrelationId);
    }

    private static bool IsValidOutboundChannelRateLimit(object? command)
    {
        SubmitOutboundChannelRateLimit? rateLimit = ReadSubmitOutboundChannelRateLimit(command);
        return rateLimit is not null &&
            rateLimit.SourceVersion >= 0 &&
            IsSafeAdminToken(rateLimit.RateLimitChangeId) &&
            AuditMetadata.IsSafeStableIdentifier(rateLimit.OutboundChannelRef) &&
            // NO self-lockout guard (the divergence from the 7.23 command-capability rate-limit): the subject is an
            // outbound channel, not a governance command type, so it is never checked against Fr74GovernanceCommandTypes.
            IsSafeAdminToken(rateLimit.ReasonCode) &&
            IsSafeAdminToken(rateLimit.PolicySnapshotId) &&
            IsSafeAdminToken(rateLimit.RequesterRef) &&
            rateLimit.OldBudget >= OutboundChannelRateLimitBounds.Minimum &&
            new OutboundChannelRateLimitBounds(rateLimit.NewBudget).IsWithinBounds &&
            Enum.IsDefined(rateLimit.Window) &&
            OutboundChannelRateLimitSchemaVersions.IsKnown(rateLimit.SchemaVersion) &&
            IsSafeAdminToken(rateLimit.CorrelationId);
    }

    private static SubmitOutboundChannelRateLimit? ReadSubmitOutboundChannelRateLimit(object? command)
    {
        if (command is SubmitOutboundChannelRateLimit typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitOutboundChannelRateLimit>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidOutboundChannelQuarantineApproval(object? command)
    {
        ApproveOutboundChannelQuarantine? approval = ReadApproveOutboundChannelQuarantine(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.QuarantineChangeId) &&
            AuditMetadata.IsSafeStableIdentifier(approval.OutboundChannelRef) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.PolicySnapshotId) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.OldState == OutboundChannelControlState.Active &&
            approval.NewState == OutboundChannelControlState.Quarantined &&
            OutboundChannelControlSchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId);
    }

    private static SubmitOutboundChannelQuarantine? ReadSubmitOutboundChannelQuarantine(object? command)
    {
        if (command is SubmitOutboundChannelQuarantine typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitOutboundChannelQuarantine>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveOutboundChannelQuarantine? ReadApproveOutboundChannelQuarantine(object? command)
    {
        if (command is ApproveOutboundChannelQuarantine typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveOutboundChannelQuarantine>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidCommandCapabilityQuarantine(object? command)
    {
        SubmitCommandCapabilityQuarantine? quarantine = ReadSubmitCommandCapabilityQuarantine(command);
        return quarantine is not null &&
            quarantine.SourceVersion >= 0 &&
            IsSafeAdminToken(quarantine.QuarantineChangeId) &&
            IsSafeAdminToken(quarantine.CommandCapabilityRef) &&
            !Fr74GovernanceCommandTypes.Contains(quarantine.CommandCapabilityRef) &&
            IsSafeAdminToken(quarantine.ReasonCode) &&
            IsSafeAdminToken(quarantine.PolicySnapshotId) &&
            IsSafeAdminToken(quarantine.RequesterRef) &&
            quarantine.OldState == CommandCapabilityControlState.Active &&
            quarantine.NewState == CommandCapabilityControlState.Quarantined &&
            CommandCapabilityControlSchemaVersions.IsKnown(quarantine.SchemaVersion) &&
            IsSafeAdminToken(quarantine.CorrelationId);
    }

    private static bool IsValidCommandCapabilityQuarantineApproval(object? command)
    {
        ApproveCommandCapabilityQuarantine? approval = ReadApproveCommandCapabilityQuarantine(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.QuarantineChangeId) &&
            IsSafeAdminToken(approval.CommandCapabilityRef) &&
            !Fr74GovernanceCommandTypes.Contains(approval.CommandCapabilityRef) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.PolicySnapshotId) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.OldState == CommandCapabilityControlState.Active &&
            approval.NewState == CommandCapabilityControlState.Quarantined &&
            CommandCapabilityControlSchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId);
    }

    private static SubmitCommandCapabilityQuarantine? ReadSubmitCommandCapabilityQuarantine(object? command)
    {
        if (command is SubmitCommandCapabilityQuarantine typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitCommandCapabilityQuarantine>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveCommandCapabilityQuarantine? ReadApproveCommandCapabilityQuarantine(object? command)
    {
        if (command is ApproveCommandCapabilityQuarantine typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveCommandCapabilityQuarantine>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidAiActorQuarantine(object? command)
    {
        SubmitAiActorQuarantine? quarantine = ReadSubmitAiActorQuarantine(command);
        return quarantine is not null &&
            quarantine.SourceVersion >= 0 &&
            IsSafeAdminToken(quarantine.QuarantineChangeId) &&
            IsSafeAdminToken(quarantine.AiActorRef) &&
            IsSafeAdminToken(quarantine.ReasonCode) &&
            IsSafeAdminToken(quarantine.PolicySnapshotId) &&
            IsSafeAdminToken(quarantine.RequesterRef) &&
            quarantine.OldState == AiActorControlState.Active &&
            quarantine.NewState == AiActorControlState.Quarantined &&
            AiActorControlSchemaVersions.IsKnown(quarantine.SchemaVersion) &&
            IsSafeAdminToken(quarantine.CorrelationId);
    }

    private static bool IsValidAiActorQuarantineApproval(object? command)
    {
        ApproveAiActorQuarantine? approval = ReadApproveAiActorQuarantine(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.QuarantineChangeId) &&
            IsSafeAdminToken(approval.AiActorRef) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.PolicySnapshotId) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.OldState == AiActorControlState.Active &&
            approval.NewState == AiActorControlState.Quarantined &&
            AiActorControlSchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId);
    }

    private static bool IsValidServiceClientQuarantine(object? command)
    {
        SubmitServiceClientQuarantine? quarantine = ReadSubmitServiceClientQuarantine(command);
        return quarantine is not null &&
            quarantine.SourceVersion >= 0 &&
            IsSafeAdminToken(quarantine.QuarantineChangeId) &&
            IsSafeAdminToken(quarantine.ServiceClientRef) &&
            IsSafeAdminToken(quarantine.ReasonCode) &&
            IsSafeAdminToken(quarantine.PolicySnapshotId) &&
            IsSafeAdminToken(quarantine.RequesterRef) &&
            quarantine.OldState == ServiceClientControlState.Active &&
            quarantine.NewState == ServiceClientControlState.Quarantined &&
            ServiceClientControlSchemaVersions.IsKnown(quarantine.SchemaVersion) &&
            IsSafeAdminToken(quarantine.CorrelationId);
    }

    private static bool IsValidServiceClientQuarantineApproval(object? command)
    {
        ApproveServiceClientQuarantine? approval = ReadApproveServiceClientQuarantine(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.QuarantineChangeId) &&
            IsSafeAdminToken(approval.ServiceClientRef) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.PolicySnapshotId) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.OldState == ServiceClientControlState.Active &&
            approval.NewState == ServiceClientControlState.Quarantined &&
            ServiceClientControlSchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId);
    }

    private static bool IsValidMailboxSourceQuarantine(object? command)
    {
        SubmitMailboxSourceQuarantine? quarantine = ReadSubmitMailboxSourceQuarantine(command);
        return quarantine is not null &&
            quarantine.SourceVersion >= 0 &&
            IsSafeAdminToken(quarantine.QuarantineChangeId) &&
            IsSafeAdminToken(quarantine.MailboxSourceRef) &&
            IsSafeAdminToken(quarantine.ReasonCode) &&
            IsSafeAdminToken(quarantine.PolicySnapshotId) &&
            IsSafeAdminToken(quarantine.RequesterRef) &&
            quarantine.OldState == MailboxSourceControlState.Active &&
            quarantine.NewState == MailboxSourceControlState.Quarantined &&
            MailboxSourceControlSchemaVersions.IsKnown(quarantine.SchemaVersion) &&
            IsSafeAdminToken(quarantine.CorrelationId);
    }

    private static bool IsValidMailboxSourceQuarantineApproval(object? command)
    {
        ApproveMailboxSourceQuarantine? approval = ReadApproveMailboxSourceQuarantine(command);
        return approval is not null &&
            approval.SourceVersion >= 0 &&
            IsSafeAdminToken(approval.QuarantineChangeId) &&
            IsSafeAdminToken(approval.MailboxSourceRef) &&
            IsSafeAdminToken(approval.ReasonCode) &&
            IsSafeAdminToken(approval.PolicySnapshotId) &&
            IsSafeAdminToken(approval.RequesterRef) &&
            IsSafeAdminToken(approval.ApproverRef) &&
            !string.Equals(approval.RequesterRef, approval.ApproverRef, StringComparison.Ordinal) &&
            approval.OldState == MailboxSourceControlState.Active &&
            approval.NewState == MailboxSourceControlState.Quarantined &&
            MailboxSourceControlSchemaVersions.IsKnown(approval.SchemaVersion) &&
            IsSafeAdminToken(approval.CorrelationId);
    }

    private static bool IsValidMailboxSourceRateLimit(object? command)
    {
        SubmitMailboxSourceRateLimit? rateLimit = ReadSubmitMailboxSourceRateLimit(command);
        return rateLimit is not null &&
            rateLimit.SourceVersion >= 0 &&
            IsSafeAdminToken(rateLimit.RateLimitChangeId) &&
            IsSafeAdminToken(rateLimit.MailboxSourceRef) &&
            IsSafeAdminToken(rateLimit.ReasonCode) &&
            IsSafeAdminToken(rateLimit.PolicySnapshotId) &&
            IsSafeAdminToken(rateLimit.RequesterRef) &&
            rateLimit.OldBudget >= MailboxRateLimitBounds.Minimum &&
            new MailboxRateLimitBounds(rateLimit.NewBudget).IsWithinBounds &&
            Enum.IsDefined(rateLimit.Window) &&
            MailboxSourceRateLimitSchemaVersions.IsKnown(rateLimit.SchemaVersion) &&
            IsSafeAdminToken(rateLimit.CorrelationId);
    }

    private static SubmitMailboxSourceRateLimit? ReadSubmitMailboxSourceRateLimit(object? command)
    {
        if (command is SubmitMailboxSourceRateLimit typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitMailboxSourceRateLimit>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidServiceClientRateLimit(object? command)
    {
        SubmitServiceClientRateLimit? rateLimit = ReadSubmitServiceClientRateLimit(command);
        return rateLimit is not null &&
            rateLimit.SourceVersion >= 0 &&
            IsSafeAdminToken(rateLimit.RateLimitChangeId) &&
            IsSafeAdminToken(rateLimit.ServiceClientRef) &&
            IsSafeAdminToken(rateLimit.ReasonCode) &&
            IsSafeAdminToken(rateLimit.PolicySnapshotId) &&
            IsSafeAdminToken(rateLimit.RequesterRef) &&
            rateLimit.OldBudget >= ServiceClientRateLimitBounds.Minimum &&
            new ServiceClientRateLimitBounds(rateLimit.NewBudget).IsWithinBounds &&
            Enum.IsDefined(rateLimit.Window) &&
            ServiceClientRateLimitSchemaVersions.IsKnown(rateLimit.SchemaVersion) &&
            IsSafeAdminToken(rateLimit.CorrelationId);
    }

    private static SubmitServiceClientRateLimit? ReadSubmitServiceClientRateLimit(object? command)
    {
        if (command is SubmitServiceClientRateLimit typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitServiceClientRateLimit>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidAiActorRateLimit(object? command)
    {
        SubmitAiActorRateLimit? rateLimit = ReadSubmitAiActorRateLimit(command);
        return rateLimit is not null &&
            rateLimit.SourceVersion >= 0 &&
            IsSafeAdminToken(rateLimit.RateLimitChangeId) &&
            IsSafeAdminToken(rateLimit.AiActorRef) &&
            IsSafeAdminToken(rateLimit.ReasonCode) &&
            IsSafeAdminToken(rateLimit.PolicySnapshotId) &&
            IsSafeAdminToken(rateLimit.RequesterRef) &&
            rateLimit.OldBudget >= AiActorRateLimitBounds.Minimum &&
            new AiActorRateLimitBounds(rateLimit.NewBudget).IsWithinBounds &&
            Enum.IsDefined(rateLimit.Window) &&
            AiActorRateLimitSchemaVersions.IsKnown(rateLimit.SchemaVersion) &&
            IsSafeAdminToken(rateLimit.CorrelationId);
    }

    private static bool IsValidCommandCapabilityRateLimit(object? command)
    {
        SubmitCommandCapabilityRateLimit? rateLimit = ReadSubmitCommandCapabilityRateLimit(command);
        return rateLimit is not null &&
            rateLimit.SourceVersion >= 0 &&
            IsSafeAdminToken(rateLimit.RateLimitChangeId) &&
            IsSafeAdminToken(rateLimit.CommandCapabilityRef) &&
            // Self-lockout guard: a tenant cannot rate-limit an FR74 governance command (including the rate-limit
            // command itself), which would otherwise risk locking the tenant out of governing/reversing controls.
            !Fr74GovernanceCommandTypes.Contains(rateLimit.CommandCapabilityRef) &&
            IsSafeAdminToken(rateLimit.ReasonCode) &&
            IsSafeAdminToken(rateLimit.PolicySnapshotId) &&
            IsSafeAdminToken(rateLimit.RequesterRef) &&
            rateLimit.OldBudget >= CommandCapabilityRateLimitBounds.Minimum &&
            new CommandCapabilityRateLimitBounds(rateLimit.NewBudget).IsWithinBounds &&
            Enum.IsDefined(rateLimit.Window) &&
            CommandCapabilityRateLimitSchemaVersions.IsKnown(rateLimit.SchemaVersion) &&
            IsSafeAdminToken(rateLimit.CorrelationId);
    }

    private static SubmitCommandCapabilityRateLimit? ReadSubmitCommandCapabilityRateLimit(object? command)
    {
        if (command is SubmitCommandCapabilityRateLimit typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitCommandCapabilityRateLimit>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitAiActorRateLimit? ReadSubmitAiActorRateLimit(object? command)
    {
        if (command is SubmitAiActorRateLimit typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitAiActorRateLimit>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitMailboxSourceQuarantine? ReadSubmitMailboxSourceQuarantine(object? command)
    {
        if (command is SubmitMailboxSourceQuarantine typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitMailboxSourceQuarantine>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveMailboxSourceQuarantine? ReadApproveMailboxSourceQuarantine(object? command)
    {
        if (command is ApproveMailboxSourceQuarantine typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveMailboxSourceQuarantine>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitMailboxSourceDisable? ReadSubmitMailboxSourceDisable(object? command)
    {
        if (command is SubmitMailboxSourceDisable typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitMailboxSourceDisable>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveMailboxSourceDisable? ReadApproveMailboxSourceDisable(object? command)
    {
        if (command is ApproveMailboxSourceDisable typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveMailboxSourceDisable>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitServiceClientDisable? ReadSubmitServiceClientDisable(object? command)
    {
        if (command is SubmitServiceClientDisable typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitServiceClientDisable>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveServiceClientDisable? ReadApproveServiceClientDisable(object? command)
    {
        if (command is ApproveServiceClientDisable typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveServiceClientDisable>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitAiActorDisable? ReadSubmitAiActorDisable(object? command)
    {
        if (command is SubmitAiActorDisable typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitAiActorDisable>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveAiActorDisable? ReadApproveAiActorDisable(object? command)
    {
        if (command is ApproveAiActorDisable typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveAiActorDisable>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitAiActorQuarantine? ReadSubmitAiActorQuarantine(object? command)
    {
        if (command is SubmitAiActorQuarantine typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitAiActorQuarantine>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveAiActorQuarantine? ReadApproveAiActorQuarantine(object? command)
    {
        if (command is ApproveAiActorQuarantine typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveAiActorQuarantine>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitServiceClientQuarantine? ReadSubmitServiceClientQuarantine(object? command)
    {
        if (command is SubmitServiceClientQuarantine typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitServiceClientQuarantine>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveServiceClientQuarantine? ReadApproveServiceClientQuarantine(object? command)
    {
        if (command is ApproveServiceClientQuarantine typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveServiceClientQuarantine>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidNotificationRoutingChange(object? command)
    {
        SubmitNotificationRoutingChange? change = ReadSubmitNotificationRoutingChange(command);
        return change is not null &&
            change.SourceVersion >= 0 &&
            IsSafeAdminToken(change.RoutingChangeId) &&
            IsSafeAdminToken(change.SourceRoutingSnapshotId) &&
            IsSafeAdminToken(change.ProposedRoutingSnapshotId) &&
            IsSafeAdminToken(change.ReasonCode) &&
            IsSafeAdminToken(change.RequesterRef) &&
            NotificationRoutingSchemaVersions.IsKnown(change.SchemaVersion) &&
            IsSafeAdminToken(change.CorrelationId) &&
            NotificationRoutingSchema.IsSafeFingerprint(change.OldRoutingFingerprint) &&
            NotificationRoutingSchema.IsSafeFingerprint(change.NewRoutingFingerprint) &&
            NotificationRoutingSchema.Validate(change.ChangeSet).IsValid;
    }

    private static SubmitNotificationRoutingChange? ReadSubmitNotificationRoutingChange(object? command)
    {
        if (command is SubmitNotificationRoutingChange typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitNotificationRoutingChange>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidEscalationPolicyChange(object? command)
    {
        SubmitEscalationPolicyChange? change = ReadSubmitEscalationPolicyChange(command);
        return change is not null &&
            change.SourceVersion >= 0 &&
            IsSafeAdminToken(change.EscalationPolicyChangeId) &&
            IsSafeAdminToken(change.SourceEscalationSnapshotId) &&
            IsSafeAdminToken(change.ProposedEscalationSnapshotId) &&
            IsSafeAdminToken(change.ReasonCode) &&
            IsSafeAdminToken(change.RequesterRef) &&
            EscalationPolicySchemaVersions.IsKnown(change.SchemaVersion) &&
            IsSafeAdminToken(change.CorrelationId) &&
            EscalationPolicySchema.IsSafeFingerprint(change.OldEscalationFingerprint) &&
            EscalationPolicySchema.IsSafeFingerprint(change.NewEscalationFingerprint) &&
            EscalationPolicySchema.Validate(change.ChangeSet).IsValid;
    }

    private static SubmitEscalationPolicyChange? ReadSubmitEscalationPolicyChange(object? command)
    {
        if (command is SubmitEscalationPolicyChange typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitEscalationPolicyChange>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidMailboxProviderConnection(object? command)
    {
        RecordMailboxProviderConnection? connection = ReadRecordMailboxProviderConnection(command);
        return connection is not null &&
            connection.SourceVersion >= 0 &&
            IsSafeAdminToken(connection.ProviderConnectionChangeId) &&
            IsSafeAdminToken(connection.ProviderConnectionRef) &&
            connection.ProviderKind is not MailboxProviderKind.Unknown &&
            Enum.IsDefined(connection.ProviderKind) &&
            MailboxConfigurationSchema.IsSafeFingerprint(connection.CredentialFingerprint) &&
            IsSafeAdminToken(connection.PermissionEvidenceRef) &&
            connection.Freshness is not MailboxPermissionFreshnessState.Unknown &&
            Enum.IsDefined(connection.Freshness) &&
            IsSafeAdminToken(connection.ReasonCode) &&
            IsSafeAdminToken(connection.RequesterRef) &&
            MailboxConfigurationSchemaVersions.IsKnown(connection.SchemaVersion) &&
            IsSafeAdminToken(connection.CorrelationId) &&
            IsSafeAdminToken(connection.PolicySnapshotId);
    }

    private static bool IsValidComplianceInvestigation(object? command)
    {
        RequestComplianceInvestigation? investigation = ReadRequestComplianceInvestigation(command);
        return investigation is not null &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(investigation.InvestigationId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(investigation.QueryRef) &&
            investigation.FilterRefs is { Count: > 0 } &&
            investigation.FilterRefs.Count <= ComplianceAdministrationSchema.MaxAuditFilters &&
            investigation.FilterRefs.All(ComplianceAdministrationSchema.IsSafeComplianceToken) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(investigation.ReasonCode) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(investigation.RequesterRef) &&
            investigation.SourceVersion >= 0 &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(investigation.CorrelationId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(investigation.PolicySnapshotId) &&
            ComplianceAdministrationSchema.IsValidRedactionState(investigation.RedactionState) &&
            ComplianceAdministrationSchema.IsValidEscalationStatus(investigation.EscalationStatus) &&
            ComplianceAdministrationSchemaVersions.IsKnown(investigation.SchemaVersion);
    }

    private static bool IsValidComplianceEscalation(object? command)
    {
        RequestComplianceEscalation? escalation = ReadRequestComplianceEscalation(command);
        return escalation is not null &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(escalation.EscalationId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(escalation.InvestigationId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(escalation.AuditRecordRef) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(escalation.ReasonCode) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(escalation.RequesterRef) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(escalation.EscalationTargetRef) &&
            escalation.SourceVersion >= 0 &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(escalation.CorrelationId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(escalation.PolicySnapshotId) &&
            ComplianceAdministrationSchema.IsValidRedactionState(escalation.RedactionState) &&
            ComplianceAdministrationSchema.IsValidEscalationStatus(escalation.EscalationStatus) &&
            ComplianceAdministrationSchemaVersions.IsKnown(escalation.SchemaVersion);
    }

    private static bool IsValidRetentionConfigurationChange(object? command)
    {
        SubmitRetentionConfigurationChange? change = ReadSubmitRetentionConfigurationChange(command);
        return change is not null &&
            change.SourceVersion >= 0 &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.RetentionChangeId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.SourceRetentionSnapshotId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.ProposedRetentionSnapshotId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.ReasonCode) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.RequesterRef) &&
            ComplianceAdministrationSchemaVersions.IsKnown(change.SchemaVersion) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.CorrelationId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.PolicySnapshotId) &&
            ComplianceAdministrationSchema.IsSafeFingerprint(change.OldRetentionSnapshotFingerprint) &&
            ComplianceAdministrationSchema.IsSafeFingerprint(change.NewRetentionSnapshotFingerprint) &&
            ComplianceAdministrationSchema.IsUtc(change.EffectiveAtUtc) &&
            ComplianceAdministrationSchema.ValidateRetentionChangeSet(change.ChangeSet).IsValid;
    }

    private static bool IsValidDataClassInventoryChange(object? command)
    {
        SubmitDataClassInventoryChange? change = ReadSubmitDataClassInventoryChange(command);
        return change is not null &&
            change.SourceVersion >= 0 &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.InventoryChangeId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.SourceInventorySnapshotId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.ProposedInventorySnapshotId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.ReasonCode) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.RequesterRef) &&
            DataClassInventorySchemaVersions.IsKnown(change.SchemaVersion) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.CorrelationId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(change.PolicySnapshotId) &&
            ComplianceAdministrationSchema.IsSafeFingerprint(change.OldInventorySnapshotFingerprint) &&
            ComplianceAdministrationSchema.IsSafeFingerprint(change.NewInventorySnapshotFingerprint) &&
            ComplianceAdministrationSchema.IsUtc(change.EffectiveAtUtc) &&
            DataClassInventorySchema.ValidateChangeSet(change.ChangeSet).IsValid;
    }

    private static bool IsValidTenantExportRequest(object? command)
    {
        SubmitTenantExportRequest? request = ReadSubmitTenantExportRequest(command);
        return request is not null &&
            request.SourceVersion >= 0 &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.ExportRunId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.InventorySnapshotId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.ReasonCode) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.RequesterRef) &&
            TenantExportSchemaVersions.IsKnown(request.SchemaVersion) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.CorrelationId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.PolicySnapshotId) &&
            ComplianceAdministrationSchema.IsSafeFingerprint(request.ManifestFingerprint) &&
            ComplianceAdministrationSchema.IsUtc(request.EffectiveAtUtc) &&
            TenantExportSchema.ValidateRequestSpec(request.RequestSpec).IsValid;
    }

    private static bool IsValidDeletionErasureRequest(object? command)
    {
        SubmitDeletionErasureRequest? request = ReadSubmitDeletionErasureRequest(command);
        return request is not null &&
            request.SourceVersion >= 0 &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.DeletionRunId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.InventorySnapshotId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.ReasonCode) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.RequesterRef) &&
            DeletionErasureSchemaVersions.IsKnown(request.SchemaVersion) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.CorrelationId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.PolicySnapshotId) &&
            ComplianceAdministrationSchema.IsSafeFingerprint(request.ProofFingerprint) &&
            ComplianceAdministrationSchema.IsUtc(request.EffectiveAtUtc) &&
            DeletionErasureSchema.ValidateRequestSpec(request.RequestSpec).IsValid;
    }

    private static bool IsValidConsentLawfulBasisRecord(object? command)
    {
        SubmitConsentLawfulBasisRecord? request = ReadSubmitConsentLawfulBasisRecord(command);
        return request is not null &&
            request.SourceVersion >= 0 &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.RecordId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.SubjectLocator) &&
            AuditMetadata.IsSafeStableIdentifier(request.SubjectLocator) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.ProjectScopeRef) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.BasisSource) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.ReasonCode) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.RequesterRef) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.CorrelationId) &&
            ComplianceAdministrationSchema.IsSafeComplianceToken(request.PolicySnapshotId) &&
            ConsentSubjectKinds.Contains(request.SubjectKind) &&
            ConsentLawfulBases.Contains(request.LawfulBasis) &&
            ConsentRecordStatuses.Contains(request.RecordStatus) &&
            DataClassRedactionSensitivities.Contains(request.RedactionSensitivity) &&
            ConsentLawfulBasisSchemaVersions.IsKnown(request.SchemaVersion) &&
            ComplianceAdministrationSchema.IsSafeFingerprint(request.RecordFingerprint) &&
            ComplianceAdministrationSchema.IsUtc(request.EffectiveAtUtc);
    }

    private static bool IsValidChangedKnobs(IReadOnlyList<string>? changedKnobIds, TenantPolicyChangeSet? changeSet)
    {
        if (changedKnobIds is not { Count: > 0 } || changeSet?.Values is not { Count: > 0 })
        {
            return false;
        }

        HashSet<string> changed = changedKnobIds.ToHashSet(StringComparer.Ordinal);
        HashSet<string> values = changeSet.Values.Select(static value => value.KnobId).ToHashSet(StringComparer.Ordinal);
        return changed.SetEquals(values) &&
            changed.All(static knob => TenantPolicySchema.TryGetDefinition(knob, out _));
    }

    private static SubmitTenantPolicyChange? ReadSubmitTenantPolicyChange(object? command)
    {
        if (command is SubmitTenantPolicyChange typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitTenantPolicyChange>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static ApproveTenantPolicyChange? ReadApproveTenantPolicyChange(object? command)
    {
        if (command is ApproveTenantPolicyChange typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ApproveTenantPolicyChange>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitMailboxConfigurationChange? ReadSubmitMailboxConfigurationChange(object? command)
    {
        if (command is SubmitMailboxConfigurationChange typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitMailboxConfigurationChange>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static RecordMailboxProviderConnection? ReadRecordMailboxProviderConnection(object? command)
    {
        if (command is RecordMailboxProviderConnection typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<RecordMailboxProviderConnection>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static RequestComplianceInvestigation? ReadRequestComplianceInvestigation(object? command)
    {
        if (command is RequestComplianceInvestigation typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<RequestComplianceInvestigation>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static RequestComplianceEscalation? ReadRequestComplianceEscalation(object? command)
    {
        if (command is RequestComplianceEscalation typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<RequestComplianceEscalation>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitRetentionConfigurationChange? ReadSubmitRetentionConfigurationChange(object? command)
    {
        if (command is SubmitRetentionConfigurationChange typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitRetentionConfigurationChange>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitDataClassInventoryChange? ReadSubmitDataClassInventoryChange(object? command)
    {
        if (command is SubmitDataClassInventoryChange typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitDataClassInventoryChange>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitTenantExportRequest? ReadSubmitTenantExportRequest(object? command)
    {
        if (command is SubmitTenantExportRequest typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitTenantExportRequest>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitDeletionErasureRequest? ReadSubmitDeletionErasureRequest(object? command)
    {
        if (command is SubmitDeletionErasureRequest typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitDeletionErasureRequest>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static SubmitConsentLawfulBasisRecord? ReadSubmitConsentLawfulBasisRecord(object? command)
    {
        if (command is SubmitConsentLawfulBasisRecord typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<SubmitConsentLawfulBasisRecord>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static AssignTenantAdminRole? ReadAssignTenantAdminRole(object? command)
    {
        if (command is AssignTenantAdminRole typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<AssignTenantAdminRole>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsValidAdminQueueOperation(object? command)
    {
        ExecuteAdminQueueOperation? operation = ReadExecuteAdminQueueOperation(command);
        return operation is not null &&
            AdminQueueOperations.All.Contains(operation.Operation) &&
            operation.ScopeUsed == AdminScope.Operate &&
            IsSafeAdminToken(operation.OperationId) &&
            IsSafeAdminToken(operation.QueueRef) &&
            IsAllowedAdminQueueReason(operation.ReasonCode) &&
            IsSafeAdminToken(operation.PolicySnapshotId) &&
            IsSafeAdminToken(operation.RedactionState) &&
            operation.SourceVersion >= 0 &&
            operation.ItemCount > 0 &&
            operation.ItemRefs is not null &&
            operation.ItemRefs.All(IsSafeAdminToken) &&
            (operation.ItemRefs.Count == 0 || operation.ItemRefs.Count == operation.ItemCount) &&
            IsValidOperationalQueueMetadata(operation);
    }

    private static ExecuteAdminQueueOperation? ReadExecuteAdminQueueOperation(object? command)
    {
        if (command is ExecuteAdminQueueOperation typed)
        {
            return typed;
        }

        try
        {
            JsonElement element = command is JsonElement json
                ? json
                : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return element.ValueKind == JsonValueKind.Object
                ? element.Deserialize<ExecuteAdminQueueOperation>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsSafeAdminToken(string? value)
        => AuditMetadata.SafeOptionalToken(value) is not null;

    private static bool IsValidOperationalQueueMetadata(ExecuteAdminQueueOperation operation)
    {
        bool isAssignmentOperation = operation.Operation is AdminQueueOperation.Claim or AdminQueueOperation.Assign or AdminQueueOperation.Prioritize;
        if (!isAssignmentOperation)
        {
            return IsSafeOptionalAdminToken(operation.AssigneeRef) &&
                IsSafeOptionalAdminToken(operation.ReviewerRef) &&
                IsSafeOptionalAdminToken(operation.PreviousAssigneeRef) &&
                IsUtcIfPresent(operation.CommandTimestampUtc) &&
                !IsTerminalOrCompleted(operation.OperationState);
        }

        if (operation.QueueFamily is not { } queueFamily ||
            !OperationalQueueFamilies.All.Contains(queueFamily) ||
            !IsUtcIfPresent(operation.CommandTimestampUtc) ||
            IsTerminalOrCompleted(operation.OperationState))
        {
            return false;
        }

        return operation.Operation switch
        {
            AdminQueueOperation.Claim => IsSafeAdminToken(operation.ReviewerRef) &&
                IsSafeOptionalAdminToken(operation.AssigneeRef) &&
                IsSafeOptionalAdminToken(operation.PreviousAssigneeRef),
            AdminQueueOperation.Assign => IsSafeAdminToken(operation.AssigneeRef) &&
                IsSafeAdminToken(operation.ReviewerRef) &&
                IsSafeOptionalAdminToken(operation.PreviousAssigneeRef),
            AdminQueueOperation.Prioritize => IsSafeOptionalAdminToken(operation.AssigneeRef) &&
                IsSafeOptionalAdminToken(operation.ReviewerRef) &&
                IsSafeOptionalAdminToken(operation.PreviousAssigneeRef),
            _ => false,
        };
    }

    private static bool IsSafeOptionalAdminToken(string? value)
        => string.IsNullOrWhiteSpace(value) || IsSafeAdminToken(value);

    private static bool IsUtcIfPresent(DateTimeOffset? timestamp)
        => timestamp is null || timestamp.Value.Offset == TimeSpan.Zero;

    private static bool IsTerminalOrCompleted(string? state)
        => state is not null &&
            (string.Equals(state, "terminal", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(state, "completed", StringComparison.OrdinalIgnoreCase));

    private static bool IsAllowedAdminQueueReason(string? reasonCode)
        => reasonCode is ChatBotDisabledActionReasons.DependencyDegraded
            or ChatBotDisabledActionReasons.InsufficientAuthority
            or ChatBotDisabledActionReasons.PolicyBlocked
            or ChatBotAuthorizationReasonCodes.AuthorizationDenied
            or "operator-claim"
            or "operator-assign"
            or "operator-prioritize"
            or "stale-source-version";

    private static bool HasTrustedOutboundDraftOrigin(
        CreateOutboundDraft command,
        ChatBotAuthenticatedActor actor,
        ServiceClientGrantEvidence? serviceClientGrantEvidence)
    {
        if (!string.Equals(command.SourceActorId, actor.ActorId, StringComparison.Ordinal))
        {
            return false;
        }

        bool isServiceOrAiActor =
            string.Equals(actor.ActorType, ServiceActorValue, StringComparison.Ordinal) ||
            string.Equals(actor.ActorType, AiActorValue, StringComparison.Ordinal);
        if (!isServiceOrAiActor)
        {
            return true;
        }

        return serviceClientGrantEvidence is not null &&
            !string.IsNullOrWhiteSpace(serviceClientGrantEvidence.DelegatedUserId) &&
            string.Equals(command.RequesterId, serviceClientGrantEvidence.DelegatedUserId, StringComparison.Ordinal);
    }

    private static bool CanCorrectAssociation(ClaimsPrincipal principal, object? command)
    {
        if (!principal.HasClaim(ActorTypeClaim, HumanActorValue))
        {
            return false;
        }

        (string? PriorProjectId, string? TargetProjectId) projects = CorrectionProjects(command);
        if (string.IsNullOrWhiteSpace(projects.PriorProjectId) ||
            string.IsNullOrWhiteSpace(projects.TargetProjectId))
        {
            return false;
        }

        string[] ownedProjects = principal
            .FindAll(ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return ownedProjects.Contains("*", StringComparer.Ordinal) ||
            (ownedProjects.Contains(projects.PriorProjectId, StringComparer.Ordinal) &&
                ownedProjects.Contains(projects.TargetProjectId, StringComparer.Ordinal));
    }

    private static (string? PriorProjectId, string? TargetProjectId) CorrectionProjects(object? command)
    {
        if (command is CorrectEmailProjectAssociation typed)
        {
            return (typed.PriorProjectId, typed.TargetProjectId);
        }

        JsonElement element = command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (element.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        string? priorProjectId = element.TryGetProperty("priorProjectId", out JsonElement priorProject) &&
            priorProject.ValueKind == JsonValueKind.String
                ? priorProject.GetString()
                : null;
        string? targetProjectId = element.TryGetProperty("targetProjectId", out JsonElement targetProject) &&
            targetProject.ValueKind == JsonValueKind.String
                ? targetProject.GetString()
                : null;
        return (priorProjectId, targetProjectId);
    }

    private static bool CanReadProject(ClaimsPrincipal principal, object? command)
    {
        string? projectId = CommandProjectId(command);
        if (string.IsNullOrWhiteSpace(projectId))
        {
            return false;
        }

        string[] projectClaims = principal
            .FindAll(ProjectOwnerClaim)
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
        return projectClaims.Contains("*", StringComparer.Ordinal) ||
            projectClaims.Contains(projectId, StringComparer.Ordinal);
    }

    private static string? CommandProjectId(object? command)
    {
        if (command is ExecuteLowRiskAIAssistance typed)
        {
            return typed.ProjectId;
        }

        if (command is ExecuteApprovedAIAction approved)
        {
            return approved.ProjectId;
        }

        JsonElement element = command is JsonElement json
            ? json
            : JsonSerializer.SerializeToElement(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("projectId", out JsonElement projectId) &&
            projectId.ValueKind == JsonValueKind.String
                ? projectId.GetString()
                : null;
    }

    private sealed class PassThroughServiceClientGrantValidator : IServiceClientGrantValidator
    {
        public ValueTask<ChatBotAuthorizationResult> ValidateAsync(
            ChatBotCommandSubmission submission,
            ChatBotAuthenticatedActor actor,
            ChatBotTenantBinding tenantBinding,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(ChatBotAuthorizationResult.Allowed());
    }
}
