using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Supplies explicitly synthetic evidence for admission regression tests.</summary>
/// <param name="snapshot">The unavailable, stale, or foreign synthetic snapshot.</param>
internal sealed class SyntheticAdmissionPolicySnapshotProvider(TenantAiPolicySnapshot? snapshot) : ITenantAiPolicySnapshotProvider
{
    /// <summary>Gets the number of evidence reads, proving admission reached policy resolution.</summary>
    public int ReadCount { get; private set; }

    /// <summary>Returns the fixture evidence without granting production policy availability.</summary>
    public ValueTask<TenantAiPolicySnapshot?> TryGetAsync(string tenantId, string projectId, string? requestedPolicySnapshotId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCount++;
        return ValueTask.FromResult(snapshot);
    }
}
