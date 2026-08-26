using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Acceptance;

internal sealed class Story132AcceptanceTenantAiPolicySnapshotProvider : ITenantAiPolicySnapshotProvider
{
    private const string DefaultSnapshotId = "story-13-2-acceptance-policy";

    public ValueTask<TenantAiPolicySnapshot?> TryGetAsync(
        string tenantId,
        string projectId,
        string? requestedPolicySnapshotId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        cancellationToken.ThrowIfCancellationRequested();

        string snapshotId = string.IsNullOrWhiteSpace(requestedPolicySnapshotId)
            ? DefaultSnapshotId
            : requestedPolicySnapshotId;
        return ValueTask.FromResult<TenantAiPolicySnapshot?>(new TenantAiPolicySnapshot(
            snapshotId,
            LowRiskAllowed: true,
            EffectSurface: "read-only",
            AssistanceKinds: ["summarize-visible-context", "explain-visible-evidence"],
            IsFresh: true,
            IsValid: true));
    }
}
