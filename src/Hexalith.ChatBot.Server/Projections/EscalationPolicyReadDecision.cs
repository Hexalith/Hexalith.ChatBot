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
