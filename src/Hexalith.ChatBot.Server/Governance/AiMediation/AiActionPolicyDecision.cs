using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed record AiActionPolicyDecision(
    AiActionPolicyDecisionKind Kind,
    string PolicySnapshotId,
    string ReasonCode,
    string SafeNextAction)
{
    public bool AllowsLowRiskExecution => Kind is AiActionPolicyDecisionKind.LowRiskExecuteAllowed;

    public static AiActionPolicyDecision Allowed(string policySnapshotId)
        => new(AiActionPolicyDecisionKind.LowRiskExecuteAllowed, policySnapshotId, "low-risk-execute-allowed", "none");

    public static AiActionPolicyDecision Routed(string policySnapshotId, string reasonCode)
        => new(AiActionPolicyDecisionKind.LowRiskRoutedToApproval, policySnapshotId, reasonCode, "review-ai-action");

    public static AiActionPolicyDecision Blocked(string policySnapshotId, string reasonCode)
        => new(AiActionPolicyDecisionKind.Blocked, policySnapshotId, reasonCode, "none");
}
