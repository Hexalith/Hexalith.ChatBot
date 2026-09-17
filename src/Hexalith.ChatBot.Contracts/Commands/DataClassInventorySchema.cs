using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Validation for the data-class inventory and its change set. Reuses the Story 7.4
/// <see cref="RetentionValidationResult"/> and <see cref="ComplianceAdministrationSchema"/> token helpers — it does
/// NOT introduce a second result type or token validator. Beyond per-field safe-token/closed-set checks it enforces
/// the AC4 completeness invariant (the classification set is a bijection over <see cref="ComplianceRetentionClassIds.All"/>)
/// and the architecture cross-cutting #13 constraint (<c>audit-records</c> may never be <c>hard-delete</c>).
/// </summary>
public static class DataClassInventorySchema
{
    public static RetentionValidationResult ValidateChangeSet(DataClassInventoryChangeSet? changeSet)
        => ValidateClassifications(changeSet?.Classifications);

    public static RetentionValidationResult Validate(DataClassInventory? inventory)
    {
        if (inventory is null ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(inventory.Owner) ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(inventory.Version) ||
            !DataClassInventorySchemaVersions.IsKnown(inventory.SchemaVersion) ||
            !ComplianceAdministrationSchema.IsUtc(inventory.LastReviewedAtUtc))
        {
            return RetentionValidationResult.Invalid("data_class_inventory_invalid");
        }

        return ValidateClassifications(inventory.Classifications);
    }

    private static RetentionValidationResult ValidateClassifications(IReadOnlyList<DataClassClassification>? classifications)
    {
        if (classifications is not { Count: > 0 } items || items.Count > ComplianceRetentionClassIds.All.Count)
        {
            return RetentionValidationResult.Invalid("data_class_inventory_invalid");
        }

        List<string> errors = [];
        HashSet<string> classes = new(StringComparer.Ordinal);
        foreach (DataClassClassification classification in items)
        {
            if (classification is null)
            {
                errors.Add("data_class_inventory_invalid");
                continue;
            }

            if (!ComplianceRetentionClassIds.All.Contains(classification.DataClassId))
            {
                errors.Add("data_class_invalid");
            }
            else if (!classes.Add(classification.DataClassId))
            {
                errors.Add("data_class_duplicate");
            }

            if (!AdminRoles.TryFromWireValue(classification.OwnerRole, out _))
            {
                errors.Add("owner_role_invalid");
            }

            if (!ComplianceRetentionClassIds.All.Contains(classification.RetentionClassId))
            {
                errors.Add("retention_class_invalid");
            }

            if (!DataClassRedactionSensitivities.Contains(classification.RedactionSensitivity))
            {
                errors.Add("redaction_sensitivity_invalid");
            }

            if (!DataClassDeletionBehaviors.Contains(classification.DeletionBehavior))
            {
                errors.Add("deletion_behavior_invalid");
            }

            if (!DataClassExportEligibilities.Contains(classification.ExportEligibility))
            {
                errors.Add("export_eligibility_invalid");
            }

            if (!ComplianceAdministrationSchema.IsSafeComplianceToken(classification.MinimizationRuleRef))
            {
                errors.Add("minimization_rule_invalid");
            }

            // Architecture cross-cutting #13 (WORM-vs-erasure): the immutable audit chain is never hard-deleted.
            if (string.Equals(classification.DataClassId, ComplianceRetentionClassIds.AuditRecords, StringComparison.Ordinal) &&
                !string.Equals(classification.DeletionBehavior, DataClassDeletionBehaviors.RetainImmutable, StringComparison.Ordinal) &&
                !string.Equals(classification.DeletionBehavior, DataClassDeletionBehaviors.ProjectionTombstone, StringComparison.Ordinal))
            {
                errors.Add("audit_class_deletion_invalid");
            }
        }

        // AC4 completeness: every canonical data class is classified exactly once — none unclassified.
        foreach (string dataClassId in ComplianceRetentionClassIds.All)
        {
            if (!classes.Contains(dataClassId))
            {
                errors.Add("data_class_unclassified");
            }
        }

        return errors.Count == 0
            ? RetentionValidationResult.Valid
            : new RetentionValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }
}
