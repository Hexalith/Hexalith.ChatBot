using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.RecoverySandbox;

/// <summary>
/// Fixed low-risk-allowed policy snapshot so the real <c>AiActionApprovalGate</c>/<c>DefaultAiActionPolicyEvaluator</c>
/// route the recovery exercise's <c>ExecuteLowRiskAIAssistance</c> command to the real AI provider instead of
/// pending-approval. The policy dimension is orthogonal to the ai-provider fault under test.
/// </summary>
internal sealed class RecoveryTenantAiPolicySnapshotProvider : ITenantAiPolicySnapshotProvider
{
    /// <inheritdoc />
    public ValueTask<TenantAiPolicySnapshot?> TryGetAsync(
        string tenantId,
        string projectId,
        string? requestedPolicySnapshotId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TenantAiPolicySnapshot snapshot = new(
            string.IsNullOrWhiteSpace(requestedPolicySnapshotId) ? "policy-recovery" : requestedPolicySnapshotId,
            LowRiskAllowed: true,
            "read-only",
            ["summarize-visible-context", "explain-visible-evidence"],
            IsFresh: true,
            IsValid: true);
        return ValueTask.FromResult<TenantAiPolicySnapshot?>(snapshot);
    }
}
