using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Story 9.7 contracts coverage: the closed classification dimensions, the inventory/change-set schema with its
/// completeness + WORM-vs-erasure invariants, the seed catalog bijection, and the two new canonical class ids.
/// Mirrors the <c>AdminContractTests</c> style (round-trips + closed-set membership + Shouldly).
/// </summary>
public static class DataClassInventoryContractTests
{
    [Theory]
    [InlineData(ComplianceRetentionClassIds.Backups, "backups")]
    [InlineData(ComplianceRetentionClassIds.EvaluationDatasets, "evaluation-datasets")]
    public static void NewRetentionClassIdsShouldRoundTripAndBeMembers(string constant, string wire)
    {
        constant.ShouldBe(wire);
        ComplianceRetentionClassIds.All.ShouldContain(wire);
    }

    [Fact]
    public static void ExtendedRetentionClassSetShouldStayDefineOnceAndBoundRetentionWindows()
    {
        // The set is the single canonical spine; the two new members lift the count to 13 (keyed off .Count, never
        // a literal). A retention window for either new member validates (neither is audit-records).
        ComplianceRetentionClassIds.All.Count.ShouldBe(13);
        ComplianceAdministrationSchema.ValidateRetentionChangeSet(new RetentionConfigurationChangeSet(
        [
            new RetentionWindow(ComplianceRetentionClassIds.Backups, "backups-window", 365),
            new RetentionWindow(ComplianceRetentionClassIds.EvaluationDatasets, "evaluation-datasets-window", 365),
        ])).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(DataClassRedactionSensitivities.Restricted)]
    [InlineData(DataClassRedactionSensitivities.Sensitive)]
    [InlineData(DataClassRedactionSensitivities.Internal)]
    [InlineData(DataClassRedactionSensitivities.MetadataOnly)]
    public static void RedactionSensitivitiesShouldBeAClosedSet(string value)
    {
        DataClassRedactionSensitivities.Contains(value).ShouldBeTrue();
        DataClassRedactionSensitivities.Contains("public").ShouldBeFalse();
        DataClassRedactionSensitivities.Contains(null).ShouldBeFalse();
    }

    [Theory]
    [InlineData(DataClassDeletionBehaviors.KeyShred)]
    [InlineData(DataClassDeletionBehaviors.ProjectionTombstone)]
    [InlineData(DataClassDeletionBehaviors.HardDelete)]
    [InlineData(DataClassDeletionBehaviors.RetainImmutable)]
    public static void DeletionBehaviorsShouldBeAClosedSet(string value)
    {
        DataClassDeletionBehaviors.Contains(value).ShouldBeTrue();
        DataClassDeletionBehaviors.Contains("purge").ShouldBeFalse();
    }

    [Theory]
    [InlineData(DataClassExportEligibilities.Exportable)]
    [InlineData(DataClassExportEligibilities.RedactedExport)]
    [InlineData(DataClassExportEligibilities.NotExportable)]
    public static void ExportEligibilitiesShouldBeAClosedSet(string value)
    {
        DataClassExportEligibilities.Contains(value).ShouldBeTrue();
        DataClassExportEligibilities.Contains("anything-goes").ShouldBeFalse();
    }

    [Fact]
    public static void SeedCatalogShouldBeValidAndBijectiveOverTheCanonicalClassSet()
    {
        DataClassInventorySchema.Validate(DataClassInventoryCatalog.Published).IsValid.ShouldBeTrue();

        // AC4 completeness: every canonical class is classified exactly once.
        IReadOnlyList<DataClassClassification> classifications = DataClassInventoryCatalog.Published.Classifications;
        classifications.Count.ShouldBe(ComplianceRetentionClassIds.All.Count);
        classifications
            .Select(static classification => classification.DataClassId)
            .ToHashSet(StringComparer.Ordinal)
            .ShouldBe(ComplianceRetentionClassIds.All.ToHashSet(StringComparer.Ordinal), ignoreOrder: true);

        // The seed audit-records class never permits hard-delete (architecture #13).
        DataClassClassification auditRecords = classifications.Single(static classification =>
            string.Equals(classification.DataClassId, ComplianceRetentionClassIds.AuditRecords, StringComparison.Ordinal));
        auditRecords.DeletionBehavior.ShouldBe(DataClassDeletionBehaviors.RetainImmutable);
        auditRecords.ExportEligibility.ShouldBe(DataClassExportEligibilities.NotExportable);
    }

