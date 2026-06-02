using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Governance.Admin;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record NotificationRoutingReadDecision(
    bool IsAllowed,
    string ReasonCode,
    NotificationRoutingSummary? Summary)
{
    public static NotificationRoutingReadDecision Denied(string reasonCode)
        => new(false, reasonCode, null);

    public static NotificationRoutingReadDecision Allowed(NotificationRoutingSummary summary)
        => new(true, "authorized", summary);
}

/// <summary>
/// Gates notification routing-config read-back to human admins holding the routing scope
/// (<see cref="AdminScope.Policy"/>, held by <c>policy-admin</c> and <c>tenant-admin</c>), reusing the existing
/// read-policy pattern. Denials carry a safe reason code and leak no resource existence.
/// </summary>
internal static class NotificationRoutingReadPolicy
{
    public static NotificationRoutingReadDecision Read(
        ClaimsPrincipal principal,
        GetNotificationRoutingSummary query,
        NotificationRoutingChangeSet snapshot,
        long sourceVersion,
        string routingFingerprint,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(snapshot);

        if (!AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.Policy) ||
            !NotificationRoutingSchema.IsSafeRoutingToken(query.ActiveSnapshotRef) ||
            !NotificationRoutingSchema.IsSafeRoutingToken(correlationId))
        {
            return NotificationRoutingReadDecision.Denied(ChatBotAuthorizationReasonCodes.NotificationRoutingUnauthorized);
        }

        NotificationRoutingSummary summary = NotificationRoutingSnapshotProjector.Create(
            snapshot,
            query.ActiveSnapshotRef,
            sourceVersion,
            routingFingerprint,
            correlationId);

        return NotificationRoutingReadDecision.Allowed(summary);
    }
}
