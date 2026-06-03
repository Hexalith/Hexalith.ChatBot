using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Story 9.9 contracts coverage: the five closed action/exclusion/status/run-status/mode token sets, the pure
/// <see cref="DeletionErasurePlanner"/> decision engine against the Story 9.7 seed catalog (deletion-behavior →
/// action, authority → retained/unauthorized with destruction biased fail-closed, WORM-class absolute), and the
/// <see cref="DeletionErasureSchema"/> accept/reject invariants (behavior-vs-action, no-silent-partial, proof).
/// Mirrors the <c>TenantExportContractTests</c> style (round-trips + closed-set membership + Shouldly).
/// </summary>
public static class DeletionErasureContractTests
{
    private static readonly DateTimeOffset GeneratedAt = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(DeletionErasureClassActions.CryptoShredded)]
    [InlineData(DeletionErasureClassActions.Tombstoned)]
    [InlineData(DeletionErasureClassActions.HardDeleted)]
    [InlineData(DeletionErasureClassActions.Retained)]
    public static void ClassActionsShouldBeAClosedSet(string value)
    {
        DeletionErasureClassActions.Contains(value).ShouldBeTrue();
        DeletionErasureClassActions.Contains("purged").ShouldBeFalse();
        DeletionErasureClassActions.Contains(null).ShouldBeFalse();
    }

    [Theory]
    [InlineData(DeletionErasureExclusionReasons.WormRetained)]
    [InlineData(DeletionErasureExclusionReasons.Unauthorized)]
    [InlineData(DeletionErasureExclusionReasons.NotRequested)]
    public static void ExclusionReasonsShouldBeAClosedSet(string value)
    {
        DeletionErasureExclusionReasons.Contains(value).ShouldBeTrue();
        DeletionErasureExclusionReasons.Contains("because").ShouldBeFalse();
    }

    [Theory]
    [InlineData(DeletionErasureClassStatuses.Succeeded)]
    [InlineData(DeletionErasureClassStatuses.FailedRetryable)]
    [InlineData(DeletionErasureClassStatuses.FailedTerminal)]
    public static void ClassStatusesShouldBeAClosedSet(string value)
    {
        DeletionErasureClassStatuses.Contains(value).ShouldBeTrue();
        DeletionErasureClassStatuses.Contains("pending").ShouldBeFalse();
    }

    [Theory]
    [InlineData(DeletionErasureRunStatuses.Completed)]
    [InlineData(DeletionErasureRunStatuses.PartialFailure)]
    [InlineData(DeletionErasureRunStatuses.Failed)]
    public static void RunStatusesShouldBeAClosedSet(string value)
    {
        DeletionErasureRunStatuses.Contains(value).ShouldBeTrue();
        DeletionErasureRunStatuses.Contains("queued").ShouldBeFalse();
    }

    [Theory]
    [InlineData(DeletionErasureModes.Deletion)]
    [InlineData(DeletionErasureModes.Erasure)]
    public static void ModesShouldBeAClosedSet(string value)
    {
        DeletionErasureModes.Contains(value).ShouldBeTrue();
        DeletionErasureModes.Contains("purge").ShouldBeFalse();
    }

    [Fact]
    public static void PlanAgainstSeedCatalogShouldBeDeletionBehaviorAware()
    {
        DeletionErasureRequestSpec spec = TenantWideSpec();
        DeletionErasureRunResult run = DeletionErasurePlanner.Plan(
            DataClassInventoryCatalog.Published, spec, FullComplianceAuthority(), "deletion-run-001", GeneratedAt, "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        // AC1: an all-actionable authorized run completes and validates as a sealed, complete result.
        run.RunStatus.ShouldBe(DeletionErasureRunStatuses.Completed);
        run.Mode.ShouldBe(DeletionErasureModes.Erasure);
        run.GeneratedAtUtc.Offset.ShouldBe(TimeSpan.Zero);
        DeletionErasureSchema.ValidateRunResult(run, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();

        // Architecture #13 WORM: audit-records (retain-immutable) is ALWAYS retained/worm-retained, never destroyed.
        DeletionErasureClassResult audit = ClassFor(run, ComplianceRetentionClassIds.AuditRecords);
        audit.Action.ShouldBe(DeletionErasureClassActions.Retained);
        audit.ExclusionReason.ShouldBe(DeletionErasureExclusionReasons.WormRetained);

        // key-shred classes ⇒ crypto-shredded.
        ClassFor(run, ComplianceRetentionClassIds.SourceEmailMetadata).Action.ShouldBe(DeletionErasureClassActions.CryptoShredded);
        ClassFor(run, ComplianceRetentionClassIds.AiPromptsOutputsContext).Action.ShouldBe(DeletionErasureClassActions.CryptoShredded);

        // projection-tombstone classes ⇒ tombstoned.
        ClassFor(run, ComplianceRetentionClassIds.AssociationRecords).Action.ShouldBe(DeletionErasureClassActions.Tombstoned);
        ClassFor(run, ComplianceRetentionClassIds.EvaluationDatasets).Action.ShouldBe(DeletionErasureClassActions.Tombstoned);

        // AC5: the proof seals exactly the succeeded crypto-shredded/tombstoned classes — never the retained WORM class.
        run.Proof.Entries.ShouldNotContain(static entry => entry.DataClassId == ComplianceRetentionClassIds.AuditRecords);
        run.Proof.ProofFingerprint.ShouldStartWith("sha256:");
    }

    [Fact]
    public static void PlanWithUnauthorizedProjectShouldRetainWithoutLeakingTheResource()
    {
        // AC2/NFR2: a project-bounded request whose project is NOT authorized retains every otherwise-destructive class
        // with reason `unauthorized` (never a destructive action) and carries no resource identity — but a WORM class
        // keeps its absolute `worm-retained` reason (behavior precedes authority).
        DeletionErasureRequestSpec spec = new(
            DeletionErasureModes.Erasure,
            [.. ComplianceRetentionClassIds.All],
            new DeletionErasureScope("tenant-deletion-owner", ["project-hidden-007"]));
        DeletionErasureAuthorityView authority = new(true, new HashSet<string>(StringComparer.Ordinal));

        DeletionErasureRunResult run = DeletionErasurePlanner.Plan(
            DataClassInventoryCatalog.Published, spec, authority, "deletion-run-001", GeneratedAt, "corr-2");

        // Destruction is fail-closed: every class is retained, none destructive.
        run.ClassResults.ShouldAllBe(static result => result.Action == DeletionErasureClassActions.Retained);
        run.Proof.Entries.ShouldBeEmpty();

        ClassFor(run, ComplianceRetentionClassIds.SourceEmailMetadata).ExclusionReason.ShouldBe(DeletionErasureExclusionReasons.Unauthorized);
        ClassFor(run, ComplianceRetentionClassIds.AuditRecords).ExclusionReason.ShouldBe(DeletionErasureExclusionReasons.WormRetained);

        // The hidden project ref never reaches the run result.
        string rendered = JsonSerializer.Serialize(run, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        rendered.ShouldNotContain("project-hidden-007", Case.Insensitive);
        DeletionErasureSchema.ValidateRunResult(run, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();
    }

    [Fact]
    public static void PlanWithoutComplianceScopeShouldRetainEverythingActionable()
    {
        DeletionErasureAuthorityView authority = new(false, new HashSet<string>(StringComparer.Ordinal));
        DeletionErasureRunResult run = DeletionErasurePlanner.Plan(
            DataClassInventoryCatalog.Published, TenantWideSpec(), authority, "deletion-run-001", GeneratedAt, "corr-3");

        run.ClassResults.ShouldAllBe(static result => result.Action == DeletionErasureClassActions.Retained);
        ClassFor(run, ComplianceRetentionClassIds.SourceEmailMetadata).ExclusionReason.ShouldBe(DeletionErasureExclusionReasons.Unauthorized);
        ClassFor(run, ComplianceRetentionClassIds.AuditRecords).ExclusionReason.ShouldBe(DeletionErasureExclusionReasons.WormRetained);
    }

    [Fact]
    public static void PlanWithAuthorizedProjectScopeShouldProduceDestructiveActions()
    {
        // AC2 positive path: a project-bounded request whose project IS authorized lets every destructive class through
        // (no `unauthorized`), while the WORM class stays retained/worm-retained.
        DeletionErasureRequestSpec spec = new(
            DeletionErasureModes.Erasure,
            [.. ComplianceRetentionClassIds.All],
            new DeletionErasureScope("tenant-deletion-owner", ["project-authorized-001"]));
        DeletionErasureAuthorityView authority = new(
            true, new HashSet<string>(["project-authorized-001"], StringComparer.Ordinal));

        DeletionErasureRunResult run = DeletionErasurePlanner.Plan(
            DataClassInventoryCatalog.Published, spec, authority, "deletion-run-001", GeneratedAt, "corr-auth");

        ClassFor(run, ComplianceRetentionClassIds.SourceEmailMetadata).Action.ShouldBe(DeletionErasureClassActions.CryptoShredded);
        run.ClassResults.ShouldNotContain(static result => result.ExclusionReason == DeletionErasureExclusionReasons.Unauthorized);
        ClassFor(run, ComplianceRetentionClassIds.AuditRecords).ExclusionReason.ShouldBe(DeletionErasureExclusionReasons.WormRetained);
        DeletionErasureSchema.ValidateRunResult(run, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();
    }

    [Fact]
    public static void PlanWithAnyUnauthorizedProjectInScopeShouldRetainTheWholeRunWithoutLeaking()
    {
        // AC2/NFR2: the authority gate is all-or-nothing — a single unauthorized project ref forces the whole run to
        // retained/unauthorized, and the hidden ref never reaches the result.
        DeletionErasureRequestSpec spec = new(
            DeletionErasureModes.Deletion,
            [ComplianceRetentionClassIds.SourceEmailMetadata],
            new DeletionErasureScope("tenant-deletion-owner", ["project-authorized-001", "project-unauthorized-002"]));
        DeletionErasureAuthorityView authority = new(
            true, new HashSet<string>(["project-authorized-001"], StringComparer.Ordinal));

        DeletionErasureRunResult run = DeletionErasurePlanner.Plan(
            DataClassInventoryCatalog.Published, spec, authority, "deletion-run-001", GeneratedAt, "corr-part");

        DeletionErasureClassResult email = ClassFor(run, ComplianceRetentionClassIds.SourceEmailMetadata);
        email.Action.ShouldBe(DeletionErasureClassActions.Retained);
        email.ExclusionReason.ShouldBe(DeletionErasureExclusionReasons.Unauthorized);

        string rendered = JsonSerializer.Serialize(run, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        rendered.ShouldNotContain("project-unauthorized-002", Case.Insensitive);
    }

    [Fact]
    public static void ValidateRequestSpecShouldAcceptAValidRequestAndRejectMalformedOnes()
    {
        DeletionErasureSchema.ValidateRequestSpec(TenantWideSpec()).IsValid.ShouldBeTrue();

        // (a) a requested class outside the canonical set.
        DeletionErasureSchema.ValidateRequestSpec(new DeletionErasureRequestSpec(
                DeletionErasureModes.Deletion, ["not-a-real-class"], new DeletionErasureScope("tenant-deletion-owner", [])))
            .Errors.ShouldContain("deletion_class_invalid");

        // (b) a duplicate requested class.
        DeletionErasureSchema.ValidateRequestSpec(new DeletionErasureRequestSpec(
                DeletionErasureModes.Deletion,
                [ComplianceRetentionClassIds.Attachments, ComplianceRetentionClassIds.Attachments],
                new DeletionErasureScope("tenant-deletion-owner", [])))
            .Errors.ShouldContain("deletion_class_duplicate");

        // Structural: empty class set / bad mode / bad tenant ref / unsafe project ref.
        DeletionErasureSchema.ValidateRequestSpec(new DeletionErasureRequestSpec(
                DeletionErasureModes.Deletion, [], new DeletionErasureScope("tenant-deletion-owner", [])))
            .Errors.ShouldContain("deletion_request_invalid");
        DeletionErasureSchema.ValidateRequestSpec(new DeletionErasureRequestSpec(
                "purge", [ComplianceRetentionClassIds.Attachments], new DeletionErasureScope("tenant-deletion-owner", [])))
            .Errors.ShouldContain("deletion_request_invalid");
        DeletionErasureSchema.ValidateRequestSpec(new DeletionErasureRequestSpec(
                DeletionErasureModes.Deletion, [ComplianceRetentionClassIds.Attachments], new DeletionErasureScope("unsafe tenant!", [])))
            .Errors.ShouldContain("deletion_request_invalid");
        DeletionErasureSchema.ValidateRequestSpec(new DeletionErasureRequestSpec(
                DeletionErasureModes.Deletion, [ComplianceRetentionClassIds.Attachments], new DeletionErasureScope("tenant-deletion-owner", ["unsafe project!"])))
            .Errors.ShouldContain("deletion_project_ref_invalid");
    }

    [Fact]
    public static void ValidateRunResultShouldRejectWormBehaviorProofAndCompletenessViolations()
    {
        DeletionErasureRequestSpec spec = TenantWideSpec();
        DeletionErasureRunResult valid = DeletionErasurePlanner.Plan(
            DataClassInventoryCatalog.Published, spec, FullComplianceAuthority(), "deletion-run-001", GeneratedAt, "corr-4");

        // (c) a retain-immutable WORM class marked destroyed.
        MutateClass(valid, ComplianceRetentionClassIds.AuditRecords, static result => result with
        {
            Action = DeletionErasureClassActions.CryptoShredded,
            ExclusionReason = string.Empty,
        }).Errors.ShouldContain("deletion_worm_class_destroyed");

        // (d) a key-shred class whose action is not crypto-shredded (behavior/action mismatch).
        MutateClass(valid, ComplianceRetentionClassIds.Attachments, static result => result with
        {
            Action = DeletionErasureClassActions.HardDeleted,
        }).Errors.ShouldContain("deletion_behavior_action_mismatch");

        // (e) a proof entry for a non-succeeded class (no-partial-exposure).
        DeletionErasureRunResult forgedProof = valid with
        {
            ClassResults =
            [
                .. valid.ClassResults.Select(result =>
                    string.Equals(result.DataClassId, ComplianceRetentionClassIds.SourceEmailMetadata, StringComparison.Ordinal)
                        ? result with { Status = DeletionErasureClassStatuses.FailedRetryable }
                        : result),
            ],
        };
        DeletionErasureSchema.ValidateRunResult(forgedProof).Errors.ShouldContain("deletion_proof_partial_exposed");

        // (f) a requested class missing from the results.
        DeletionErasureSchema.ValidateRunResult(valid, [.. spec.RequestedDataClassIds, "evaluation-datasets-extra-not-present"])
            .Errors.ShouldContain("deletion_class_unprocessed");
    }

    [Fact]
    public static void ValidateRunResultShouldRejectClosedSetAndStructuralViolations()
    {
        DeletionErasureRequestSpec spec = TenantWideSpec();
        DeletionErasureRunResult valid = DeletionErasurePlanner.Plan(
            DataClassInventoryCatalog.Published, spec, FullComplianceAuthority(), "deletion-run-001", GeneratedAt, "corr-cs");

        // Top-level structural guards: null, bad run status, bad mode, unsafe run id, non-UTC stamp.
        DeletionErasureSchema.ValidateRunResult(null).Errors.ShouldContain("deletion_result_invalid");
        DeletionErasureSchema.ValidateRunResult(valid with { RunStatus = "in-progress" }).Errors.ShouldContain("deletion_result_invalid");
        DeletionErasureSchema.ValidateRunResult(valid with { Mode = "purge" }).Errors.ShouldContain("deletion_result_invalid");
        DeletionErasureSchema.ValidateRunResult(valid with { DeletionRunId = "unsafe run!" }).Errors.ShouldContain("deletion_result_invalid");
        DeletionErasureSchema.ValidateRunResult(valid with { GeneratedAtUtc = new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.FromHours(2)) })
            .Errors.ShouldContain("deletion_result_invalid");

        // Per-class closed-set violations.
        MutateClass(valid, ComplianceRetentionClassIds.SourceEmailMetadata, static result => result with { Action = "purged" })
            .Errors.ShouldContain("deletion_action_invalid");
        MutateClass(valid, ComplianceRetentionClassIds.SourceEmailMetadata, static result => result with { Status = "pending" })
            .Errors.ShouldContain("deletion_status_invalid");
        MutateClass(valid, ComplianceRetentionClassIds.SourceEmailMetadata, static result => result with { DeletionBehavior = "vaporize" })
            .Errors.ShouldContain("deletion_behavior_invalid");

        // Exclusion-reason both ways: a retained class with an out-of-set reason, and a destructive class carrying one.
        MutateClass(valid, ComplianceRetentionClassIds.AuditRecords, static result => result with { ExclusionReason = "because" })
            .Errors.ShouldContain("deletion_exclusion_reason_invalid");
        MutateClass(valid, ComplianceRetentionClassIds.SourceEmailMetadata, static result => result with { ExclusionReason = DeletionErasureExclusionReasons.Unauthorized })
            .Errors.ShouldContain("deletion_exclusion_reason_invalid");

        // Result-level duplicate.
        DeletionErasureRunResult duplicated = valid with { ClassResults = [.. valid.ClassResults, valid.ClassResults[0]] };
        DeletionErasureSchema.ValidateRunResult(duplicated).Errors.ShouldContain("deletion_class_duplicate");
    }

    [Fact]
    public static void ValidateRunResultShouldAcceptPartialFailureWithProofOverSucceededClassesOnly()
    {
        // AC4: when one destructive class fails (retryable), the run is `partial-failure`, the failed class carries no
        // proof entry, and the sealed proof covers exactly the still-succeeded destructive classes (no partial exposure).
        DeletionErasureRequestSpec spec = TenantWideSpec();
        DeletionErasureRunResult run = DeletionErasurePlanner.Plan(
            DataClassInventoryCatalog.Published, spec, FullComplianceAuthority(), "deletion-run-001", GeneratedAt, "corr-pf");

        DeletionErasureRunResult partial = WithClassStatuses(
            run,
            static result => string.Equals(result.DataClassId, ComplianceRetentionClassIds.SourceEmailMetadata, StringComparison.Ordinal)
                ? result with { Status = DeletionErasureClassStatuses.FailedRetryable }
                : result,
            DeletionErasureRunStatuses.PartialFailure);

        DeletionErasureSchema.ValidateRunResult(partial, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();
        partial.Proof.Entries.ShouldNotContain(static entry => entry.DataClassId == ComplianceRetentionClassIds.SourceEmailMetadata);

        // Mislabeling the exact same per-class shape as `completed` is rejected as run-status-inconsistent.
        DeletionErasureSchema.ValidateRunResult(
                partial with { RunStatus = DeletionErasureRunStatuses.Completed }, spec.RequestedDataClassIds)
            .Errors.ShouldContain("deletion_run_status_inconsistent");
    }

    [Fact]
    public static void DeletionContractsShouldNotExposeSecretBearingProperties()
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
            typeof(DeletionErasureScope),
            typeof(DeletionErasureRequestSpec),
            typeof(DeletionErasureAuthorityView),
            typeof(DeletionErasureClassResult),
            typeof(ErasureProofEntry),
            typeof(ErasureProofArtifact),
            typeof(DeletionErasureRunResult),
            typeof(DeletionErasureSnapshotMetadata),
            typeof(SubmitDeletionErasureRequest),
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
    public static void SubmitDeletionErasureRequestShouldSerializeMetadataOnlyTokensAndFingerprints()
    {
        SubmitDeletionErasureRequest request = DeletionRequest();
        DeletionErasureSnapshotMetadata snapshot = new(
            "deletion-snapshot-001",
            DeletionErasureSchemaVersions.V1,
            "deletion-snapshot-current",
            "deletion-snapshot-next",
            "deletion-run-001",
            "admin-requester",
            AdminScope.Compliance,
            [ComplianceRetentionClassIds.SourceEmailMetadata],
            8,
            GeneratedAt,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "deletion-erasure-request",
            "policy-snapshot-admin-v1",
            "sha256:olddeletionfingerprint001",
            "sha256:newdeletionfingerprint001");

        string json = JsonSerializer.Serialize(
            new { request, snapshot }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("deletion-erasure-schema.v1");
        json.ShouldContain("source-email-metadata");
        json.ShouldNotContain("mailboxSubject", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public static void PlanShouldResolveHardDeleteBehaviorToHardDeletedAndSealNoProofEntry()
    {
        // AC1: `hard-delete`⇒`hard-deleted` is a closed-set member with its own planner + behavior-vs-action branch, yet
        // NO seed class is hard-delete (it is only reachable via a future inventory edit). Exercise it with a synthetic
        // inventory. A hard-deleted class is actionable+succeeded — but the proof seals only crypto-shred/tombstone
        // confirmations, so it contributes NO proof entry.
        DataClassInventory inventory = InventoryWith(
            Classification(ComplianceRetentionClassIds.Attachments, DataClassDeletionBehaviors.HardDelete));
        DeletionErasureRequestSpec spec = new(
            DeletionErasureModes.Deletion,
            [ComplianceRetentionClassIds.Attachments],
            new DeletionErasureScope("tenant-deletion-owner", []));

        DeletionErasureRunResult run = DeletionErasurePlanner.Plan(
            inventory, spec, FullComplianceAuthority(), "deletion-run-001", GeneratedAt, "corr-hard-delete");

        DeletionErasureClassResult attachments = ClassFor(run, ComplianceRetentionClassIds.Attachments);
        attachments.Action.ShouldBe(DeletionErasureClassActions.HardDeleted);
        attachments.Status.ShouldBe(DeletionErasureClassStatuses.Succeeded);
        attachments.ExclusionReason.ShouldBeEmpty();

        run.RunStatus.ShouldBe(DeletionErasureRunStatuses.Completed);
        run.Proof.Entries.ShouldBeEmpty();
        DeletionErasureSchema.ValidateRunResult(run, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();
    }

    [Fact]
    public static void PlanShouldFailClosedToRetainedForAClassMissingFromTheInventory()
    {
        // AC1 fail-closed (Completion-Notes invariant): a requested canonical class with NO classification in the
        // inventory is treated as WORM — retained/worm-retained, never destroyed (the planner defaults an unknown
        // behavior to retain-immutable). Destruction is the most dangerous operation; an unclassifiable class is safe.
        DataClassInventory inventory = InventoryWith(
            Classification(ComplianceRetentionClassIds.SourceEmailMetadata, DataClassDeletionBehaviors.KeyShred));
        DeletionErasureRequestSpec spec = new(
            DeletionErasureModes.Erasure,
            [ComplianceRetentionClassIds.Backups], // canonical, but absent from this inventory
            new DeletionErasureScope("tenant-deletion-owner", []));

        DeletionErasureRunResult run = DeletionErasurePlanner.Plan(
            inventory, spec, FullComplianceAuthority(), "deletion-run-001", GeneratedAt, "corr-fail-closed");

        DeletionErasureClassResult backups = ClassFor(run, ComplianceRetentionClassIds.Backups);
        backups.Action.ShouldBe(DeletionErasureClassActions.Retained);
        backups.ExclusionReason.ShouldBe(DeletionErasureExclusionReasons.WormRetained);
        backups.DeletionBehavior.ShouldBe(DataClassDeletionBehaviors.RetainImmutable);
        run.Proof.Entries.ShouldBeEmpty();
        DeletionErasureSchema.ValidateRunResult(run, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();
    }

    [Fact]
    public static void ValidateRunResultShouldAcceptAFullyFailedRunAndRejectMislabeling()
    {
        // AC4: when every actionable class fails, the run status is `failed` — the all-failed branch the completed/
        // partial-failure tests never reach. A failed class carries no proof entry, and mislabeling as `completed` is
        // rejected as run-status-inconsistent.
        DataClassInventory inventory = InventoryWith(
            Classification(ComplianceRetentionClassIds.SourceEmailMetadata, DataClassDeletionBehaviors.KeyShred));
        DeletionErasureRequestSpec spec = new(
            DeletionErasureModes.Erasure,
            [ComplianceRetentionClassIds.SourceEmailMetadata],
            new DeletionErasureScope("tenant-deletion-owner", []));
        DeletionErasureRunResult run = DeletionErasurePlanner.Plan(
            inventory, spec, FullComplianceAuthority(), "deletion-run-001", GeneratedAt, "corr-all-failed");

        DeletionErasureRunResult failed = WithClassStatuses(
            run,
            static result => result with { Status = DeletionErasureClassStatuses.FailedTerminal },
            DeletionErasureRunStatuses.Failed);

        DeletionErasureSchema.ValidateRunResult(failed, spec.RequestedDataClassIds).IsValid.ShouldBeTrue();
        failed.Proof.Entries.ShouldBeEmpty();

        DeletionErasureSchema.ValidateRunResult(
                failed with { RunStatus = DeletionErasureRunStatuses.Completed }, spec.RequestedDataClassIds)
            .Errors.ShouldContain("deletion_run_status_inconsistent");
    }

    [Fact]
    public static void PlanShouldBeDeterministicForIdempotentRetries()
    {
        // AC4 (Story 1.5 two-altitude idempotency floor): the pure planner is deterministic — a same-run-id retry over
        // the same inputs yields a structurally identical run + identical proof fingerprint, so a retry never signals
        // duplicate destruction.
        DeletionErasureRequestSpec spec = TenantWideSpec();
        DeletionErasureRunResult first = DeletionErasurePlanner.Plan(
            DataClassInventoryCatalog.Published, spec, FullComplianceAuthority(), "deletion-run-001", GeneratedAt, "corr-idem");
        DeletionErasureRunResult second = DeletionErasurePlanner.Plan(
            DataClassInventoryCatalog.Published, spec, FullComplianceAuthority(), "deletion-run-001", GeneratedAt, "corr-idem");

        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        JsonSerializer.Serialize(second, options).ShouldBe(JsonSerializer.Serialize(first, options));
        second.Proof.ProofFingerprint.ShouldBe(first.Proof.ProofFingerprint);
    }

    private static DataClassInventory InventoryWith(params DataClassClassification[] classifications)
        => new("compliance", "v-test", GeneratedAt, "data-class-inventory-schema.v1", classifications);

    private static DataClassClassification Classification(string dataClassId, string deletionBehavior)
        => new(
            dataClassId,
            AdminRoles.ComplianceAdmin,
            dataClassId,
            DataClassRedactionSensitivities.MetadataOnly,
            deletionBehavior,
            DataClassExportEligibilities.NotExportable,
            "minimization-none");

    private static DeletionErasureRequestSpec TenantWideSpec()
        => new(DeletionErasureModes.Erasure, [.. ComplianceRetentionClassIds.All], new DeletionErasureScope("tenant-deletion-owner", []));

    private static DeletionErasureAuthorityView FullComplianceAuthority()
        => new(true, new HashSet<string>(StringComparer.Ordinal));

    private static DeletionErasureClassResult ClassFor(DeletionErasureRunResult run, string dataClassId)
        => run.ClassResults.Single(result => string.Equals(result.DataClassId, dataClassId, StringComparison.Ordinal));

    // Rebuilds a run after per-class status mutations, re-sealing the proof over exactly the succeeded destructive
    // classes — so the resulting run is a faithful (valid-by-construction) partial-failure shape, never a forged one.
    private static DeletionErasureRunResult WithClassStatuses(
        DeletionErasureRunResult run,
        Func<DeletionErasureClassResult, DeletionErasureClassResult> mutate,
        string runStatus)
    {
        DeletionErasureClassResult[] mutated = [.. run.ClassResults.Select(mutate)];
        HashSet<string> keptDestructive = mutated
            .Where(DeletionErasurePlanner.IsSucceededDestructive)
            .Select(static result => result.DataClassId)
            .ToHashSet(StringComparer.Ordinal);
        ErasureProofEntry[] keptEntries = [.. run.Proof.Entries.Where(entry => keptDestructive.Contains(entry.DataClassId))];
        return run with
        {
            ClassResults = mutated,
            RunStatus = runStatus,
            Proof = run.Proof with
            {
                Entries = keptEntries,
                ProofFingerprint = DeletionErasurePlanner.ComputeProofFingerprint(keptEntries),
            },
        };
    }

    private static RetentionValidationResult MutateClass(
        DeletionErasureRunResult run,
        string dataClassId,
        Func<DeletionErasureClassResult, DeletionErasureClassResult> mutate)
    {
        DeletionErasureRunResult mutated = run with
        {
            ClassResults =
            [
                .. run.ClassResults.Select(result =>
                    string.Equals(result.DataClassId, dataClassId, StringComparison.Ordinal) ? mutate(result) : result),
            ],
        };
        RetentionValidationResult result = DeletionErasureSchema.ValidateRunResult(mutated);
        result.IsValid.ShouldBeFalse();
        return result;
    }

    private static SubmitDeletionErasureRequest DeletionRequest()
        => new(
            "deletion-run-001",
            "inventory-snapshot-current",
            8,
            new DeletionErasureRequestSpec(
                DeletionErasureModes.Erasure,
                [ComplianceRetentionClassIds.SourceEmailMetadata, ComplianceRetentionClassIds.AuditRecords],
                new DeletionErasureScope("tenant-deletion-owner", ["project-authorized-001"])),
            "deletion-erasure-request",
            "admin-requester",
            DeletionErasureSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:deletionprooffingerprint001",
            new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.Zero));
}
