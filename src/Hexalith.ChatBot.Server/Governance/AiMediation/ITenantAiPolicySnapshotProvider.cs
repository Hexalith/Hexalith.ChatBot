namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal interface ITenantAiPolicySnapshotProvider
{
    ValueTask<TenantAiPolicySnapshot?> TryGetAsync(
        string tenantId,
        string projectId,
        string? requestedPolicySnapshotId,
        CancellationToken cancellationToken);
}