    [Fact]
    public static void SchemaShouldRejectMissingDuplicateUnknownAndHardDeletedAuditClasses()
    {
        IReadOnlyList<DataClassClassification> seed = DataClassInventoryCatalog.Published.Classifications;

        // (a) a missing class — drop the first classification.
        DataClassInventorySchema.ValidateChangeSet(new DataClassInventoryChangeSet([.. seed.Skip(1)]))
            .Errors.ShouldContain("data_class_unclassified");

        // (b) a duplicate class — repeat the first classification (drop the last to stay within the count bound).
        DataClassInventorySchema.ValidateChangeSet(new DataClassInventoryChangeSet([seed[0], .. seed.Skip(1).Take(seed.Count - 2), seed[0]]))
            .Errors.ShouldContain("data_class_duplicate");

        // (c) an unknown dimension token.
        DataClassInventorySchema.ValidateChangeSet(new DataClassInventoryChangeSet(
                [seed[0] with { RedactionSensitivity = "public" }, .. seed.Skip(1)]))
            .Errors.ShouldContain("redaction_sensitivity_invalid");

        // (d) audit-records with hard-delete.
        DataClassInventorySchema.ValidateChangeSet(new DataClassInventoryChangeSet(
                [.. seed.Select(classification =>
                    string.Equals(classification.DataClassId, ComplianceRetentionClassIds.AuditRecords, StringComparison.Ordinal)
                        ? classification with { DeletionBehavior = DataClassDeletionBehaviors.HardDelete }
                        : classification)]))
            .Errors.ShouldContain("audit_class_deletion_invalid");
    }

    [Fact]
    public static void SchemaShouldRejectEachInvalidClassificationDimension()
    {
        // AC1: each closed-set / safe-token dimension has its own rejection branch. Flip exactly one field on a
        // single non-audit class (source-email-metadata) so completeness stays satisfied and only the targeted
        // error code surfaces — every dimension's validator branch is exercised independently.
        IReadOnlyList<DataClassClassification> seed = DataClassInventoryCatalog.Published.Classifications;

        RejectWithFirstClassMutation(seed, classification => classification with { OwnerRole = "not-a-real-role" })
            .ShouldContain("owner_role_invalid");
        RejectWithFirstClassMutation(seed, classification => classification with { RetentionClassId = "bogus-class" })
            .ShouldContain("retention_class_invalid");
        RejectWithFirstClassMutation(seed, classification => classification with { DeletionBehavior = "purge" })
            .ShouldContain("deletion_behavior_invalid");
        RejectWithFirstClassMutation(seed, classification => classification with { ExportEligibility = "anything-goes" })
            .ShouldContain("export_eligibility_invalid");
        RejectWithFirstClassMutation(seed, classification => classification with { MinimizationRuleRef = "unsafe rule!" })
            .ShouldContain("minimization_rule_invalid");
    }

    [Fact]
    public static void ValidateChangeSetShouldRejectNullAndEmptyClassificationSets()
    {
        // AC1/AC4: a null or empty change set is structurally invalid before per-class checks run.
        DataClassInventorySchema.ValidateChangeSet(null).IsValid.ShouldBeFalse();
        DataClassInventorySchema.ValidateChangeSet(null).Errors.ShouldContain("data_class_inventory_invalid");
        DataClassInventorySchema.ValidateChangeSet(new DataClassInventoryChangeSet([]))
            .Errors.ShouldContain("data_class_inventory_invalid");
    }

    [Fact]
    public static void SeedCatalogShouldExposeTheVersionedArtifactHeader()
    {
        // AC4: the inventory is a versioned artifact carrying owner, version, last-reviewed date, and schema version.
        DataClassInventory published = DataClassInventoryCatalog.Published;
        published.Owner.ShouldBe(AdminRoles.ComplianceAdmin);
        published.Version.ShouldBe("data-class-inventory-v1");
        published.SchemaVersion.ShouldBe(DataClassInventorySchemaVersions.V1);
        published.LastReviewedAtUtc.ShouldBe(DataClassInventoryCatalog.SeedLastReviewedAtUtc);
        published.LastReviewedAtUtc.Offset.ShouldBe(TimeSpan.Zero); // quarterly-review clock is UTC.
    }

