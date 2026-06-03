using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The schema versions for the Story 9.9 deletion/erasure run artifact and its governed request command. Mirrors
/// <see cref="TenantExportSchemaVersions"/> — a closed, ordinal set with a known-membership check.
/// </summary>
public static class DeletionErasureSchemaVersions
{
    public const string V1 = "deletion-erasure-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// The closed per-class action dimension (AC1). Each value is an <c>AuditMetadata</c>-safe bounded token. Mirrors
/// the <see cref="TenantExportClassDispositions"/> shape line-for-line. The action is keyed off the class's
/// <see cref="DataClassDeletionBehaviors"/>: <c>key-shred</c>⇒<see cref="CryptoShredded"/>;
/// <c>projection-tombstone</c>⇒<see cref="Tombstoned"/>; <c>hard-delete</c>⇒<see cref="HardDeleted"/>;
/// <c>retain-immutable</c> (and every fail-closed/unauthorized case)⇒<see cref="Retained"/>.
/// </summary>
public static class DeletionErasureClassActions
{
    public const string CryptoShredded = "crypto-shredded";
    public const string Tombstoned = "tombstoned";
    public const string HardDeleted = "hard-deleted";
    public const string Retained = "retained";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([CryptoShredded, Tombstoned, HardDeleted, Retained], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed denial/exclusion-reason dimension (AC1/AC2). <see cref="WormRetained"/> is the absolute WORM-class
/// reason (audit-records are never destroyed); <see cref="Unauthorized"/> is the only signal a hidden resource ever
/// produces — never the resource identity (NFR2). Mirrors <see cref="TenantExportExclusionReasons"/>.
/// </summary>
public static class DeletionErasureExclusionReasons
{
    public const string WormRetained = "worm-retained";
    public const string Unauthorized = "unauthorized";
    public const string NotRequested = "not-requested";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([WormRetained, Unauthorized, NotRequested], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed per-class status dimension (AC4, NFR17). <see cref="FailedRetryable"/>/<see cref="FailedTerminal"/>
/// are produced by the deferred destruction runtime via the one <c>RetryFailurePolicy</c> taxonomy. Mirrors
/// <see cref="TenantExportClassStatuses"/>.
/// </summary>
public static class DeletionErasureClassStatuses
{
    public const string Succeeded = "succeeded";
    public const string FailedRetryable = "failed-retryable";
    public const string FailedTerminal = "failed-terminal";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Succeeded, FailedRetryable, FailedTerminal], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed run-status dimension (AC4). Mirrors <see cref="TenantExportRunStatuses"/>.
/// </summary>
public static class DeletionErasureRunStatuses
{
    public const string Completed = "completed";
    public const string PartialFailure = "partial-failure";
    public const string Failed = "failed";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Completed, PartialFailure, Failed], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed request-mode dimension (AC1/AC3). <see cref="Erasure"/> additionally runs audit-chain erasure through
/// the existing Story 9.1 <c>AuditRedactionService</c> seam. Mirrors <see cref="TenantExportClassDispositions"/>.
/// </summary>
public static class DeletionErasureModes
{
    public const string Deletion = "deletion";
    public const string Erasure = "erasure";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Deletion, Erasure], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The bounded deletion/erasure scope (AC1/AC2). An empty <see cref="ProjectScopeRefs"/> is a tenant-wide request; a
/// non-empty list is project-bounded and every member must be covered by the requester's per-project authority.
/// </summary>
public sealed record DeletionErasureScope(
    string TenantRef,
    IReadOnlyList<string> ProjectScopeRefs);

/// <summary>
/// The requested set of ChatBot-owned data classes plus the deletion/erasure mode and scope (AC1).
/// <see cref="Mode"/> ∈ <see cref="DeletionErasureModes"/>.
/// </summary>
public sealed record DeletionErasureRequestSpec(
    string Mode,
    IReadOnlyList<string> RequestedDataClassIds,
    DeletionErasureScope Scope);

/// <summary>
/// The bounded authority value the pure <see cref="DeletionErasurePlanner"/> consumes (AC2). The server-side
/// <c>DeletionErasureAuthorizationPolicy</c> projects a <c>ClaimsPrincipal</c> into this view so no
/// <c>ClaimsPrincipal</c> dependency ever crosses into <c>.Contracts</c>.
/// </summary>
public sealed record DeletionErasureAuthorityView(
    bool HasComplianceScope,
    IReadOnlySet<string> AuthorizedProjectRefs);

/// <summary>
/// The per-data-class deletion/erasure decision (AC1/AC2/AC4). Every field is a bounded, <c>AuditMetadata</c>-safe
/// token. <see cref="ExclusionReason"/> is non-empty only for a <c>retained</c> class. Destruction is fail-closed:
/// an <c>unauthorized</c> class is <c>retained</c>, never destructive.
/// </summary>
public sealed record DeletionErasureClassResult(
    string DataClassId,
    string DeletionBehavior,
    string Action,
    string ExclusionReason,
    string Status,
    string OwnerRole);

/// <summary>
/// A metadata-only erasure-proof confirmation for one successfully-erased subject/class (AC5). Carries the
/// tenant-scoped <see cref="SubjectLocator"/> + <see cref="Tombstoned"/> tombstone confirmation and the safe KMS
/// <see cref="KeyHandle"/> + <see cref="KeyShredded"/> key-shred confirmation — never raw subject content.
/// </summary>
public sealed record ErasureProofEntry(
    string DataClassId,
    string SubjectLocator,
    bool Tombstoned,
    string KeyHandle,
    bool KeyShredded);

/// <summary>
/// The metadata-only erasure-proof artifact for a completed erasure (AC5, NFR53). <see cref="ProofFingerprint"/> is a
/// deterministic <c>sha256:</c> digest over the confirmation set — a class that did not reach <c>succeeded</c>
/// contributes no entry (the no-partial-proof floor, consistent with AC4).
/// </summary>
public sealed record ErasureProofArtifact(
    string DeletionRunId,
    IReadOnlyList<ErasureProofEntry> Entries,
    string ProofFingerprint,
    DateTimeOffset GeneratedAtUtc,
    string CorrelationId);

/// <summary>
/// The correlation-stamped result of a deletion/erasure run (AC1/AC4/AC5). <see cref="DeletionRunId"/> is the stable
/// idempotency/run key. <see cref="Proof"/> seals exactly the <c>succeeded</c> destructive classes — a
/// failed/retained class contributes no proof entry.
/// </summary>
public sealed record DeletionErasureRunResult(
    string DeletionRunId,
    string Mode,
    string RunStatus,
    IReadOnlyList<DeletionErasureClassResult> ClassResults,
    ErasureProofArtifact Proof,
    DateTimeOffset GeneratedAtUtc,
    string CorrelationId);

/// <summary>
/// The NFR35 policy-snapshot metadata for a deletion/erasure run. Mirrors <see cref="TenantExportSnapshotMetadata"/>
/// field-for-field, replacing <c>ExportedDataClassIds</c> with <see cref="DeletedDataClassIds"/> and using
/// <see cref="AdminScope.Compliance"/> for <see cref="ScopeUsed"/>. Old/new values are <c>sha256:</c> fingerprints,
/// never raw subject bytes.
/// </summary>
public sealed record DeletionErasureSnapshotMetadata(
    string SnapshotId,
    string SchemaVersion,
    string SupersedesSnapshotId,
    string SupersededBySnapshotId,
    string SourceChangeId,
    string ActorRef,
    AdminScope ScopeUsed,
    IReadOnlyList<string> DeletedDataClassIds,
    long SourceVersion,
    DateTimeOffset EffectiveAtUtc,
    string CorrelationId,
    string ReasonCode,
    string PolicySnapshotId,
    string OldSnapshotFingerprint,
    string NewSnapshotFingerprint);

/// <summary>
/// The compliance-admin-gated governed command that submits a deletion/erasure request (AC1/AC2/AC4). A structural
/// twin of <see cref="SubmitTenantExportRequest"/> — gated at the <c>ParticipantAuthorizationStage</c> by
/// <c>HasHumanAdminScope(.., AdminScope.Compliance)</c>, routed through the one CommandGateway audit-commit spine,
/// fail-closed with no durable write and no destruction on unauthorized scope / invalid command / audit-writer-down.
/// <see cref="DeletionRunId"/> is the stable idempotency/run key (Story 1.5 two-altitude floor).
/// </summary>
public sealed record SubmitDeletionErasureRequest(
    string DeletionRunId,
    string InventorySnapshotId,
    long SourceVersion,
    DeletionErasureRequestSpec RequestSpec,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string PolicySnapshotId,
    string ProofFingerprint,
    DateTimeOffset EffectiveAtUtc) : IChatBotCommand;

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

/// <summary>
/// Validation for the deletion/erasure request spec and run result. Reuses the Story 7.4
/// <see cref="RetentionValidationResult"/> and <see cref="ComplianceAdministrationSchema"/> token helpers — it does
/// NOT introduce a second result type or token validator. Beyond per-field closed-set checks it enforces the AC1
/// behavior-vs-action invariant, the architecture #13 WORM-class invariant, the AC4 no-silent-partial invariant, the
/// AC5 proof invariant, and run-status consistency.
/// </summary>
public static class DeletionErasureSchema
{
    private static readonly IReadOnlySet<string> WormClasses =
        new HashSet<string>([ComplianceRetentionClassIds.AuditRecords], StringComparer.Ordinal);

    public static RetentionValidationResult ValidateRequestSpec(DeletionErasureRequestSpec? spec)
    {
        if (spec?.RequestedDataClassIds is not { Count: > 0 } requested ||
            requested.Count > ComplianceRetentionClassIds.All.Count ||
            !DeletionErasureModes.Contains(spec.Mode) ||
            spec.Scope is null ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(spec.Scope.TenantRef))
        {
            return RetentionValidationResult.Invalid("deletion_request_invalid");
        }

        List<string> errors = [];
        HashSet<string> classes = new(StringComparer.Ordinal);
        foreach (string dataClassId in requested)
        {
            if (!ComplianceRetentionClassIds.All.Contains(dataClassId))
            {
                errors.Add("deletion_class_invalid");
            }
            else if (!classes.Add(dataClassId))
            {
                errors.Add("deletion_class_duplicate");
            }
        }

        foreach (string projectScopeRef in spec.Scope.ProjectScopeRefs ?? [])
        {
            if (!ComplianceAdministrationSchema.IsSafeComplianceToken(projectScopeRef))
            {
                errors.Add("deletion_project_ref_invalid");
            }
        }

        return errors.Count == 0
            ? RetentionValidationResult.Valid
            : new RetentionValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    public static RetentionValidationResult ValidateRunResult(
        DeletionErasureRunResult? result,
        IReadOnlyCollection<string>? requestedDataClassIds = null)
    {
        if (result?.ClassResults is not { Count: > 0 } classResults ||
            !DeletionErasureRunStatuses.Contains(result.RunStatus) ||
            !DeletionErasureModes.Contains(result.Mode) ||
            result.Proof is null ||
            !ComplianceAdministrationSchema.IsSafeFingerprint(result.Proof.ProofFingerprint) ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(result.DeletionRunId) ||
            !ComplianceAdministrationSchema.IsSafeComplianceToken(result.CorrelationId) ||
            !ComplianceAdministrationSchema.IsUtc(result.GeneratedAtUtc))
        {
            return RetentionValidationResult.Invalid("deletion_result_invalid");
        }

        List<string> errors = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (DeletionErasureClassResult classResult in classResults)
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
                    errors.Add("deletion_class_unprocessed");
                }
            }
        }

        ValidateProof(result, classResults, errors);

        // Run-status consistency with the per-class statuses.
        if (!string.Equals(result.RunStatus, ExpectedRunStatus(classResults), StringComparison.Ordinal))
        {
            errors.Add("deletion_run_status_inconsistent");
        }

        return errors.Count == 0
            ? RetentionValidationResult.Valid
            : new RetentionValidationResult(false, errors.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static void ValidateClassResult(DeletionErasureClassResult classResult, HashSet<string> seen, List<string> errors)
    {
        if (classResult is null)
        {
            errors.Add("deletion_result_invalid");
            return;
        }

        if (!ComplianceRetentionClassIds.All.Contains(classResult.DataClassId))
        {
            errors.Add("deletion_class_invalid");
        }
        else if (!seen.Add(classResult.DataClassId))
        {
            errors.Add("deletion_class_duplicate");
        }

        if (!DataClassDeletionBehaviors.Contains(classResult.DeletionBehavior))
        {
            errors.Add("deletion_behavior_invalid");
        }

        if (!DeletionErasureClassActions.Contains(classResult.Action))
        {
            errors.Add("deletion_action_invalid");
        }

        if (!DeletionErasureClassStatuses.Contains(classResult.Status))
        {
            errors.Add("deletion_status_invalid");
        }

        bool retained = string.Equals(classResult.Action, DeletionErasureClassActions.Retained, StringComparison.Ordinal);
        if (retained)
        {
            if (!DeletionErasureExclusionReasons.Contains(classResult.ExclusionReason))
            {
                errors.Add("deletion_exclusion_reason_invalid");
            }
        }
        else if (!string.IsNullOrEmpty(classResult.ExclusionReason))
        {
            errors.Add("deletion_exclusion_reason_invalid");
        }

        // Architecture #13 WORM: a retain-immutable class — and audit-records by identity — is ALWAYS retained with
        // reason worm-retained; it may never be destroyed.
        bool wormBehavior = string.Equals(classResult.DeletionBehavior, DataClassDeletionBehaviors.RetainImmutable, StringComparison.Ordinal);
        if ((wormBehavior || WormClasses.Contains(classResult.DataClassId)) &&
            (!retained || !string.Equals(classResult.ExclusionReason, DeletionErasureExclusionReasons.WormRetained, StringComparison.Ordinal)))
        {
            errors.Add("deletion_worm_class_destroyed");
        }
        else if (!wormBehavior)
        {
            // Behavior-vs-action invariant for the destructive behaviors, unless authority forced retained/unauthorized.
            string expectedAction = classResult.DeletionBehavior switch
            {
                DataClassDeletionBehaviors.KeyShred => DeletionErasureClassActions.CryptoShredded,
                DataClassDeletionBehaviors.ProjectionTombstone => DeletionErasureClassActions.Tombstoned,
                DataClassDeletionBehaviors.HardDelete => DeletionErasureClassActions.HardDeleted,
                _ => string.Empty,
            };

            bool authorityRetained = retained &&
                string.Equals(classResult.ExclusionReason, DeletionErasureExclusionReasons.Unauthorized, StringComparison.Ordinal);
            if (expectedAction.Length > 0 &&
                !authorityRetained &&
                !string.Equals(classResult.Action, expectedAction, StringComparison.Ordinal))
            {
                errors.Add("deletion_behavior_action_mismatch");
            }
        }
    }

    private static void ValidateProof(
        DeletionErasureRunResult result,
        IReadOnlyList<DeletionErasureClassResult> classResults,
        List<string> errors)
    {
        HashSet<string> destructiveClasses = classResults
            .Where(DeletionErasurePlanner.IsSucceededDestructive)
            .Select(static classResult => classResult.DataClassId)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> proofClasses = new(StringComparer.Ordinal);
        foreach (ErasureProofEntry entry in result.Proof.Entries ?? [])
        {
            if (entry is null ||
                !ComplianceAdministrationSchema.IsSafeComplianceToken(entry.SubjectLocator) ||
                !ComplianceAdministrationSchema.IsSafeComplianceToken(entry.KeyHandle))
            {
                errors.Add("deletion_proof_partial_exposed");
                continue;
            }

            // No-partial-exposure: a proof entry exists only for a succeeded destructive class.
            if (!destructiveClasses.Contains(entry.DataClassId))
            {
                errors.Add("deletion_proof_partial_exposed");
            }

            proofClasses.Add(entry.DataClassId);
        }

        // Every succeeded destructive class must carry exactly one proof entry.
        if (!proofClasses.SetEquals(destructiveClasses))
        {
            errors.Add("deletion_proof_partial_exposed");
        }

        // Proof invariant: the fingerprint covers exactly the carried confirmation set.
        string expected = DeletionErasurePlanner.ComputeProofFingerprint(result.Proof.Entries ?? []);
        if (!string.Equals(result.Proof.ProofFingerprint, expected, StringComparison.Ordinal))
        {
            errors.Add("deletion_proof_partial_exposed");
        }
    }

    private static string ExpectedRunStatus(IReadOnlyList<DeletionErasureClassResult> classResults)
    {
        DeletionErasureClassResult[] actionable = classResults
            .Where(static classResult =>
                !string.Equals(classResult.Action, DeletionErasureClassActions.Retained, StringComparison.Ordinal))
            .ToArray();

        if (actionable.Length == 0)
        {
            return DeletionErasureRunStatuses.Completed;
        }

        int succeeded = actionable.Count(static classResult =>
            string.Equals(classResult.Status, DeletionErasureClassStatuses.Succeeded, StringComparison.Ordinal));

        if (succeeded == actionable.Length)
        {
            return DeletionErasureRunStatuses.Completed;
        }

        return succeeded == 0 ? DeletionErasureRunStatuses.Failed : DeletionErasureRunStatuses.PartialFailure;
    }
}
