using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The pure tenant-export decision engine (AC1/AC2/AC3). Reads the Story 9.7 <see cref="DataClassInventory"/> as the
/// single source of truth for export eligibility and redaction sensitivity — it never forks a second eligibility,
/// class-id, or sensitivity set. Authority is supplied pre-bounded as a <see cref="TenantExportAuthorityView"/> so
/// the function has no <c>ClaimsPrincipal</c> dependency and the no-leak boundary holds.
/// </summary>
public static class TenantExportPlanner
{
    public static TenantExportRunResult Plan(
        DataClassInventory inventory,
        TenantExportRequestSpec spec,
        TenantExportAuthorityView authority,
        string exportRunId,
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

        List<TenantExportClassResult> classResults = [];
        foreach (string dataClassId in spec.RequestedDataClassIds)
        {
            classResults.Add(PlanClass(byId.GetValueOrDefault(dataClassId), dataClassId, unauthorizedScope));
        }

        string manifestFingerprint = ComputeManifestFingerprint(
            classResults
                .Where(IsSucceededIncludable)
                .Select(static result => result.DataClassId));

        return new TenantExportRunResult(
            exportRunId,
            RunStatusFor(classResults),
            manifestFingerprint,
            classResults,
            generatedAtUtc.ToUniversalTime(),
            correlationId);
    }

    private static TenantExportClassResult PlanClass(
        DataClassClassification? classification,
        string dataClassId,
        bool unauthorizedScope)
    {
        // Fail-closed: an unclassifiable class cannot be exported.
        string eligibility = classification?.ExportEligibility ?? DataClassExportEligibilities.NotExportable;
        string ownerRole = classification?.OwnerRole ?? AdminRoles.ComplianceAdmin;
        string sensitivity = classification?.RedactionSensitivity ?? DataClassRedactionSensitivities.Restricted;

        // 1) Eligibility is absolute (architecture #13 WORM): a not-exportable class is always excluded/not-exportable,
        //    regardless of authority — so a not-exportable class never carries the `unauthorized` reason.
        if (string.Equals(eligibility, DataClassExportEligibilities.NotExportable, StringComparison.Ordinal))
        {
            return Excluded(dataClassId, eligibility, ownerRole, TenantExportExclusionReasons.NotExportable);
        }

        // 2) Authority gates exportable/redacted-export classes. On a missing project grant the class is
        //    excluded/unauthorized and carries no resource identity (NFR2).
        if (unauthorizedScope)
        {
            return Excluded(dataClassId, eligibility, ownerRole, TenantExportExclusionReasons.Unauthorized);
        }

        // 3) Eligibility → disposition for the authorized, exportable classes.
        bool exportable = string.Equals(eligibility, DataClassExportEligibilities.Exportable, StringComparison.Ordinal);
        string disposition = exportable ? TenantExportClassDispositions.Included : TenantExportClassDispositions.Redacted;
        string redactionDecision = exportable
            ? TenantExportRedactionDecisions.None
            : string.Equals(sensitivity, DataClassRedactionSensitivities.MetadataOnly, StringComparison.Ordinal)
                ? TenantExportRedactionDecisions.MetadataOnly
                : TenantExportRedactionDecisions.Redacted;

        return new TenantExportClassResult(
            dataClassId,
            eligibility,
            disposition,
            string.Empty,
            redactionDecision,
            TenantExportClassStatuses.Succeeded,
            ownerRole,
            ArtifactFingerprint(dataClassId));
    }

    private static TenantExportClassResult Excluded(
        string dataClassId,
        string eligibility,
        string ownerRole,
        string exclusionReason)
        => new(
            dataClassId,
            eligibility,
            TenantExportClassDispositions.Excluded,
            exclusionReason,
            TenantExportRedactionDecisions.MetadataOnly,
            TenantExportClassStatuses.Succeeded,
            ownerRole,
            string.Empty);

    private static bool IsSucceededIncludable(TenantExportClassResult result)
        => string.Equals(result.Status, TenantExportClassStatuses.Succeeded, StringComparison.Ordinal) &&
            (string.Equals(result.Disposition, TenantExportClassDispositions.Included, StringComparison.Ordinal) ||
                string.Equals(result.Disposition, TenantExportClassDispositions.Redacted, StringComparison.Ordinal));

    private static string RunStatusFor(IReadOnlyList<TenantExportClassResult> classResults)
    {
        TenantExportClassResult[] includable = classResults
            .Where(static result =>
                string.Equals(result.Disposition, TenantExportClassDispositions.Included, StringComparison.Ordinal) ||
                string.Equals(result.Disposition, TenantExportClassDispositions.Redacted, StringComparison.Ordinal))
            .ToArray();

        if (includable.Length == 0)
        {
            return TenantExportRunStatuses.Completed;
        }

        int succeeded = includable.Count(static result =>
            string.Equals(result.Status, TenantExportClassStatuses.Succeeded, StringComparison.Ordinal));

        if (succeeded == includable.Length)
        {
            return TenantExportRunStatuses.Completed;
        }

        return succeeded == 0 ? TenantExportRunStatuses.Failed : TenantExportRunStatuses.PartialFailure;
    }

    internal static string ComputeManifestFingerprint(IEnumerable<string> includableClassIds)
    {
        string joined = string.Join('|', includableClassIds.OrderBy(static id => id, StringComparer.Ordinal));
        return Fingerprint($"manifest:{joined}");
    }

    private static string ArtifactFingerprint(string dataClassId)
        => Fingerprint($"artifact:{dataClassId}");

    private static string Fingerprint(string seed)
        => $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(seed))).ToLowerInvariant()}";
}
