using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Story 9.10 (AC1/AC4, inert-control-floor): the DEFERRED server seam that will build a tenant-overridden
/// <see cref="ConsentRequirementProfile"/> from a referenced tenant-policy snapshot. For now it returns the
/// regulatory-profile default (<see cref="ConsentRequirementMatrix.Published"/>) — the live policy-snapshot →
/// requirement-profile merge (the tenant-policy-knob override that lets a tenant additionally require a basis beyond
/// the regulatory default) is M2-deferred. The <see cref="ConsentRequirementMatrix.Published"/> seed + the pure
/// evaluator ship now; this mapper is the seam the override will populate.
/// </summary>
internal static class ConsentRequirementProfileMapper
{
    /// <summary>
    /// Resolves the requirement profile for <paramref name="policySnapshotId"/>. DEFERRED: the live merge of the
    /// tenant policy-snapshot override over the regulatory default is not wired in this story — it returns the
    /// published regulatory-profile matrix unchanged. See the Story 9.10 ADR deferrals.
    /// </summary>
    public static ConsentRequirementProfile ProfileFor(string? policySnapshotId)
    {
        _ = policySnapshotId;

        // Deferred: no tenant policy-snapshot → requirement-profile merge ships in Story 9.10. The regulatory-profile
        // default biases every governed subject kind to `required` (fail-closed).
        return ConsentRequirementMatrix.Published;
    }
}
