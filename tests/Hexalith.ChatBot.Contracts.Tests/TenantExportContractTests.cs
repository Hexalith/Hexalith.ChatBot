using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Story 9.8 contracts coverage: the closed disposition/exclusion/redaction-decision/status/run-status token sets,
/// the pure <see cref="TenantExportPlanner"/> decision engine against the Story 9.7 seed catalog (eligibility →
/// disposition, authority → exclusion, WORM-class exclusion, no-partial-exposure manifest), and the
/// <see cref="TenantExportSchema"/> accept/reject invariants. Mirrors the <c>DataClassInventoryContractTests</c>
/// style (round-trips + closed-set membership + Shouldly).
/// </summary>
public static class TenantExportContractTests
{
    private static readonly DateTimeOffset GeneratedAt = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(TenantExportClassDispositions.Included)]
    [InlineData(TenantExportClassDispositions.Redacted)]
    [InlineData(TenantExportClassDispositions.Excluded)]
    public static void ClassDispositionsShouldBeAClosedSet(string value)
    {
        TenantExportClassDispositions.Contains(value).ShouldBeTrue();
        TenantExportClassDispositions.Contains("partial").ShouldBeFalse();
        TenantExportClassDispositions.Contains(null).ShouldBeFalse();
    }

    [Theory]
    [InlineData(TenantExportExclusionReasons.NotExportable)]
    [InlineData(TenantExportExclusionReasons.Unauthorized)]
    [InlineData(TenantExportExclusionReasons.NotRequested)]
    public static void ExclusionReasonsShouldBeAClosedSet(string value)
    {
        TenantExportExclusionReasons.Contains(value).ShouldBeTrue();
        TenantExportExclusionReasons.Contains("because").ShouldBeFalse();
    }

    [Theory]
    [InlineData(TenantExportRedactionDecisions.MetadataOnly)]
    [InlineData(TenantExportRedactionDecisions.Redacted)]
    [InlineData(TenantExportRedactionDecisions.None)]
    public static void RedactionDecisionsShouldBeAClosedSet(string value)
    {
        TenantExportRedactionDecisions.Contains(value).ShouldBeTrue();
        TenantExportRedactionDecisions.Contains("raw").ShouldBeFalse();
    }

    [Theory]
    [InlineData(TenantExportClassStatuses.Succeeded)]
    [InlineData(TenantExportClassStatuses.FailedRetryable)]
    [InlineData(TenantExportClassStatuses.FailedTerminal)]
    public static void ClassStatusesShouldBeAClosedSet(string value)
    {
        TenantExportClassStatuses.Contains(value).ShouldBeTrue();
        TenantExportClassStatuses.Contains("pending").ShouldBeFalse();
    }

    [Theory]
    [InlineData(TenantExportRunStatuses.Completed)]
    [InlineData(TenantExportRunStatuses.PartialFailure)]
    [InlineData(TenantExportRunStatuses.Failed)]
    public static void RunStatusesShouldBeAClosedSet(string value)
    {
        TenantExportRunStatuses.Contains(value).ShouldBeTrue();
        TenantExportRunStatuses.Contains("queued").ShouldBeFalse();
    }

    [Fact]
    public static void PlanAgainstSeedCatalogShouldBeDataClassAndRedactionAware()
    {
        TenantExportRequestSpec spec = TenantWideSpec();
        TenantExportRunResult run = TenantExportPlanner.Plan(
            DataClassInventoryCatalog.Published, spec, FullComplianceAuthority(), "export-run-001", GeneratedAt, "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        // AC3: an all-includable authorized run completes and validates as a sealed, complete result.
        run.RunStatus.ShouldBe(TenantExportRunStatuses.Completed);
        run.GeneratedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
        TenantExportSchema.ValidateRunResult(run, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();

        // Architecture #13 WORM: audit-records and backups are always excluded/not-exportable, never produced.
        foreach (string wormClass in new[] { ComplianceRetentionClassIds.AuditRecords, ComplianceRetentionClassIds.Backups })
        {
            TenantExportClassResult result = ClassFor(run, wormClass);
            result.Disposition.ShouldBe(TenantExportClassDispositions.Excluded);
            result.ExclusionReason.ShouldBe(TenantExportExclusionReasons.NotExportable);
            result.ArtifactFingerprint.ShouldBeEmpty();
        }

        // A redacted-export class with restricted sensitivity ⇒ redacted disposition + redacted decision.
        TenantExportClassResult email = ClassFor(run, ComplianceRetentionClassIds.SourceEmailMetadata);
        email.Disposition.ShouldBe(TenantExportClassDispositions.Redacted);
        email.RedactionDecision.ShouldBe(TenantExportRedactionDecisions.Redacted);
        email.ArtifactFingerprint.ShouldStartWith("sha256:");

        // A redacted-export class with metadata-only sensitivity ⇒ redacted disposition + metadata-only decision.
        TenantExportClassResult logs = ClassFor(run, ComplianceRetentionClassIds.LogsSupportBundles);
        logs.Disposition.ShouldBe(TenantExportClassDispositions.Redacted);
        logs.RedactionDecision.ShouldBe(TenantExportRedactionDecisions.MetadataOnly);
    }

    [Fact]
    public static void PlanShouldIncludeExportableClassesWithNoRedaction()
    {
        // The seed catalog has no `exportable` class; exercise the included/none path with a custom inventory.
        DataClassInventory inventory = DataClassInventoryCatalog.Published with
        {
            Classifications =
            [
                .. DataClassInventoryCatalog.Published.Classifications.Select(classification =>
                    string.Equals(classification.DataClassId, ComplianceRetentionClassIds.PolicySnapshots, StringComparison.Ordinal)
                        ? classification with { ExportEligibility = DataClassExportEligibilities.Exportable }
                        : classification),
            ],
        };

        TenantExportRunResult run = TenantExportPlanner.Plan(
            inventory, TenantWideSpec(), FullComplianceAuthority(), "export-run-001", GeneratedAt, "corr-1");

        TenantExportClassResult policy = ClassFor(run, ComplianceRetentionClassIds.PolicySnapshots);
        policy.Disposition.ShouldBe(TenantExportClassDispositions.Included);
        policy.RedactionDecision.ShouldBe(TenantExportRedactionDecisions.None);
        policy.ArtifactFingerprint.ShouldStartWith("sha256:");
        TenantExportSchema.ValidateRunResult(run, run.ClassResults.Select(static r => r.DataClassId).ToArray()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public static void PlanWithUnauthorizedProjectShouldExcludeWithoutLeakingTheResource()
    {
        // AC2/NFR2: a project-bounded request whose project is NOT in the authorized set excludes every otherwise
        // exportable class with reason `unauthorized` and carries no resource identity — but a not-exportable class
        // keeps its absolute `not-exportable` reason (eligibility precedes authority).
        TenantExportRequestSpec spec = new(
            [.. ComplianceRetentionClassIds.All],
            new TenantExportScope("tenant-alpha", ["project-hidden-007"]));
        TenantExportAuthorityView authority = new(true, new HashSet<string>(StringComparer.Ordinal));

        TenantExportRunResult run = TenantExportPlanner.Plan(
            DataClassInventoryCatalog.Published, spec, authority, "export-run-001", GeneratedAt, "corr-2");

        foreach (TenantExportClassResult result in run.ClassResults)
        {
            result.Disposition.ShouldBe(TenantExportClassDispositions.Excluded);
            result.ArtifactFingerprint.ShouldBeEmpty();
        }

        ClassFor(run, ComplianceRetentionClassIds.SourceEmailMetadata).ExclusionReason.ShouldBe(TenantExportExclusionReasons.Unauthorized);
        ClassFor(run, ComplianceRetentionClassIds.AuditRecords).ExclusionReason.ShouldBe(TenantExportExclusionReasons.NotExportable);

        // The hidden project ref never reaches the run result.
        string rendered = JsonSerializer.Serialize(run, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        rendered.ShouldNotContain("project-hidden-007", Case.Insensitive);
        TenantExportSchema.ValidateRunResult(run, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();
    }

    [Fact]
    public static void PlanWithoutComplianceScopeShouldExcludeEverythingExportable()
    {
        TenantExportAuthorityView authority = new(false, new HashSet<string>(StringComparer.Ordinal));
        TenantExportRunResult run = TenantExportPlanner.Plan(
            DataClassInventoryCatalog.Published, TenantWideSpec(), authority, "export-run-001", GeneratedAt, "corr-3");

        run.ClassResults.ShouldAllBe(static result =>
            result.Disposition == TenantExportClassDispositions.Excluded);
        ClassFor(run, ComplianceRetentionClassIds.SourceEmailMetadata).ExclusionReason.ShouldBe(TenantExportExclusionReasons.Unauthorized);
    }

    [Fact]
    public static void ValidateRequestSpecShouldAcceptAValidRequestAndRejectMalformedOnes()
    {
        TenantExportSchema.ValidateRequestSpec(TenantWideSpec()).IsValid.ShouldBeTrue();

        // (a) a requested class outside the canonical set.
        TenantExportSchema.ValidateRequestSpec(new TenantExportRequestSpec(
                ["not-a-real-class"], new TenantExportScope("tenant-alpha", [])))
            .Errors.ShouldContain("export_class_invalid");

        // (b) a duplicate requested class.
        TenantExportSchema.ValidateRequestSpec(new TenantExportRequestSpec(
                [ComplianceRetentionClassIds.Attachments, ComplianceRetentionClassIds.Attachments], new TenantExportScope("tenant-alpha", [])))
            .Errors.ShouldContain("export_class_duplicate");

        // Structural: empty class set / bad tenant ref / unsafe project ref.
        TenantExportSchema.ValidateRequestSpec(new TenantExportRequestSpec([], new TenantExportScope("tenant-alpha", [])))
            .Errors.ShouldContain("tenant_export_request_invalid");
        TenantExportSchema.ValidateRequestSpec(new TenantExportRequestSpec(
                [ComplianceRetentionClassIds.Attachments], new TenantExportScope("unsafe tenant!", [])))
            .Errors.ShouldContain("tenant_export_request_invalid");
        TenantExportSchema.ValidateRequestSpec(new TenantExportRequestSpec(
                [ComplianceRetentionClassIds.Attachments], new TenantExportScope("tenant-alpha", ["unsafe project!"])))
            .Errors.ShouldContain("export_project_ref_invalid");
    }

    [Fact]
    public static void ValidateRunResultShouldRejectEligibilityWormManifestAndCompletenessViolations()
    {
        TenantExportRequestSpec spec = TenantWideSpec();
        TenantExportRunResult valid = TenantExportPlanner.Plan(
            DataClassInventoryCatalog.Published, spec, FullComplianceAuthority(), "export-run-001", GeneratedAt, "corr-4");

        // (c) a not-exportable class marked included.
        MutateClass(valid, ComplianceRetentionClassIds.AuditRecords, static result => result with
        {
            Disposition = TenantExportClassDispositions.Included,
            ExclusionReason = string.Empty,
            ArtifactFingerprint = "sha256:forged",
        }).Errors.ShouldContain("export_eligibility_disposition_mismatch");

        // (d) audit-records marked redacted (WORM exposure).
        MutateClass(valid, ComplianceRetentionClassIds.AuditRecords, static result => result with
        {
            Disposition = TenantExportClassDispositions.Redacted,
            ExclusionReason = string.Empty,
        }).Errors.ShouldContain("export_worm_class_exposed");

        // (e) a manifest claiming a class that is no longer succeeded.
        MutateClass(valid, ComplianceRetentionClassIds.SourceEmailMetadata, static result => result with
        {
            Status = TenantExportClassStatuses.FailedRetryable,
        }).Errors.ShouldContain("export_manifest_partial_exposed");

        // (f) a requested class missing from the results.
        TenantExportSchema.ValidateRunResult(valid, [.. spec.RequestedDataClassIds, "evaluation-datasets-extra-not-present"])
            .Errors.ShouldContain("export_class_unprocessed");
    }

    [Fact]
    public static void PlanWithAuthorizedProjectScopeShouldProduceIncludableDispositions()
    {
        // AC2 positive path: a project-bounded request whose project IS in the authorized set lets every
        // exportable/redacted-export class through (no `unauthorized`), while the WORM classes stay excluded.
        TenantExportRequestSpec spec = new(
            [.. ComplianceRetentionClassIds.All],
            new TenantExportScope("tenant-alpha", ["project-authorized-001"]));
        TenantExportAuthorityView authority = new(
            true, new HashSet<string>(["project-authorized-001"], StringComparer.Ordinal));

        TenantExportRunResult run = TenantExportPlanner.Plan(
            DataClassInventoryCatalog.Published, spec, authority, "export-run-001", GeneratedAt, "corr-auth");

        ClassFor(run, ComplianceRetentionClassIds.SourceEmailMetadata).Disposition.ShouldBe(TenantExportClassDispositions.Redacted);
        run.ClassResults.ShouldNotContain(static result => result.ExclusionReason == TenantExportExclusionReasons.Unauthorized);
        ClassFor(run, ComplianceRetentionClassIds.AuditRecords).ExclusionReason.ShouldBe(TenantExportExclusionReasons.NotExportable);
        TenantExportSchema.ValidateRunResult(run, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();
    }

    [Fact]
    public static void PlanWithAnyUnauthorizedProjectInScopeShouldExcludeTheWholeRunWithoutLeaking()
    {
        // AC2/NFR2: the authority gate is all-or-nothing — a single unauthorized project ref in the scope forces the
        // whole run to `excluded`/`unauthorized`, and the hidden ref never reaches the result.
        TenantExportRequestSpec spec = new(
            [ComplianceRetentionClassIds.SourceEmailMetadata],
            new TenantExportScope("tenant-alpha", ["project-authorized-001", "project-unauthorized-002"]));
        TenantExportAuthorityView authority = new(
            true, new HashSet<string>(["project-authorized-001"], StringComparer.Ordinal));

        TenantExportRunResult run = TenantExportPlanner.Plan(
            DataClassInventoryCatalog.Published, spec, authority, "export-run-001", GeneratedAt, "corr-part");

        TenantExportClassResult email = ClassFor(run, ComplianceRetentionClassIds.SourceEmailMetadata);
        email.Disposition.ShouldBe(TenantExportClassDispositions.Excluded);
        email.ExclusionReason.ShouldBe(TenantExportExclusionReasons.Unauthorized);

        string rendered = JsonSerializer.Serialize(run, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        rendered.ShouldNotContain("project-unauthorized-002", Case.Insensitive);
    }

    [Fact]
    public static void ValidateRunResultShouldAcceptPartialFailureWithManifestOverSucceededClassesOnly()
    {
        // AC3: when one includable class fails (retryable), the run is `partial-failure`, the failed class carries no
        // artifact, and the sealed manifest covers exactly the still-succeeded includable classes (no partial exposure).
        TenantExportRequestSpec spec = TenantWideSpec();
        TenantExportRunResult run = TenantExportPlanner.Plan(
            DataClassInventoryCatalog.Published, spec, FullComplianceAuthority(), "export-run-001", GeneratedAt, "corr-pf");

        TenantExportRunResult partial = WithClassStatuses(
            run,
            static result => string.Equals(result.DataClassId, ComplianceRetentionClassIds.SourceEmailMetadata, StringComparison.Ordinal)
                ? result with { Status = TenantExportClassStatuses.FailedRetryable, ArtifactFingerprint = string.Empty }
                : result,
            TenantExportRunStatuses.PartialFailure);

        TenantExportSchema.ValidateRunResult(partial, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();
        ClassFor(partial, ComplianceRetentionClassIds.SourceEmailMetadata).ArtifactFingerprint.ShouldBeEmpty();

        // Mislabeling the exact same per-class shape as `completed` is rejected as run-status-inconsistent.
        TenantExportSchema.ValidateRunResult(
                partial with { RunStatus = TenantExportRunStatuses.Completed }, spec.RequestedDataClassIds)
            .Errors.ShouldContain("export_run_status_inconsistent");
    }

    [Fact]
    public static void ValidateRunResultShouldAcceptFullyFailedRunWithEmptyManifestCoverage()
    {
        // AC3: every includable class fails ⇒ run status `failed`, no class carries an artifact, and the manifest
        // seals the empty set — the no-partial-exposure floor holds even under total failure.
        TenantExportRequestSpec spec = TenantWideSpec();
        TenantExportRunResult run = TenantExportPlanner.Plan(
            DataClassInventoryCatalog.Published, spec, FullComplianceAuthority(), "export-run-001", GeneratedAt, "corr-f");

        TenantExportRunResult failed = WithClassStatuses(
            run,
            static result => IsIncludable(result)
                ? result with { Status = TenantExportClassStatuses.FailedTerminal, ArtifactFingerprint = string.Empty }
                : result,
            TenantExportRunStatuses.Failed);

        TenantExportSchema.ValidateRunResult(failed, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();
        failed.ClassResults.ShouldAllBe(static result => result.ArtifactFingerprint == string.Empty);
    }

    [Fact]
    public static void ValidateRunResultShouldRejectClosedSetAndStructuralViolations()
    {
        TenantExportRequestSpec spec = TenantWideSpec();
        TenantExportRunResult valid = TenantExportPlanner.Plan(
            DataClassInventoryCatalog.Published, spec, FullComplianceAuthority(), "export-run-001", GeneratedAt, "corr-cs");

        // Top-level structural guards: null, bad run status, bad manifest fingerprint, unsafe run id, non-UTC stamp.
        TenantExportSchema.ValidateRunResult(null).Errors.ShouldContain("tenant_export_result_invalid");
        TenantExportSchema.ValidateRunResult(valid with { RunStatus = "in-progress" }).Errors.ShouldContain("tenant_export_result_invalid");
        TenantExportSchema.ValidateRunResult(valid with { ManifestFingerprint = "not-a-fingerprint" }).Errors.ShouldContain("tenant_export_result_invalid");
        TenantExportSchema.ValidateRunResult(valid with { ExportRunId = "unsafe run!" }).Errors.ShouldContain("tenant_export_result_invalid");
        TenantExportSchema.ValidateRunResult(valid with { GeneratedAtUtc = new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.FromHours(2)) })
            .Errors.ShouldContain("tenant_export_result_invalid");

        // Per-class closed-set violations.
        MutateClass(valid, ComplianceRetentionClassIds.SourceEmailMetadata, static result => result with { Disposition = "partial" })
            .Errors.ShouldContain("export_disposition_invalid");
        MutateClass(valid, ComplianceRetentionClassIds.SourceEmailMetadata, static result => result with { RedactionDecision = "raw" })
            .Errors.ShouldContain("export_redaction_decision_invalid");
        MutateClass(valid, ComplianceRetentionClassIds.SourceEmailMetadata, static result => result with { Status = "pending" })
            .Errors.ShouldContain("export_status_invalid");
        MutateClass(valid, ComplianceRetentionClassIds.SourceEmailMetadata, static result => result with { ExportEligibility = "maybe" })
            .Errors.ShouldContain("export_eligibility_invalid");

        // Exclusion-reason both ways: an excluded class with an out-of-set reason, and a non-excluded class carrying one.
        MutateClass(valid, ComplianceRetentionClassIds.AuditRecords, static result => result with { ExclusionReason = "because" })
            .Errors.ShouldContain("export_exclusion_reason_invalid");
        MutateClass(valid, ComplianceRetentionClassIds.SourceEmailMetadata, static result => result with { ExclusionReason = TenantExportExclusionReasons.Unauthorized })
            .Errors.ShouldContain("export_exclusion_reason_invalid");

        // Result-level duplicate (distinct from the request-spec duplicate covered above).
        TenantExportRunResult duplicated = valid with { ClassResults = [.. valid.ClassResults, valid.ClassResults[0]] };
        TenantExportSchema.ValidateRunResult(duplicated).Errors.ShouldContain("export_class_duplicate");
    }

    [Fact]
    public static void ExportContractsShouldNotExposeSecretBearingProperties()
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
            "Body",
        ];
        Type[] contractTypes =
        [
            typeof(TenantExportScope),
            typeof(TenantExportRequestSpec),
            typeof(TenantExportAuthorityView),
            typeof(TenantExportClassResult),
            typeof(TenantExportRunResult),
            typeof(TenantExportSnapshotMetadata),
            typeof(SubmitTenantExportRequest),
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

    [Fact]
    public static void SubmitTenantExportRequestShouldSerializeMetadataOnlyTokensAndFingerprints()
    {
        SubmitTenantExportRequest request = ExportRequest();
        TenantExportSnapshotMetadata snapshot = new(
            "export-snapshot-001",
            TenantExportSchemaVersions.V1,
            "export-snapshot-current",
            "export-snapshot-next",
            "export-run-001",
            "admin-requester",
            AdminScope.Compliance,
            [ComplianceRetentionClassIds.SourceEmailMetadata],
            8,
            GeneratedAt,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "tenant-export-request",
            "policy-snapshot-admin-v1",
            "sha256:oldexportfingerprint001",
            "sha256:newexportfingerprint001");

        string json = JsonSerializer.Serialize(
            new { request, snapshot }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("tenant-export-schema.v1");
        json.ShouldContain("source-email-metadata");
        json.ShouldNotContain("mailboxSubject", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
    }

    private static TenantExportRequestSpec TenantWideSpec()
        => new([.. ComplianceRetentionClassIds.All], new TenantExportScope("tenant-alpha", []));

    private static TenantExportAuthorityView FullComplianceAuthority()
        => new(true, new HashSet<string>(StringComparer.Ordinal));

    private static TenantExportClassResult ClassFor(TenantExportRunResult run, string dataClassId)
        => run.ClassResults.Single(result => string.Equals(result.DataClassId, dataClassId, StringComparison.Ordinal));

    private static bool IsIncludable(TenantExportClassResult result)
        => string.Equals(result.Disposition, TenantExportClassDispositions.Included, StringComparison.Ordinal) ||
            string.Equals(result.Disposition, TenantExportClassDispositions.Redacted, StringComparison.Ordinal);

    private static bool IsSucceededIncludable(TenantExportClassResult result)
        => string.Equals(result.Status, TenantExportClassStatuses.Succeeded, StringComparison.Ordinal) && IsIncludable(result);

    // Rebuilds a run after per-class status mutations, re-sealing the manifest over exactly the succeeded includable
    // classes — so the resulting run is a faithful (valid-by-construction) partial/total-failure shape, never a forged one.
    private static TenantExportRunResult WithClassStatuses(
        TenantExportRunResult run,
        Func<TenantExportClassResult, TenantExportClassResult> mutate,
        string runStatus)
    {
        TenantExportClassResult[] mutated = [.. run.ClassResults.Select(mutate)];
        string manifest = TenantExportPlanner.ComputeManifestFingerprint(
            mutated.Where(IsSucceededIncludable).Select(static result => result.DataClassId));
        return run with { ClassResults = mutated, RunStatus = runStatus, ManifestFingerprint = manifest };
    }

    private static RetentionValidationResult MutateClass(
        TenantExportRunResult run,
        string dataClassId,
        Func<TenantExportClassResult, TenantExportClassResult> mutate)
    {
        TenantExportRunResult mutated = run with
        {
            ClassResults =
            [
                .. run.ClassResults.Select(result =>
                    string.Equals(result.DataClassId, dataClassId, StringComparison.Ordinal) ? mutate(result) : result),
            ],
        };
        RetentionValidationResult result = TenantExportSchema.ValidateRunResult(mutated);
        result.IsValid.ShouldBeFalse();
        return result;
    }

    private static SubmitTenantExportRequest ExportRequest()
        => new(
            "export-run-001",
            "inventory-snapshot-current",
            8,
            new TenantExportRequestSpec(
                [ComplianceRetentionClassIds.SourceEmailMetadata, ComplianceRetentionClassIds.AuditRecords],
                new TenantExportScope("tenant-alpha", ["project-authorized-001"])),
            "tenant-export-request",
            "admin-requester",
            TenantExportSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:exportmanifestfingerprint001",
            new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.Zero));
}
