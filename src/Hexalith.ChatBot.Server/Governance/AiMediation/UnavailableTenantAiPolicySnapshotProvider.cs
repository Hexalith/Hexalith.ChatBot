namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed class UnavailableTenantAiPolicySnapshotProvider : ITenantAiPolicySnapshotProvider
{
    public ValueTask<TenantAiPolicySnapshot?> TryGetAsync(
        string tenantId,
        string projectId,
        string? requestedPolicySnapshotId,
        CancellationToken cancellationToken)
        => ValueTask.FromResult<TenantAiPolicySnapshot?>(null);
}
