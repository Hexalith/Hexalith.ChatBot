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
