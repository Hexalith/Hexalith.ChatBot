using Hexalith.ChatBot.Contracts.Commands;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Story 9.10 contracts coverage: the five closed token sets, the <see cref="ConsentRequirementMatrix.Published"/>
/// bijection, the pure <see cref="ConsentRequirementPolicy.Evaluate"/> / <see cref="ConsentGate.Evaluate"/> fail-closed
/// decisions (AC4), the <see cref="ConsentLawfulBasisSchema"/> accept/reject invariants (AC1), and the per-project
/// <see cref="ConsentLawfulBasisRedactionPolicy.Redact"/> no-leak redaction (AC2). Mirrors the
/// <c>TenantExportContractTests</c> style (round-trips + closed-set membership + Shouldly).
/// </summary>
public static class ConsentLawfulBasisContractTests
{
    private static readonly DateTimeOffset RecordedAt = new(2026, 6, 3, 4, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ConsentSubjectKinds.ExternalParticipant)]
    [InlineData(ConsentSubjectKinds.RetainedContent)]
    [InlineData(ConsentSubjectKinds.Attachment)]
    [InlineData(ConsentSubjectKinds.AiProcessing)]
    public static void SubjectKindsShouldBeAClosedSet(string value)
    {
        ConsentSubjectKinds.Contains(value).ShouldBeTrue();
        ConsentSubjectKinds.Contains("unknown-subject").ShouldBeFalse();
        ConsentSubjectKinds.Contains(null).ShouldBeFalse();
    }

    [Theory]
    [InlineData(ConsentLawfulBases.Consent)]
    [InlineData(ConsentLawfulBases.Contract)]
    [InlineData(ConsentLawfulBases.LegalObligation)]
    [InlineData(ConsentLawfulBases.VitalInterests)]
    [InlineData(ConsentLawfulBases.PublicTask)]
    [InlineData(ConsentLawfulBases.LegitimateInterests)]
    public static void LawfulBasesShouldBeAClosedSet(string value)
    {
        ConsentLawfulBases.Contains(value).ShouldBeTrue();
        ConsentLawfulBases.Contains("vibes").ShouldBeFalse();
    }

    [Theory]
    [InlineData(ConsentRecordStatuses.Active)]
    [InlineData(ConsentRecordStatuses.Withdrawn)]
    [InlineData(ConsentRecordStatuses.Expired)]
    [InlineData(ConsentRecordStatuses.Superseded)]
    public static void RecordStatusesShouldBeAClosedSet(string value)
    {
        ConsentRecordStatuses.Contains(value).ShouldBeTrue();
        ConsentRecordStatuses.Contains("pending").ShouldBeFalse();
    }

    [Theory]
    [InlineData(ConsentRequirementDispositions.Required)]
    [InlineData(ConsentRequirementDispositions.NotRequired)]
    public static void RequirementDispositionsShouldBeAClosedSet(string value)
    {
        ConsentRequirementDispositions.Contains(value).ShouldBeTrue();
        ConsentRequirementDispositions.Contains("maybe").ShouldBeFalse();
    }

    [Theory]
    [InlineData(ConsentGateDecisions.Satisfied)]
    [InlineData(ConsentGateDecisions.BlockedMissingBasis)]
    public static void GateDecisionsShouldBeAClosedSet(string value)
    {
        ConsentGateDecisions.Contains(value).ShouldBeTrue();
        ConsentGateDecisions.Contains("allowed").ShouldBeFalse();
    }

    [Fact]
    public static void SchemaVersionsIsKnownShouldRecognizeOnlyTheShippedVersion()
    {
        ConsentLawfulBasisSchemaVersions.IsKnown(ConsentLawfulBasisSchemaVersions.V1).ShouldBeTrue();
        ConsentLawfulBasisSchemaVersions.IsKnown("consent-lawful-basis-schema.custom").ShouldBeFalse();
        ConsentLawfulBasisSchemaVersions.IsKnown(null).ShouldBeFalse();
    }

    [Fact]
    public static void PublishedMatrixShouldBeACompleteBijectionOverSubjectKinds()
    {
        ConsentRequirementProfile profile = ConsentRequirementMatrix.Published;

        // Every subject kind is declared exactly once with a valid disposition (the AC1 completeness invariant).
        profile.DispositionsBySubjectKind.Count.ShouldBe(ConsentSubjectKinds.All.Count);
        foreach (string subjectKind in ConsentSubjectKinds.All)
        {
            profile.DispositionsBySubjectKind.ShouldContainKey(subjectKind);
            ConsentRequirementDispositions.Contains(profile.DispositionsBySubjectKind[subjectKind]).ShouldBeTrue();
        }

        ConsentLawfulBasisSchema.ValidateRequirementProfile(profile).IsValid.ShouldBeTrue();
    }

    [Fact]
    public static void RequirementPolicyShouldBiasUnknownAndMissingToRequired()
    {
        ConsentRequirementProfile profile = ConsentRequirementMatrix.Published;

        // A seeded-required kind resolves to required.
        ConsentRequirementPolicy.Evaluate(ConsentSubjectKinds.AiProcessing, profile)
            .ShouldBe(ConsentRequirementDispositions.Required);

        // AC4 fail-closed: an UNKNOWN subject kind biases to required even with a valid profile.
        ConsentRequirementPolicy.Evaluate("unknown-subject", profile)
            .ShouldBe(ConsentRequirementDispositions.Required);

        // AC4 fail-closed: a missing/empty profile entry biases to required.
        ConsentRequirementProfile empty = new(new Dictionary<string, string>(StringComparer.Ordinal));
        ConsentRequirementPolicy.Evaluate(ConsentSubjectKinds.Attachment, empty)
            .ShouldBe(ConsentRequirementDispositions.Required);

        // A profile that explicitly relaxes a kind returns the seeded value.
        ConsentRequirementProfile relaxed = new(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ConsentSubjectKinds.Attachment] = ConsentRequirementDispositions.NotRequired,
        });
        ConsentRequirementPolicy.Evaluate(ConsentSubjectKinds.Attachment, relaxed)
            .ShouldBe(ConsentRequirementDispositions.NotRequired);
    }

    [Theory]
    [InlineData(ConsentRequirementDispositions.NotRequired, null, ConsentGateDecisions.Satisfied)]
    [InlineData(ConsentRequirementDispositions.NotRequired, ConsentRecordStatuses.Active, ConsentGateDecisions.Satisfied)]
    [InlineData(ConsentRequirementDispositions.Required, ConsentRecordStatuses.Active, ConsentGateDecisions.Satisfied)]
    [InlineData(ConsentRequirementDispositions.Required, null, ConsentGateDecisions.BlockedMissingBasis)]
    [InlineData(ConsentRequirementDispositions.Required, ConsentRecordStatuses.Withdrawn, ConsentGateDecisions.BlockedMissingBasis)]
    [InlineData(ConsentRequirementDispositions.Required, ConsentRecordStatuses.Expired, ConsentGateDecisions.BlockedMissingBasis)]
    [InlineData(ConsentRequirementDispositions.Required, ConsentRecordStatuses.Superseded, ConsentGateDecisions.BlockedMissingBasis)]
    [InlineData("unknown-disposition", ConsentRecordStatuses.Active, ConsentGateDecisions.BlockedMissingBasis)]
    public static void GateShouldFailClosedUnlessRequiredIsBackedByAnActiveBasis(
        string disposition,
        string? activeRecordStatus,
        string expected)
        => ConsentGate.Evaluate(disposition, activeRecordStatus).ShouldBe(expected);

    [Fact]
    public static void ValidateRecordShouldAcceptAWellFormedRecord()
        => ConsentLawfulBasisSchema.ValidateRecord(ValidRecord()).IsValid.ShouldBeTrue();

    [Fact]
    public static void ValidateRecordShouldRejectInvalidTokensAndFingerprints()
    {
        ConsentLawfulBasisSchema.ValidateRecord(ValidRecord() with { SubjectKind = "subject-x" })
            .Errors.ShouldContain("consent_subject_kind_invalid");
        ConsentLawfulBasisSchema.ValidateRecord(ValidRecord() with { LawfulBasis = "because" })
            .Errors.ShouldContain("consent_lawful_basis_invalid");
        ConsentLawfulBasisSchema.ValidateRecord(ValidRecord() with { RecordStatus = "pending" })
            .Errors.ShouldContain("consent_record_status_invalid");
        ConsentLawfulBasisSchema.ValidateRecord(ValidRecord() with { RedactionSensitivity = "top-secret" })
            .Errors.ShouldContain("consent_redaction_sensitivity_invalid");
        ConsentLawfulBasisSchema.ValidateRecord(ValidRecord() with { RecordFingerprint = "not-a-fingerprint" })
            .Errors.ShouldContain("consent_record_fingerprint_invalid");
    }

    [Fact]
    public static void ValidateRecordShouldRejectNullUnsafeIdentifiersAndNonUtcTimestamps()
    {
        // A null record collapses to the single safe-not-found result code.
        ConsentLawfulBasisSchema.ValidateRecord(null).Errors.ShouldContain("consent_record_invalid");

        // Unsafe (space/special-char) tokens fail the IsSafeComplianceToken floor on each identifier field.
        ConsentLawfulBasisSchema.ValidateRecord(ValidRecord() with { RecordId = "bad id!" })
            .Errors.ShouldContain("consent_record_id_invalid");
        ConsentLawfulBasisSchema.ValidateRecord(ValidRecord() with { SubjectLocator = "raw subject!" })
            .Errors.ShouldContain("consent_subject_locator_invalid");
        ConsentLawfulBasisSchema.ValidateRecord(ValidRecord() with { ProjectScopeRef = "bad project!" })
            .Errors.ShouldContain("consent_project_scope_ref_invalid");
        ConsentLawfulBasisSchema.ValidateRecord(ValidRecord() with { BasisSource = "bad source!" })
            .Errors.ShouldContain("consent_basis_source_invalid");

        // A non-UTC (non-zero offset) recorded timestamp is rejected (IsUtc requires Offset == Zero).
        ConsentLawfulBasisSchema.ValidateRecord(
                ValidRecord() with { RecordedAtUtc = new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.FromHours(2)) })
            .Errors.ShouldContain("consent_recorded_at_invalid");
    }

    [Fact]
    public static void ValidateRequirementProfileShouldRejectNullEmptyAndMalformedEntries()
    {
        // Null / empty profiles collapse to the single invalid-profile code.
        ConsentLawfulBasisSchema.ValidateRequirementProfile(null)
            .Errors.ShouldContain("consent_requirement_profile_invalid");
        ConsentLawfulBasisSchema.ValidateRequirementProfile(
                new ConsentRequirementProfile(new Dictionary<string, string>(StringComparer.Ordinal)))
            .Errors.ShouldContain("consent_requirement_profile_invalid");

        // An out-of-set key is flagged as an invalid subject kind.
        ConsentRequirementProfile badKey = new(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["not-a-subject-kind"] = ConsentRequirementDispositions.Required,
        });
        ConsentLawfulBasisSchema.ValidateRequirementProfile(badKey)
            .Errors.ShouldContain("consent_requirement_subject_kind_invalid");

        // A complete profile with an out-of-set disposition value is flagged as an invalid disposition.
        ConsentRequirementProfile badValue = new(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ConsentSubjectKinds.ExternalParticipant] = ConsentRequirementDispositions.Required,
            [ConsentSubjectKinds.RetainedContent] = ConsentRequirementDispositions.Required,
            [ConsentSubjectKinds.Attachment] = ConsentRequirementDispositions.Required,
            [ConsentSubjectKinds.AiProcessing] = "maybe",
        });
        ConsentLawfulBasisSchema.ValidateRequirementProfile(badValue)
            .Errors.ShouldContain("consent_requirement_disposition_invalid");
    }

    [Fact]
    public static void ValidateRequirementProfileShouldRejectAnIncompleteProfile()
    {
        ConsentRequirementProfile incomplete = new(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ConsentSubjectKinds.ExternalParticipant] = ConsentRequirementDispositions.Required,
            [ConsentSubjectKinds.RetainedContent] = ConsentRequirementDispositions.Required,
            [ConsentSubjectKinds.Attachment] = ConsentRequirementDispositions.Required,
            // ai-processing intentionally omitted ⇒ incomplete bijection.
        });

        RetentionValidationResult result = ConsentLawfulBasisSchema.ValidateRequirementProfile(incomplete);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("consent_requirement_profile_incomplete");
    }

    [Fact]
    public static void RedactShouldDropSubjectLocatorAndProjectRefForUnauthorizedProject()
    {
        ConsentLawfulBasisRecord record = ValidRecord();

        // Authorized project ⇒ record preserved verbatim.
        ConsentLawfulBasisAuthorityView authorized = new(
            true, new HashSet<string>(["project-consent-001"], StringComparer.Ordinal));
        ConsentLawfulBasisRecord kept = ConsentLawfulBasisRedactionPolicy.Redact(record, authorized);
        kept.SubjectLocator.ShouldBe("subject-locator-001");
        kept.ProjectScopeRef.ShouldBe("project-consent-001");

        // AC2/NFR2: an unauthorized project drops the locator + project ref — indistinguishable from safe-not-found.
        ConsentLawfulBasisAuthorityView unauthorized = new(
            true, new HashSet<string>(["project-other-009"], StringComparer.Ordinal));
        ConsentLawfulBasisRecord redacted = ConsentLawfulBasisRedactionPolicy.Redact(record, unauthorized);
        redacted.SubjectLocator.ShouldBeEmpty();
        redacted.ProjectScopeRef.ShouldBeEmpty();
        redacted.RedactionSensitivity.ShouldBe(DataClassRedactionSensitivities.MetadataOnly);

        // No compliance scope ⇒ also redacted.
        ConsentLawfulBasisAuthorityView noScope = new(
            false, new HashSet<string>(["project-consent-001"], StringComparer.Ordinal));
        ConsentLawfulBasisRedactionPolicy.Redact(record, noScope).SubjectLocator.ShouldBeEmpty();
    }

    private static ConsentLawfulBasisRecord ValidRecord()
        => new(
            "consent-record-001",
            ConsentSubjectKinds.ExternalParticipant,
            "subject-locator-001",
            "project-consent-001",
            ConsentLawfulBases.Consent,
            ConsentRecordStatuses.Active,
            "basis-source-dpia-001",
            DataClassRedactionSensitivities.Restricted,
            RecordedAt,
            "sha256:consentrecordfingerprint001");
}
