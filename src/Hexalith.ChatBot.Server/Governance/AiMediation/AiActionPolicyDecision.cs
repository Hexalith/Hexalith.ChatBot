using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

/// <summary>A safe decision retaining the exact reference only after successful policy resolution.</summary>
/// <param name="Kind">The execution or approval disposition.</param>
/// <param name="PolicySnapshotId">The resolved immutable identity, or unavailable.</param>
/// <param name="ReasonCode">The safe reason.</param>
/// <param name="SafeNextAction">The safe permitted next action.</param>
/// <param name="TenantId">The resolved tenant, absent for unresolved evidence.</param>
/// <param name="PolicyVersion">The exact resolved version, absent for unresolved evidence.</param>
internal sealed record AiActionPolicyDecision(
    AiActionPolicyDecisionKind Kind,
    string PolicySnapshotId,
    string ReasonCode,
    string SafeNextAction,
    string? TenantId = null,
    long? PolicyVersion = null)
{
    /// <summary>Gets whether this decision permits low-risk execution.</summary>
    public bool AllowsLowRiskExecution => Kind is AiActionPolicyDecisionKind.LowRiskExecuteAllowed;

    /// <summary>Retains the exact validated snapshot reference for an allowed execution.</summary>
    public static AiActionPolicyDecision Allowed(TenantAiPolicySnapshot snapshot)
        => new(AiActionPolicyDecisionKind.LowRiskExecuteAllowed, snapshot.PolicySnapshotId, "low-risk-execute-allowed", "none", snapshot.TenantId, snapshot.PolicyVersion);

    /// <summary>Routes an unresolved decision without a resolved tenant/version reference.</summary>
    public static AiActionPolicyDecision Routed(string policySnapshotId, string reasonCode)
        => new(AiActionPolicyDecisionKind.LowRiskRoutedToApproval, policySnapshotId, reasonCode, "review-ai-action");

    /// <summary>Retains the exact resolved snapshot reference for a denied execution.</summary>
    public static AiActionPolicyDecision Routed(TenantAiPolicySnapshot snapshot, string reasonCode)
        => new(AiActionPolicyDecisionKind.LowRiskRoutedToApproval, snapshot.PolicySnapshotId, reasonCode, "review-ai-action", snapshot.TenantId, snapshot.PolicyVersion);

    /// <summary>Blocks execution without a resolved tenant/version reference.</summary>
    public static AiActionPolicyDecision Blocked(string policySnapshotId, string reasonCode)
        => new(AiActionPolicyDecisionKind.Blocked, policySnapshotId, reasonCode, "none");
}
