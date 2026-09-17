using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal enum AiActionPolicyDecisionKind
{
    LowRiskExecuteAllowed,
    LowRiskRoutedToApproval,
    Blocked,
}
