using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The schema versions for the Story 9.10 consent/lawful-basis record artifact and its governed recording command.
/// Mirrors <see cref="DeletionErasureSchemaVersions"/> — a closed, ordinal set with a known-membership check.
/// </summary>
public static class ConsentLawfulBasisSchemaVersions
{
    public const string V1 = "consent-lawful-basis-schema.v1";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([V1], StringComparer.Ordinal);

    public static bool IsKnown(string? schemaVersion)
        => !string.IsNullOrWhiteSpace(schemaVersion) && All.Contains(schemaVersion);
}

/// <summary>
/// The closed governed-subject dimension (AC1). Each value is an <c>AuditMetadata</c>-safe bounded token. Mirrors
/// the <see cref="DeletionErasureClassActions"/> shape line-for-line. The four kinds are the FR20 governed subjects:
/// external participants, retained content, attachments, and AI-processing events.
/// </summary>
public static class ConsentSubjectKinds
{
    public const string ExternalParticipant = "external-participant";
    public const string RetainedContent = "retained-content";
    public const string Attachment = "attachment";
    public const string AiProcessing = "ai-processing";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([ExternalParticipant, RetainedContent, Attachment, AiProcessing], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed GDPR lawful-basis dimension (AC1). Each value is an <c>AuditMetadata</c>-safe bounded token mirroring
/// the GDPR Article 6 bases. Callers select within the set; they never invent a basis.
/// </summary>
public static class ConsentLawfulBases
{
    public const string Consent = "consent";
    public const string Contract = "contract";
    public const string LegalObligation = "legal-obligation";
    public const string VitalInterests = "vital-interests";
    public const string PublicTask = "public-task";
    public const string LegitimateInterests = "legitimate-interests";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(
            [Consent, Contract, LegalObligation, VitalInterests, PublicTask, LegitimateInterests],
            StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed consent-record status dimension (AC1/AC4). Only <see cref="Active"/> satisfies a <c>required</c> gate;
/// <see cref="Withdrawn"/>/<see cref="Expired"/>/<see cref="Superseded"/> never do (the AC4 fail-closed invariant).
/// </summary>
public static class ConsentRecordStatuses
{
    public const string Active = "active";
    public const string Withdrawn = "withdrawn";
    public const string Expired = "expired";
    public const string Superseded = "superseded";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Active, Withdrawn, Expired, Superseded], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed requirement-disposition dimension (AC4). A subject kind either <see cref="Required"/> (a basis must be
/// recorded before a governed action proceeds) or <see cref="NotRequired"/>. An unknown/missing entry biases to
/// <see cref="Required"/> in <see cref="ConsentRequirementPolicy"/> (fail-closed).
/// </summary>
public static class ConsentRequirementDispositions
{
    public const string Required = "required";
    public const string NotRequired = "not-required";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Required, NotRequired], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The closed gate-decision dimension (AC4). <see cref="ConsentGate"/> returns <see cref="Satisfied"/> when a
/// governed action may proceed and <see cref="BlockedMissingBasis"/> when it must fail closed pending an active basis.
/// </summary>
public static class ConsentGateDecisions
{
    public const string Satisfied = "satisfied";
    public const string BlockedMissingBasis = "blocked-missing-basis";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([Satisfied, BlockedMissingBasis], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

/// <summary>
/// The metadata-only consent/lawful-basis record (AC1). Every field is a bounded, <c>AuditMetadata</c>-safe token.
/// <see cref="SubjectLocator"/> is an opaque <c>AuditMetadata.IsSafeStableIdentifier</c> reference — never raw
/// participant email, file name, or message body. <see cref="RedactionSensitivity"/> is a
/// <see cref="DataClassRedactionSensitivities"/> member (the ONE sensitivity set — never forked).
/// </summary>
public sealed record ConsentLawfulBasisRecord(
    string RecordId,
    string SubjectKind,
    string SubjectLocator,
    string ProjectScopeRef,
    string LawfulBasis,
    string RecordStatus,
    string BasisSource,
    string RedactionSensitivity,
    DateTimeOffset RecordedAtUtc,
    string RecordFingerprint);

/// <summary>
/// The bounded requirement value the pure <see cref="ConsentRequirementPolicy"/> and <see cref="ConsentGate"/>
/// consume (AC4). Keys ⊆ <see cref="ConsentSubjectKinds.All"/>; each value ∈ <see cref="ConsentRequirementDispositions"/>.
/// The server seam builds this from <see cref="ConsentRequirementMatrix.Published"/> merged with any tenant override
/// (override wiring deferred — see the <c>ConsentRequirementProfileMapper</c> deferral hook).
/// </summary>
public sealed record ConsentRequirementProfile(
    IReadOnlyDictionary<string, string> DispositionsBySubjectKind);

/// <summary>
/// The bounded authority value the pure <see cref="ConsentLawfulBasisRedactionPolicy"/> consumes (AC2). The
/// server-side <c>ConsentLawfulBasisAuthorizationPolicy</c> projects a <c>ClaimsPrincipal</c> into this view so no
/// <c>ClaimsPrincipal</c> dependency ever crosses into <c>.Contracts</c>. Mirrors <see cref="DeletionErasureAuthorityView"/>.
/// </summary>
public sealed record ConsentLawfulBasisAuthorityView(
    bool HasComplianceScope,
    IReadOnlySet<string> AuthorizedProjectRefs);

/// <summary>
/// The NFR35 policy-snapshot metadata for a consent/lawful-basis recording. Mirrors
/// <see cref="DeletionErasureSnapshotMetadata"/> field-for-field, replacing <c>DeletedDataClassIds</c> with
/// <see cref="RecordedSubjectKinds"/> and using <see cref="AdminScope.Compliance"/> for <see cref="ScopeUsed"/>.
/// Old/new values are <c>sha256:</c> fingerprints, never raw subject bytes.
/// </summary>
public sealed record ConsentLawfulBasisSnapshotMetadata(
    string SnapshotId,
    string SchemaVersion,
    string SupersedesSnapshotId,
    string SupersededBySnapshotId,
    string SourceChangeId,
    string ActorRef,
    AdminScope ScopeUsed,
    IReadOnlyList<string> RecordedSubjectKinds,
    long SourceVersion,
    DateTimeOffset EffectiveAtUtc,
    string CorrelationId,
    string ReasonCode,
    string PolicySnapshotId,
    string OldSnapshotFingerprint,
    string NewSnapshotFingerprint);

/// <summary>
/// The compliance-admin-gated governed command that records consent/lawful-basis metadata (AC1/AC2/AC3). A structural
/// twin of <see cref="SubmitDeletionErasureRequest"/> — gated at the <c>ParticipantAuthorizationStage</c> by
/// <c>HasHumanAdminScope(.., AdminScope.Compliance)</c>, routed through the one CommandGateway audit-commit spine,
/// fail-closed with no durable write on unauthorized scope / invalid command / audit-writer-down.
/// <see cref="RecordId"/> is the stable idempotency/run key (Story 1.5 two-altitude floor). <see cref="SubjectLocator"/>
/// is an opaque safe stable identifier — never raw PII.
/// </summary>
public sealed record SubmitConsentLawfulBasisRecord(
    string RecordId,
    long SourceVersion,
    string SubjectKind,
    string SubjectLocator,
    string ProjectScopeRef,
    string LawfulBasis,
    string RecordStatus,
    string BasisSource,
    string RedactionSensitivity,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string PolicySnapshotId,
    string RecordFingerprint,
    DateTimeOffset EffectiveAtUtc) : IChatBotCommand;

/// <summary>
/// The as-shipped seed v1 consent-requirement matrix (AC1/AC4) declaring the default regulatory-profile disposition
/// per <see cref="ConsentSubjectKinds"/> member. Immutable, deterministic, token-only — mirrors
/// <see cref="DataClassInventoryCatalog.Published"/> (no <c>UtcNow</c>; fixed values). It biases every governed
/// subject kind to <c>required</c>; a future tenant-policy override may relax a kind (the override mapper is the
/// deferred server seam). The pure <see cref="ConsentRequirementPolicy"/>/<see cref="ConsentGate"/> consume it; they
/// do not redefine it.
/// </summary>
public static class ConsentRequirementMatrix
{
    public static ConsentRequirementProfile Published { get; } = new(
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ConsentSubjectKinds.ExternalParticipant] = ConsentRequirementDispositions.Required,
            [ConsentSubjectKinds.RetainedContent] = ConsentRequirementDispositions.Required,
            [ConsentSubjectKinds.Attachment] = ConsentRequirementDispositions.Required,
            [ConsentSubjectKinds.AiProcessing] = ConsentRequirementDispositions.Required,
        });
}

/// <summary>
/// The pure requirement-decision function (AC1/AC4). Looks up <paramref name="subjectKind"/> in the bounded profile;
/// an UNKNOWN subject kind or a missing/empty profile entry biases to <see cref="ConsentRequirementDispositions.Required"/>
/// (fail-closed, AC4). It carries no <c>ClaimsPrincipal</c> dependency and is a real, testable function.
/// </summary>
public static class ConsentRequirementPolicy
{
    public static string Evaluate(string? subjectKind, ConsentRequirementProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        // Fail-closed: an unknown subject kind is always treated as requiring a basis.
        if (!ConsentSubjectKinds.Contains(subjectKind))
        {
            return ConsentRequirementDispositions.Required;
        }

        // Fail-closed: a missing/empty/unrecognized disposition entry biases to required.
        if (profile.DispositionsBySubjectKind is null ||
            !profile.DispositionsBySubjectKind.TryGetValue(subjectKind!, out string? disposition) ||
            !ConsentRequirementDispositions.Contains(disposition))
        {
            return ConsentRequirementDispositions.Required;
        }

        return disposition;
    }
}

/// <summary>
/// The pure AC4 fail-closed gate decision (NFR7/FR68). Given a requirement disposition and the active-basis status of
/// the subject, returns a <see cref="ConsentGateDecisions"/> token: <c>not-required ⇒ satisfied</c>; <c>required</c>
/// AND an <c>active</c> basis ⇒ <c>satisfied</c>; <c>required</c> with a <c>null</c>/<c>withdrawn</c>/<c>expired</c>/
/// <c>superseded</c> status ⇒ <c>blocked-missing-basis</c>; an UNKNOWN disposition biases to <c>blocked-missing-basis</c>.
/// This is a real, testable function — not a comment.
/// </summary>
public static class ConsentGate
{
    public static string Evaluate(string? requirementDisposition, string? activeRecordStatus)
    {
        // not-required ⇒ satisfied without a basis record.
        if (string.Equals(requirementDisposition, ConsentRequirementDispositions.NotRequired, StringComparison.Ordinal))
        {
            return ConsentGateDecisions.Satisfied;
        }

        // required ⇒ only an active basis satisfies; everything else (null / withdrawn / expired / superseded)
        // fails closed. An unknown disposition also biases to blocked (fail-closed over convenience).
        if (string.Equals(requirementDisposition, ConsentRequirementDispositions.Required, StringComparison.Ordinal) &&
            string.Equals(activeRecordStatus, ConsentRecordStatuses.Active, StringComparison.Ordinal))
        {
            return ConsentGateDecisions.Satisfied;
        }

        return ConsentGateDecisions.BlockedMissingBasis;
    }
}

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

/// <summary>
/// The pure per-project read redaction (AC2, NFR2). When the reader lacks the compliance scope OR the record's
/// <see cref="ConsentLawfulBasisRecord.ProjectScopeRef"/> is not in the bounded
/// <see cref="ConsentLawfulBasisAuthorityView.AuthorizedProjectRefs"/>, it drops the subject locator + project ref
/// (and collapses the sensitivity to <c>metadata-only</c>), so an unauthorized read is indistinguishable from
/// safe-not-found — never the resource identity. The server <c>ConsentLawfulBasisAuthorizationPolicy</c> supplies the
/// bounded view; this function has no <c>ClaimsPrincipal</c> dependency.
/// </summary>
public static class ConsentLawfulBasisRedactionPolicy
{
    public static ConsentLawfulBasisRecord Redact(
        ConsentLawfulBasisRecord record,
        ConsentLawfulBasisAuthorityView authority)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(authority);

        bool authorized = authority.HasComplianceScope &&
            !string.IsNullOrEmpty(record.ProjectScopeRef) &&
            authority.AuthorizedProjectRefs.Contains(record.ProjectScopeRef);

        if (authorized)
        {
            return record;
        }

        // Unauthorized: drop the subject locator + project ref entirely; the redacted shape is the only signal (NFR2).
        return record with
        {
            SubjectLocator = string.Empty,
            ProjectScopeRef = string.Empty,
            RedactionSensitivity = DataClassRedactionSensitivities.MetadataOnly,
        };
    }
}
