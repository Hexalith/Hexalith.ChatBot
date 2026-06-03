using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The schema versions for the Story 9.8 tenant-export artifact and its governed request command. Mirrors
/// <see cref="DataClassInventorySchemaVersions"/> — a closed, ordinal set with a known-membership check.
/// </summary>
public static class TenantExportSchemaVersions
{
    public const string V1 = "tenant-export-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// The closed per-class disposition dimension (AC1). Each value is an <c>AuditMetadata</c>-safe bounded token.
/// Mirrors the <see cref="DataClassExportEligibilities"/> shape line-for-line.
/// </summary>
public static class TenantExportClassDispositions
{
    public const string Included = "included";
    public const string Redacted = "redacted";
    public const string Excluded = "excluded";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Included, Redacted, Excluded], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed exclusion-reason dimension (AC1/AC2). <see cref="Unauthorized"/> is the only signal a hidden
/// resource ever produces — never the resource identity (NFR2). Mirrors <see cref="DataClassExportEligibilities"/>.
/// </summary>
public static class TenantExportExclusionReasons
{
    public const string NotExportable = "not-exportable";
    public const string Unauthorized = "unauthorized";
    public const string NotRequested = "not-requested";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([NotExportable, Unauthorized, NotRequested], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed per-class redaction-decision dimension (AC1, NFR45). Distinct from the source
/// <see cref="DataClassRedactionSensitivities"/> dimension — this is the export-time decision token, never raw
/// content. Mirrors <see cref="DataClassExportEligibilities"/>.
/// </summary>
public static class TenantExportRedactionDecisions
{
    public const string MetadataOnly = "metadata-only";
    public const string Redacted = "redacted";
    public const string None = "none";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([MetadataOnly, Redacted, None], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed per-class status dimension (AC3, NFR17). <see cref="FailedRetryable"/>/<see cref="FailedTerminal"/>
/// are produced by the deferred extraction runtime via the one <c>RetryFailurePolicy</c> taxonomy. Mirrors
/// <see cref="DataClassExportEligibilities"/>.
/// </summary>
public static class TenantExportClassStatuses
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
/// The closed run-status dimension (AC3). Mirrors <see cref="DataClassExportEligibilities"/>.
/// </summary>
public static class TenantExportRunStatuses
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
/// The bounded export scope (AC1/AC2). An empty <see cref="ProjectScopeRefs"/> is a tenant-wide request; a
/// non-empty list is project-bounded and every member must be covered by the requester's per-project authority.
/// </summary>
public sealed record TenantExportScope(
    string TenantRef,
    IReadOnlyList<string> ProjectScopeRefs);

/// <summary>
/// The requested set of ChatBot-owned data classes plus the export scope (AC1).
/// </summary>
public sealed record TenantExportRequestSpec(
    IReadOnlyList<string> RequestedDataClassIds,
    TenantExportScope Scope);

/// <summary>
/// The bounded authority value the pure <see cref="TenantExportPlanner"/> consumes (AC2). The server-side
/// <c>TenantExportAuthorizationPolicy</c> projects a <c>ClaimsPrincipal</c> into this view so no
/// <c>ClaimsPrincipal</c> dependency ever crosses into <c>.Contracts</c>.
/// </summary>
public sealed record TenantExportAuthorityView(
    bool HasComplianceScope,
    IReadOnlySet<string> AuthorizedProjectRefs);

/// <summary>
/// The per-data-class export decision (AC1/AC2/AC3). Every field is a bounded, <c>AuditMetadata</c>-safe token.
/// <see cref="ArtifactFingerprint"/> is a <c>sha256:</c> token over the produced projection (never raw bytes) and
/// is empty for any class that is not a <c>succeeded</c> includable class (the no-partial-exposure floor).
/// </summary>
public sealed record TenantExportClassResult(
    string DataClassId,
    string ExportEligibility,
    string Disposition,
    string ExclusionReason,
    string RedactionDecision,
    string Status,
    string OwnerRole,
    string ArtifactFingerprint);

/// <summary>
/// The correlation-stamped result of an export run (AC1/AC3). <see cref="ManifestFingerprint"/> seals exactly the
/// <c>succeeded</c> includable classes — a failed/excluded class contributes no artifact.
/// </summary>
public sealed record TenantExportRunResult(
    string ExportRunId,
    string RunStatus,
    string ManifestFingerprint,
    IReadOnlyList<TenantExportClassResult> ClassResults,
    DateTimeOffset GeneratedAtUtc,
    string CorrelationId);

/// <summary>
/// The NFR35 policy-snapshot metadata for an export run. Mirrors <see cref="DataClassInventorySnapshotMetadata"/>
/// field-for-field, replacing <c>ChangedDataClassIds</c> with <see cref="ExportedDataClassIds"/> and using
/// <see cref="AdminScope.Compliance"/> for <see cref="ScopeUsed"/>. Old/new values are <c>sha256:</c> fingerprints,
/// never raw export bytes.
/// </summary>
public sealed record TenantExportSnapshotMetadata(
    string SnapshotId,
    string SchemaVersion,
    string SupersedesSnapshotId,
    string SupersededBySnapshotId,
    string SourceChangeId,
    string ActorRef,
    AdminScope ScopeUsed,
    IReadOnlyList<string> ExportedDataClassIds,
    long SourceVersion,
    DateTimeOffset EffectiveAtUtc,
    string CorrelationId,
    string ReasonCode,
    string PolicySnapshotId,
    string OldSnapshotFingerprint,
    string NewSnapshotFingerprint);

/// <summary>
/// The compliance-admin-gated governed command that submits a tenant export request (AC1/AC3/AC4). A structural
/// twin of <see cref="SubmitDataClassInventoryChange"/> — gated at the <c>ParticipantAuthorizationStage</c> by
/// <c>HasHumanAdminScope(.., AdminScope.Compliance)</c>, routed through the one CommandGateway audit-commit spine,
/// fail-closed with no durable write and no exposed artifact on unauthorized scope / invalid command /
/// audit-writer-down. <see cref="ExportRunId"/> is the stable idempotency/run key (Story 1.5 two-altitude floor).
/// </summary>
public sealed record SubmitTenantExportRequest(
    string ExportRunId,
    string InventorySnapshotId,
    long SourceVersion,
    TenantExportRequestSpec RequestSpec,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string PolicySnapshotId,
    string ManifestFingerprint,
    DateTimeOffset EffectiveAtUtc) : IChatBotCommand;

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
