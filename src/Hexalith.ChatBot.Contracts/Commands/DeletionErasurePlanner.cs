using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The pure deletion/erasure decision engine (AC1/AC2/AC4/AC5). Reads the Story 9.7 <see cref="DataClassInventory"/>
/// as the single source of truth for the per-class <c>DeletionBehavior</c> — it never forks a second behavior,
/// class-id, or sensitivity set. Authority is supplied pre-bounded as a <see cref="DeletionErasureAuthorityView"/> so
/// the function has no <c>ClaimsPrincipal</c> dependency and the no-leak boundary holds. Destruction is biased
/// fail-closed: WORM behavior is absolute over authority (<c>retain-immutable</c> stays <c>retained</c>/<c>worm-retained</c>,
/// never <c>unauthorized</c>), and an unauthorized class is <c>retained</c>, never destructive.
/// </summary>
public static class DeletionErasurePlanner
{
    public static DeletionErasureRunResult Plan(
        DataClassInventory inventory,
        DeletionErasureRequestSpec spec,
        DeletionErasureAuthorityView authority,
        string deletionRunId,
        DateTimeOffset generatedAtUtc,
        string correlationId)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(authority);

        Dictionary<string, DataClassClassification> byId = inventory.Classifications
            .Where(static classification => classification is not null)
            .GroupBy(static classification => classification.DataClassId, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.First(), StringComparer.Ordinal);

        bool projectBounded = spec.Scope.ProjectScopeRefs is { Count: > 0 };
        bool unauthorizedScope = !authority.HasComplianceScope ||
            (projectBounded && !spec.Scope.ProjectScopeRefs.All(authority.AuthorizedProjectRefs.Contains));

        List<DeletionErasureClassResult> classResults = [];
        foreach (string dataClassId in spec.RequestedDataClassIds)
        {
            classResults.Add(PlanClass(byId.GetValueOrDefault(dataClassId), dataClassId, unauthorizedScope));
        }

        ErasureProofEntry[] entries = classResults
            .Where(IsSucceededDestructive)
            .Select(ProofEntryFor)
            .ToArray();

        ErasureProofArtifact proof = new(
            deletionRunId,
            entries,
            ComputeProofFingerprint(entries),
            generatedAtUtc.ToUniversalTime(),
            correlationId);

        return new DeletionErasureRunResult(
            deletionRunId,
            spec.Mode,
            RunStatusFor(classResults),
            classResults,
            proof,
            generatedAtUtc.ToUniversalTime(),
            correlationId);
    }

    private static DeletionErasureClassResult PlanClass(
        DataClassClassification? classification,
        string dataClassId,
        bool unauthorizedScope)
    {
        // Fail-closed: an unclassifiable class is never destroyed — it is retained as if WORM.
        string behavior = classification?.DeletionBehavior ?? DataClassDeletionBehaviors.RetainImmutable;
        string ownerRole = classification?.OwnerRole ?? AdminRoles.ComplianceAdmin;

        // 1) WORM behavior is absolute over authority (architecture #13): a retain-immutable class is ALWAYS
        //    retained/worm-retained, regardless of authority — so audit-records are never mislabeled `unauthorized`
        //    and never escalate to a destructive action.
        if (string.Equals(behavior, DataClassDeletionBehaviors.RetainImmutable, StringComparison.Ordinal))
        {
            return Retained(dataClassId, behavior, ownerRole, DeletionErasureExclusionReasons.WormRetained);
        }

        // 2) Authority gates destructive behaviors. On a missing project grant the class is retained/unauthorized and
        //    carries no resource identity (NFR2). Destruction is fail-closed: unauthorized never escalates to destroy.
        if (unauthorizedScope)
        {
            return Retained(dataClassId, behavior, ownerRole, DeletionErasureExclusionReasons.Unauthorized);
        }

        // 3) Behavior → action for the authorized, destructive classes.
        string action = behavior switch
        {
            DataClassDeletionBehaviors.KeyShred => DeletionErasureClassActions.CryptoShredded,
            DataClassDeletionBehaviors.ProjectionTombstone => DeletionErasureClassActions.Tombstoned,
            DataClassDeletionBehaviors.HardDelete => DeletionErasureClassActions.HardDeleted,
            _ => DeletionErasureClassActions.Retained,
        };

        return new DeletionErasureClassResult(
            dataClassId,
            behavior,
            action,
            string.Empty,
            DeletionErasureClassStatuses.Succeeded,
            ownerRole);
    }

    private static DeletionErasureClassResult Retained(
        string dataClassId,
        string behavior,
        string ownerRole,
        string exclusionReason)
        => new(
            dataClassId,
            behavior,
            DeletionErasureClassActions.Retained,
            exclusionReason,
            DeletionErasureClassStatuses.Succeeded,
            ownerRole);

    /// <summary>A succeeded class whose action crypto-shreds or tombstones bytes — the proof-bearing destructive set.</summary>
    internal static bool IsSucceededDestructive(DeletionErasureClassResult result)
        => string.Equals(result.Status, DeletionErasureClassStatuses.Succeeded, StringComparison.Ordinal) &&
            (string.Equals(result.Action, DeletionErasureClassActions.CryptoShredded, StringComparison.Ordinal) ||
                string.Equals(result.Action, DeletionErasureClassActions.Tombstoned, StringComparison.Ordinal));

    /// <summary>An actionable (destructive) class — anything that is not <c>retained</c>.</summary>
    private static bool IsActionable(DeletionErasureClassResult result)
        => !string.Equals(result.Action, DeletionErasureClassActions.Retained, StringComparison.Ordinal);

    // The pure plan models the proof shape; the deferred destruction runtime (and the Story 9.1 audit-chain seam in
    // DeletionErasureRunner) overwrites SubjectLocator/KeyHandle with the real per-subject confirmations.
    private static ErasureProofEntry ProofEntryFor(DeletionErasureClassResult result)
        => new(
            result.DataClassId,
            $"subject:{result.DataClassId}",
            string.Equals(result.Action, DeletionErasureClassActions.Tombstoned, StringComparison.Ordinal),
            $"kms:{result.DataClassId}",
            string.Equals(result.Action, DeletionErasureClassActions.CryptoShredded, StringComparison.Ordinal));

    private static string RunStatusFor(IReadOnlyList<DeletionErasureClassResult> classResults)
    {
        DeletionErasureClassResult[] actionable = classResults.Where(IsActionable).ToArray();
        if (actionable.Length == 0)
        {
            return DeletionErasureRunStatuses.Completed;
        }

        int succeeded = actionable.Count(static result =>
            string.Equals(result.Status, DeletionErasureClassStatuses.Succeeded, StringComparison.Ordinal));

        if (succeeded == actionable.Length)
        {
            return DeletionErasureRunStatuses.Completed;
        }

        return succeeded == 0 ? DeletionErasureRunStatuses.Failed : DeletionErasureRunStatuses.PartialFailure;
    }

    internal static string ComputeProofFingerprint(IEnumerable<ErasureProofEntry> entries)
    {
        string joined = string.Join(
            '|',
            entries
                .Select(static entry =>
                    $"{entry.DataClassId}:{entry.SubjectLocator}:{(entry.Tombstoned ? '1' : '0')}:{entry.KeyHandle}:{(entry.KeyShredded ? '1' : '0')}")
                .OrderBy(static line => line, StringComparer.Ordinal));
        return Fingerprint($"proof:{joined}");
    }

    private static string Fingerprint(string seed)
        => $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed))).ToLowerInvariant()}";
}
