using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Governance.Admin;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record EscalationPolicyReadDecision(
    bool IsAllowed,
    string ReasonCode,
    EscalationPolicySummary? Summary)
{
    public static EscalationPolicyReadDecision Denied(string reasonCode)
        => new(false, reasonCode, null);

    public static EscalationPolicyReadDecision Allowed(EscalationPolicySummary summary)
        => new(true, "authorized", summary);
}

/// <summary>
/// Gates escalation-policy read-back to human admins holding the policy scope (<see cref="AdminScope.Policy"/>, held
/// by <c>policy-admin</c> and <c>tenant-admin</c>), reusing the existing read-policy pattern. Denials carry a safe
/// reason code and leak no resource existence. Mirrors <see cref="NotificationRoutingReadPolicy"/>.
/// </summary>
internal static class EscalationPolicyReadPolicy
{
    public static EscalationPolicyReadDecision Read(
        ClaimsPrincipal principal,
        GetEscalationPolicySummary query,
        EscalationPolicyChangeSet snapshot,
        long sourceVersion,
        string escalationFingerprint,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Policy) ||
            !EscalationPolicySchema.IsSafeEscalationToken(query.ActiveSnapshotRef) ||
            !EscalationPolicySchema.IsSafeEscalationToken(correlationId))
        {
            return EscalationPolicyReadDecision.Denied(ChatBotAuthorizationReasonCodes.EscalationPolicyUnauthorized);
        }

        EscalationPolicySummary summary = EscalationPolicySnapshotProjector.Create(
            snapshot,
            query.ActiveSnapshotRef,
            sourceVersion,
            escalationFingerprint,
            correlationId);

        return EscalationPolicyReadDecision.Allowed(summary);
    }
}