    [Fact]
    public static void ValidateShouldRejectAMalformedVersionedArtifactHeader()
    {
        // AC4: Validate(inventory) guards the artifact header (owner/version/schema/last-reviewed) before the
        // classification bijection — a classification set that is itself complete must still fail on a bad header.
        DataClassInventory seed = DataClassInventoryCatalog.Published;

        DataClassInventorySchema.Validate(null).Errors.ShouldContain("data_class_inventory_invalid");
        DataClassInventorySchema.Validate(seed with { Owner = "unsafe owner!" })
            .Errors.ShouldContain("data_class_inventory_invalid");
        DataClassInventorySchema.Validate(seed with { Version = "unsafe version!" })
            .Errors.ShouldContain("data_class_inventory_invalid");
        DataClassInventorySchema.Validate(seed with { SchemaVersion = "data-class-inventory-schema.v999" })
            .Errors.ShouldContain("data_class_inventory_invalid");
        DataClassInventorySchema.Validate(seed with
        {
            LastReviewedAtUtc = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.FromHours(2)),
        }).Errors.ShouldContain("data_class_inventory_invalid");
    }

    private static IReadOnlyList<string> RejectWithFirstClassMutation(
        IReadOnlyList<DataClassClassification> seed,
        Func<DataClassClassification, DataClassClassification> mutate)
    {
        RetentionValidationResult result = DataClassInventorySchema.ValidateChangeSet(
            new DataClassInventoryChangeSet([mutate(seed[0]), .. seed.Skip(1)]));
        result.IsValid.ShouldBeFalse();
        return result.Errors;
    }

    [Fact]
    public static void SubmitInventoryChangeShouldSerializeMetadataOnlyTokensAndFingerprints()
    {
        SubmitDataClassInventoryChange change = InventoryChange();
        DataClassInventorySnapshotMetadata snapshot = new(
            "inventory-snapshot-proposed",
            DataClassInventorySchemaVersions.V1,
            "inventory-snapshot-current",
            "inventory-snapshot-next",
            "inventory-change-001",
            "admin-requester",
            AdminScope.Compliance,
            [ComplianceRetentionClassIds.AuditRecords],
            8,
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "data-class-inventory-update",
            "policy-snapshot-admin-v1",
            "sha256:oldinventoryfingerprint001",
            "sha256:newinventoryfingerprint001");

        string json = JsonSerializer.Serialize(
            new { change, snapshot, inventory = DataClassInventoryCatalog.Published },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("data-class-inventory-schema.v1");
        json.ShouldContain("audit-records");
        json.ShouldContain("retain-immutable");
        json.ShouldNotContain("mailboxSubject", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        // Note: the `ai-prompts-outputs-context` class id legitimately contains "prompt" — the no-leak floor bans raw
        // prompt CONTENT, not the bounded class-id token, so a substring check on "prompt" would be a false positive.
    }

    [Fact]
    public static void InventoryContractsShouldNotExposeSecretBearingProperties()
    {
        string[] blockedNameFragments =
        [
            "ProjectName",
            "EvidenceContent",
            "MailboxBody",
            "MailboxSubject",
            "ProviderPayload",
            "RawClaim",
            "Header",
            "Token",
            "Secret",
            "Prompt",
        ];
        Type[] contractTypes =
        [
            typeof(DataClassClassification),
            typeof(DataClassInventory),
            typeof(DataClassInventoryChangeSet),
            typeof(DataClassInventorySnapshotMetadata),
            typeof(SubmitDataClassInventoryChange),
        ];

        foreach (Type contractType in contractTypes)
        {
            string[] propertyNames = contractType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(static property => property.Name)
                .ToArray();

            foreach (string blocked in blockedNameFragments)
            {
                propertyNames.ShouldNotContain(name => name.Contains(blocked, StringComparison.Ordinal), contractType.Name);
            }
        }
    }

    private static SubmitDataClassInventoryChange InventoryChange()
        => new(
            "inventory-change-001",
            "inventory-snapshot-current",
            "inventory-snapshot-proposed",
            8,
            new DataClassInventoryChangeSet(DataClassInventoryCatalog.Published.Classifications),
            "data-class-inventory-update",
            "admin-requester",
            DataClassInventorySchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:oldinventoryfingerprint001",
            "sha256:newinventoryfingerprint001",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero));
}
