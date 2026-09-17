using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Validation for the consent/lawful-basis record and the requirement profile (AC1/AC2/AC3). Reuses the Story 7.4
/// <see cref="RetentionValidationResult"/> and <see cref="ComplianceAdministrationSchema"/> token helpers (plus
/// <c>AuditMetadata.IsSafeStableIdentifier</c> via the gateway validator) — it does NOT introduce a second result type
/// or token validator. The profile check enforces a bijection over <see cref="ConsentSubjectKinds.All"/> (every
/// subject kind declared exactly once), mirroring the Story 9.7 inventory-completeness invariant.
/// </summary>
public static class ConsentLawfulBasisSchema
{
    public static RetentionValidationResult ValidateRecord(ConsentLawfulBasisRecord? record)
    {
        if (record is null)
        {
            return RetentionValidationResult.Invalid("consent_record_invalid");
        }

        List<string> errors = [];

        if (!ConsentSubjectKinds.Contains(record.SubjectKind))
        {
            errors.Add("consent_subject_kind_invalid");
        }

        if (!ConsentLawfulBases.Contains(record.LawfulBasis))
        {
            errors.Add("consent_lawful_basis_invalid");
        }

        if (!ConsentRecordStatuses.Contains(record.RecordStatus))
        {
            errors.Add("consent_record_status_invalid");
        }

        if (!DataClassRedactionSensitivities.Contains(record.RedactionSensitivity))
        {
            errors.Add("consent_redaction_sensitivity_invalid");
        }

        if (!ComplianceAdministrationSchema.IsSafeComplianceToken(record.RecordId))
        {
            errors.Add("consent_record_id_invalid");
        }

        // The subject locator is an opaque safe stable identifier — never raw PII.
        if (!ComplianceAdministrationSchema.IsSafeComplianceToken(record.SubjectLocator))
        {
            errors.Add("consent_subject_locator_invalid");
        }

        if (!ComplianceAdministrationSchema.IsSafeComplianceToken(record.ProjectScopeRef))
        {
            errors.Add("consent_project_scope_ref_invalid");
        }

        if (!ComplianceAdministrationSchema.IsSafeComplianceToken(record.BasisSource))
        {
            errors.Add("consent_basis_source_invalid");
        }

        if (!ComplianceAdministrationSchema.IsSafeFingerprint(record.RecordFingerprint))
        {
            errors.Add("consent_record_fingerprint_invalid");
        }

        if (!ComplianceAdministrationSchema.IsUtc(record.RecordedAtUtc))
        {
            errors.Add("consent_recorded_at_invalid");
        }

        return errors.Count == 0
            ? RetentionValidationResult.Valid
            : new RetentionValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    public static RetentionValidationResult ValidateRequirementProfile(ConsentRequirementProfile? profile)
    {
        if (profile?.DispositionsBySubjectKind is not { Count: > 0 } dispositions)
        {
            return RetentionValidationResult.Invalid("consent_requirement_profile_invalid");
        }

        List<string> errors = [];
        foreach (KeyValuePair<string, string> entry in dispositions)
        {
            if (!ConsentSubjectKinds.Contains(entry.Key))
            {
                errors.Add("consent_requirement_subject_kind_invalid");
            }

            if (!ConsentRequirementDispositions.Contains(entry.Value))
            {
                errors.Add("consent_requirement_disposition_invalid");
            }
        }

        // Completeness (bijection): every subject kind is declared exactly once — none left undeclared.
        foreach (string subjectKind in ConsentSubjectKinds.All)
        {
            if (!dispositions.ContainsKey(subjectKind))
            {
                errors.Add("consent_requirement_profile_incomplete");
            }
        }

        return errors.Count == 0
            ? RetentionValidationResult.Valid
            : new RetentionValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }
}
