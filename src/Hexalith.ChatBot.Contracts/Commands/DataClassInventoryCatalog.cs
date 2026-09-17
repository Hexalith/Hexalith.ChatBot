using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The as-shipped seed v1 data-class inventory (AC4) classifying EVERY member of the extended
/// <see cref="ComplianceRetentionClassIds.All"/>. Immutable, deterministic, token-only — mirrors
/// <c>OperatingBaselineCatalog.Published</c> (no <c>UtcNow</c>; a fixed seed review date). The live S-tagged
/// Data Governance editor surface and the storage-layer retention/deletion enforcement (Stories 9.8/9.9) consume
/// this catalog; they do not redefine it.
/// </summary>
public static class DataClassInventoryCatalog
{
    /// <summary>The fixed seed last-reviewed date (deterministic; the quarterly-review clock starts here).</summary>
    public static readonly DateTimeOffset SeedLastReviewedAtUtc = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    public static DataClassInventory Published { get; } = new(
        Owner: AdminRoles.ComplianceAdmin,
        Version: "data-class-inventory-v1",
        LastReviewedAtUtc: SeedLastReviewedAtUtc,
        SchemaVersion: DataClassInventorySchemaVersions.V1,
        Classifications:
        [
            // Mailbox-owned source classes: restricted content, key-shred erasure, mailbox-admin owner.
            Classification(
                ComplianceRetentionClassIds.SourceEmailMetadata, AdminRoles.MailboxAdmin,
                DataClassRedactionSensitivities.Restricted, DataClassDeletionBehaviors.KeyShred,
                DataClassExportEligibilities.RedactedExport, "minimize:authorized-workflow-need"),
            Classification(
                ComplianceRetentionClassIds.Attachments, AdminRoles.MailboxAdmin,
                DataClassRedactionSensitivities.Restricted, DataClassDeletionBehaviors.KeyShred,
                DataClassExportEligibilities.RedactedExport, "minimize:authorized-workflow-need"),

            // Derived projections: metadata-only stamps tombstoned on erasure.
            Classification(
                ComplianceRetentionClassIds.AssociationRecords, AdminRoles.ComplianceAdmin,
                DataClassRedactionSensitivities.Internal, DataClassDeletionBehaviors.ProjectionTombstone,
                DataClassExportEligibilities.RedactedExport, "minimize:association-need"),
            Classification(
                ComplianceRetentionClassIds.EvidenceSnapshots, AdminRoles.ComplianceAdmin,
                DataClassRedactionSensitivities.Sensitive, DataClassDeletionBehaviors.ProjectionTombstone,
                DataClassExportEligibilities.RedactedExport, "minimize:evidence-need"),
            Classification(
                ComplianceRetentionClassIds.ApprovalRecords, AdminRoles.ComplianceAdmin,
                DataClassRedactionSensitivities.Internal, DataClassDeletionBehaviors.ProjectionTombstone,
                DataClassExportEligibilities.RedactedExport, "minimize:approval-need"),
            Classification(
                ComplianceRetentionClassIds.LifecycleState, AdminRoles.ComplianceAdmin,
                DataClassRedactionSensitivities.Internal, DataClassDeletionBehaviors.ProjectionTombstone,
                DataClassExportEligibilities.RedactedExport, "minimize:lifecycle-need"),
            Classification(
                ComplianceRetentionClassIds.WorkflowLinkMaps, AdminRoles.ComplianceAdmin,
                DataClassRedactionSensitivities.Internal, DataClassDeletionBehaviors.ProjectionTombstone,
                DataClassExportEligibilities.RedactedExport, "minimize:workflow-link-need"),

            // Policy snapshots: internal config history, tombstoned (superseded, not destroyed).
            Classification(
                ComplianceRetentionClassIds.PolicySnapshots, AdminRoles.ComplianceAdmin,
                DataClassRedactionSensitivities.Internal, DataClassDeletionBehaviors.ProjectionTombstone,
                DataClassExportEligibilities.RedactedExport, "minimize:policy-snapshot-need"),

            // AI prompts/outputs/context: restricted content, key-shred erasure.
            Classification(
                ComplianceRetentionClassIds.AiPromptsOutputsContext, AdminRoles.ComplianceAdmin,
                DataClassRedactionSensitivities.Restricted, DataClassDeletionBehaviors.KeyShred,
                DataClassExportEligibilities.RedactedExport, "minimize:ai-context-need"),

            // Logs/support bundles: metadata-only, key-shred.
            Classification(
                ComplianceRetentionClassIds.LogsSupportBundles, AdminRoles.ComplianceAdmin,
                DataClassRedactionSensitivities.MetadataOnly, DataClassDeletionBehaviors.KeyShred,
                DataClassExportEligibilities.RedactedExport, "minimize:support-bundle-need"),

            // Audit records: WORM — retain-immutable, never hard-delete, never exportable (architecture #13).
            Classification(
                ComplianceRetentionClassIds.AuditRecords, AdminRoles.ComplianceAdmin,
                DataClassRedactionSensitivities.Internal, DataClassDeletionBehaviors.RetainImmutable,
                DataClassExportEligibilities.NotExportable, "minimize:audit-need"),

            // Backups: metadata-only governance stamp, key-shred erasure (NFR53).
            Classification(
                ComplianceRetentionClassIds.Backups, AdminRoles.ComplianceAdmin,
                DataClassRedactionSensitivities.MetadataOnly, DataClassDeletionBehaviors.KeyShred,
                DataClassExportEligibilities.NotExportable, "minimize:backup-need"),

            // Evaluation datasets: redacted-export, projection-tombstone erasure (NFR53).
            Classification(
                ComplianceRetentionClassIds.EvaluationDatasets, AdminRoles.ComplianceAdmin,
                DataClassRedactionSensitivities.Sensitive, DataClassDeletionBehaviors.ProjectionTombstone,
                DataClassExportEligibilities.RedactedExport, "minimize:evaluation-need"),
        ]);

    private static DataClassClassification Classification(
        string dataClassId,
        string ownerRole,
        string redactionSensitivity,
        string deletionBehavior,
        string exportEligibility,
        string minimizationRuleRef)
        => new(dataClassId, ownerRole, dataClassId, redactionSensitivity, deletionBehavior, exportEligibility, minimizationRuleRef);
}
