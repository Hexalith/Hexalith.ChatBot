using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Validation for the tenant-export request spec and run result. Reuses the Story 7.4
/// <see cref="RetentionValidationResult"/> and <see cref="ComplianceAdministrationSchema"/> token helpers — it does
/// NOT introduce a second result type or token validator. Beyond per-field closed-set checks it enforces the AC1
/// eligibility-vs-disposition invariant, the architecture #13 WORM-class invariant, the AC3 no-partial-exposure
/// manifest invariant, and request/result completeness.
/// </summary>
public static class TenantExportSchema
{
    private static readonly IReadOnlySet<string> NonExportableWormClasses =
        new HashSet<string>(
            [ComplianceRetentionClassIds.AuditRecords, ComplianceRetentionClassIds.Backups],
            StringComparer.Ordinal);

    public static RetentionValidationResult ValidateRequestSpec(TenantExportRequestSpec? spec)
    {
        if (spec?.RequestedDataClassIds is not { Count: > 0 } requested ||
            requested.Count > ComplianceRetentionClassIds.All.Count ||
            spec.Scope is null ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(spec.Scope.TenantRef))
        {
            return RetentionValidationResult.Invalid("tenant_export_request_invalid");
        }

        List<string> errors = [];
        HashSet<string> classes = new(StringComparer.Ordinal);
        foreach (string dataClassId in requested)
        {
            if (!ComplianceRetentionClassIds.All.Contains(dataClassId))
            {
                errors.Add("export_class_invalid");
            }
            else if (!classes.Add(dataClassId))
            {
                errors.Add("export_class_duplicate");
            }
        }

        foreach (string projectScopeRef in spec.Scope.ProjectScopeRefs ?? [])
        {
            if (!ComplianceAdministrationSchema.IsSafeComplianceToken(projectScopeRef))
            {
                errors.Add("export_project_ref_invalid");
            }
        }

        return errors.Count == 0
            ? RetentionValidationResult.Valid
            : new RetentionValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    public static RetentionValidationResult ValidateRunResult(
        TenantExportRunResult? result,
        IReadOnlyCollection<string>? requestedDataClassIds = null)
    {
        if (result?.ClassResults is not { Count: > 0 } classResults ||
            !TenantExportRunStatuses.Contains(result.RunStatus) ||
            !ComplianceAdministrationSchema.IsSafeFingerprint(result.ManifestFingerprint) ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(result.ExportRunId) ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(result.CorrelationId) ||
            !ComplianceAdministrationSchema.IsUtc(result.GeneratedAtUtc))
        {
            return RetentionValidationResult.Invalid("tenant_export_result_invalid");
        }

        List<string> errors = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (TenantExportClassResult classResult in classResults)
        {
            ValidateClassResult(classResult, seen, errors);
        }

        // Completeness: every requested class is processed exactly once (duplicates caught above).
        if (requestedDataClassIds is not null)
        {
            foreach (string requested in requestedDataClassIds)
            {
                if (!seen.Contains(requested))
                {
                    errors.Add("export_class_unprocessed");
                }
            }
        }

        // No-partial-exposure manifest invariant: the sealed fingerprint covers exactly the succeeded includable
        // classes — neither more nor fewer.
        string expectedManifest = TenantExportPlanner.ComputeManifestFingerprint(
            classResults.Where(IsSucceededIncludable).Select(static classResult => classResult.DataClassId));
        if (!string.Equals(result.ManifestFingerprint, expectedManifest, StringComparison.Ordinal))
        {
            errors.Add("export_manifest_partial_exposed");
        }

        // Run-status consistency with the per-class statuses.
        if (!string.Equals(result.RunStatus, ExpectedRunStatus(classResults), StringComparison.Ordinal))
        {
            errors.Add("export_run_status_inconsistent");
        }

        return errors.Count == 0
            ? RetentionValidationResult.Valid
            : new RetentionValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static void ValidateClassResult(TenantExportClassResult classResult, HashSet<string> seen, List<string> errors)
    {
        if (classResult is null)
        {
            errors.Add("tenant_export_result_invalid");
            return;
        }

        if (!ComplianceRetentionClassIds.All.Contains(classResult.DataClassId))
        {
            errors.Add("export_class_invalid");
        }
        else if (!seen.Add(classResult.DataClassId))
        {
            errors.Add("export_class_duplicate");
        }

        if (!DataClassExportEligibilities.Contains(classResult.ExportEligibility))
        {
            errors.Add("export_eligibility_invalid");
        }

        if (!TenantExportClassDispositions.Contains(classResult.Disposition))
        {
            errors.Add("export_disposition_invalid");
        }

        if (!TenantExportRedactionDecisions.Contains(classResult.RedactionDecision))
        {
            errors.Add("export_redaction_decision_invalid");
        }

        if (!TenantExportClassStatuses.Contains(classResult.Status))
        {
            errors.Add("export_status_invalid");
        }

        bool excluded = string.Equals(classResult.Disposition, TenantExportClassDispositions.Excluded, StringComparison.Ordinal);
        if (excluded)
        {
            if (!TenantExportExclusionReasons.Contains(classResult.ExclusionReason))
            {
                errors.Add("export_exclusion_reason_invalid");
            }
        }
        else if (!string.IsNullOrEmpty(classResult.ExclusionReason))
        {
            errors.Add("export_exclusion_reason_invalid");
        }

        // Eligibility-vs-disposition invariant: a not-exportable class must be excluded with reason not-exportable.
        if (string.Equals(classResult.ExportEligibility, DataClassExportEligibilities.NotExportable, StringComparison.Ordinal) &&
            (!excluded || !string.Equals(classResult.ExclusionReason, TenantExportExclusionReasons.NotExportable, StringComparison.Ordinal)))
        {
            errors.Add("export_eligibility_disposition_mismatch");
        }

        // Architecture #13 WORM: audit-records / backups may never be included or redacted.
        if (NonExportableWormClasses.Contains(classResult.DataClassId) &&
            !string.Equals(classResult.Disposition, TenantExportClassDispositions.Excluded, StringComparison.Ordinal))
        {
            errors.Add("export_worm_class_exposed");
        }

        // No-partial-exposure: only a succeeded includable class carries an artifact fingerprint.
        if (IsSucceededIncludable(classResult))
        {
            if (!ComplianceAdministrationSchema.IsSafeFingerprint(classResult.ArtifactFingerprint))
            {
                errors.Add("export_manifest_partial_exposed");
            }
        }
        else if (!string.IsNullOrEmpty(classResult.ArtifactFingerprint))
        {
            errors.Add("export_manifest_partial_exposed");
        }
    }

    private static bool IsSucceededIncludable(TenantExportClassResult classResult)
        => string.Equals(classResult.Status, TenantExportClassStatuses.Succeeded, StringComparison.Ordinal) &&
            (string.Equals(classResult.Disposition, TenantExportClassDispositions.Included, StringComparison.Ordinal) ||
                string.Equals(classResult.Disposition, TenantExportClassDispositions.Redacted, StringComparison.Ordinal));

    private static string ExpectedRunStatus(IReadOnlyList<TenantExportClassResult> classResults)
    {
        TenantExportClassResult[] includable = classResults
            .Where(static classResult =>
                string.Equals(classResult.Disposition, TenantExportClassDispositions.Included, StringComparison.Ordinal) ||
                string.Equals(classResult.Disposition, TenantExportClassDispositions.Redacted, StringComparison.Ordinal))
            .ToArray();

        if (includable.Length == 0)
        {
            return TenantExportRunStatuses.Completed;
        }

        int succeeded = includable.Count(static classResult =>
            string.Equals(classResult.Status, TenantExportClassStatuses.Succeeded, StringComparison.Ordinal));

        if (succeeded == includable.Length)
        {
            return TenantExportRunStatuses.Completed;
        }

        return succeeded == 0 ? TenantExportRunStatuses.Failed : TenantExportRunStatuses.PartialFailure;
    }
}
