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

internal enum AiActionPolicyDecisionKind
{
    LowRiskExecuteAllowed,
    LowRiskRoutedToApproval,
    Blocked,
}

internal sealed record AiActionPolicyEvaluationRequest(
    string TenantId,
    string ProjectId,
    string ProposalId,
    string ContextPackageId,
    string ContextPackageVersion,
    string? RequestedPolicySnapshotId,
    AiActionRiskClass RiskClass,
    IReadOnlyList<string> RiskActionClasses,
    string EffectSurface,
    string AssistanceKind,
    bool HasProjectAuthorization);
