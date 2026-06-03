using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The schema versions for the Story 9.7 data-class inventory artifact and its governed change command. Mirrors
/// <see cref="ComplianceAdministrationSchemaVersions"/> — a closed, ordinal set with a known-membership check.
/// </summary>
public static class DataClassInventorySchemaVersions
{
    public const string V1 = "data-class-inventory-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// The closed redaction-sensitivity dimension (NFR52, architecture cross-cutting #7). Tenants/editors may select
/// within this bounded set but never invent members. Mirrors the <see cref="ComplianceRetentionClassIds"/> shape.
/// </summary>
public static class DataClassRedactionSensitivities
{
    public const string Restricted = "restricted";
    public const string Sensitive = "sensitive";
    public const string Internal = "internal";
    public const string MetadataOnly = "metadata-only";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Restricted, Sensitive, Internal, MetadataOnly], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed deletion-behavior dimension (architecture cross-cutting #13, WORM-vs-erasure). <c>audit-records</c>
/// must never be <see cref="HardDelete"/> — erasure over the immutable chain is projection-tombstone + key-shred.
/// </summary>
public static class DataClassDeletionBehaviors
{
    public const string KeyShred = "key-shred";
    public const string ProjectionTombstone = "projection-tombstone";
    public const string HardDelete = "hard-delete";
    public const string RetainImmutable = "retain-immutable";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([KeyShred, ProjectionTombstone, HardDelete, RetainImmutable], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed export-eligibility dimension (NFR52, Story 9.8 export consumes it).
/// </summary>
public static class DataClassExportEligibilities
{
    public const string Exportable = "exportable";
    public const string RedactedExport = "redacted-export";
    public const string NotExportable = "not-exportable";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Exportable, RedactedExport, NotExportable], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The full classification tuple for a single ChatBot-owned data class (NFR52/NFR53). Every field is a bounded,
/// <c>AuditMetadata</c>-safe token. <see cref="DataClassId"/> and <see cref="RetentionClassId"/> reference the one
/// canonical <see cref="ComplianceRetentionClassIds"/> spine; <see cref="OwnerRole"/> is an <c>AdminRoles</c> wire
/// token; the three sensitivity/behavior/eligibility dimensions are closed sets; <see cref="MinimizationRuleRef"/>
/// is a safe compliance token describing the NFR52 minimization constraint (never raw content).
/// </summary>
public sealed record DataClassClassification(
    string DataClassId,
    string OwnerRole,
    string RetentionClassId,
    string RedactionSensitivity,
    string DeletionBehavior,
    string ExportEligibility,
    string MinimizationRuleRef);

/// <summary>
/// The versioned data-class inventory artifact (NFR23/NFR53). Carries <see cref="Owner"/>, <see cref="Version"/>,
/// <see cref="LastReviewedAtUtc"/> (quarterly-review obligation), <see cref="SchemaVersion"/>, and the complete
/// classification set — which <see cref="DataClassInventorySchema.Validate"/> asserts is a bijection over the
/// canonical class set (every class classified exactly once, none unclassified).
/// </summary>
public sealed record DataClassInventory(
    string Owner,
    string Version,
    DateTimeOffset LastReviewedAtUtc,
    string SchemaVersion,
    IReadOnlyList<DataClassClassification> Classifications);

/// <summary>
/// The NFR35 policy-snapshot metadata for an inventory change. Mirrors <see cref="RetentionSnapshotMetadata"/>
/// field-for-field, replacing <c>ChangedRetentionClassIds</c> with <see cref="ChangedDataClassIds"/> and using
/// <see cref="AdminScope.Compliance"/> for <see cref="ScopeUsed"/>. Old/new values are <c>sha256:</c> fingerprints,
/// never raw inventory values.
/// </summary>
public sealed record DataClassInventorySnapshotMetadata(
    string SnapshotId,
    string SchemaVersion,
    string SupersedesSnapshotId,
    string SupersededBySnapshotId,
    string SourceChangeId,
    string ActorRef,
    AdminScope ScopeUsed,
    IReadOnlyList<string> ChangedDataClassIds,
    long SourceVersion,
    DateTimeOffset EffectiveAtUtc,
    string CorrelationId,
    string ReasonCode,
    string PolicySnapshotId,
    string OldSnapshotFingerprint,
    string NewSnapshotFingerprint);

/// <summary>
/// The proposed classification set for a <see cref="SubmitDataClassInventoryChange"/>. Mirrors
/// <see cref="RetentionConfigurationChangeSet"/>.
/// </summary>
public sealed record DataClassInventoryChangeSet(
    IReadOnlyList<DataClassClassification> Classifications);

/// <summary>
/// The compliance-admin-gated governed command that edits the data-class inventory (AC2/AC3). A structural twin of
/// <see cref="SubmitRetentionConfigurationChange"/> — gated at the <c>ParticipantAuthorizationStage</c> by
/// <c>HasHumanAdminScope(.., AdminScope.Compliance)</c>, routed through the one CommandGateway audit-commit spine,
/// fail-closed with no durable write on unauthorized scope / invalid command / audit-writer-down.
/// </summary>
public sealed record SubmitDataClassInventoryChange(
    string InventoryChangeId,
    string SourceInventorySnapshotId,
    string ProposedInventorySnapshotId,
    long SourceVersion,
    DataClassInventoryChangeSet ChangeSet,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string PolicySnapshotId,
    string OldInventorySnapshotFingerprint,
    string NewInventorySnapshotFingerprint,
    DateTimeOffset EffectiveAtUtc) : IChatBotCommand;

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
