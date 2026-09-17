namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal enum ChatBotApprovalResultKind
{
    Approved,
    AllowedLowRiskExecution,
    RoutedToApproval,
    ApprovalDecisionAllowed,
    Blocked,
}
