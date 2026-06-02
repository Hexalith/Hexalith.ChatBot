using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Governance.Admin;

namespace Hexalith.ChatBot.Server.Projections;

internal static class AdminQueueSummaryReadPolicy
{
    public static AdminQueueSummaryReadDecision Evaluate(
        ClaimsPrincipal principal,
        int aggregationCount,
        int auditThreshold,
        bool auditAvailable)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (!AdminAuthorityEvaluator.HasHumanAdminScope(principal, AdminScope.SeeOnly))
        {
            return AdminQueueSummaryReadDecision.Denied(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        if (aggregationCount >= auditThreshold && !auditAvailable)
        {
            return AdminQueueSummaryReadDecision.Denied("audit_unavailable");
        }

        return AdminQueueSummaryReadDecision.Allowed();
    }
}
