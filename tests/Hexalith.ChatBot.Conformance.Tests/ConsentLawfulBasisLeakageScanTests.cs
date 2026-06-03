using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Conformance.Tests.Harness;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

/// <summary>
/// Story 9.10 (NFR2/NFR42) no-leak floor: the consent/lawful-basis recording command, the record, the snapshot
/// metadata, and the redacted record are metadata-only by construction (safe tokens + sha256 fingerprints + an opaque
/// subject locator). Serializing them and routing the rendered JSON through the shared cross-tenant leakage scanner
/// must surface no foreign-tenant (or any other corpus-class) sentinel. Mirrors <c>DeletionErasureLeakageScanTests</c>.
/// The scope token is the neutral, non-sentinel <c>tenant-consent-owner</c> (not the Story 1.12 corpus
/// <c>tenant-alpha</c> boundary).
/// </summary>
public sealed class ConsentLawfulBasisLeakageScanTests
{
    [Fact]
    public void ConsentContractSerializationCarriesNoCrossTenantSentinel()
    {
        ConsentLawfulBasisSnapshotMetadata snapshot = new(
            "consent-snapshot-001",
            ConsentLawfulBasisSchemaVersions.V1,
            "consent-snapshot-current",
            "consent-snapshot-next",
            "consent-record-001",
            "admin-requester",
            AdminScope.Compliance,
            [ConsentSubjectKinds.ExternalParticipant, ConsentSubjectKinds.AiProcessing],
            8,
            new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.Zero),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "consent-lawful-basis-request",
            "policy-snapshot-admin-v1",
            "sha256:oldconsentfingerprint001",
            "sha256:newconsentfingerprint001");

        SubmitConsentLawfulBasisRecord request = new(
            "consent-record-001",
            8,
            ConsentSubjectKinds.ExternalParticipant,
            "subject-locator-001",
            "tenant-consent-owner",
            ConsentLawfulBases.Consent,
            ConsentRecordStatuses.Active,
            "basis-source-dpia-001",
            DataClassRedactionSensitivities.Restricted,
            "consent-lawful-basis-request",
            "admin-requester",
            ConsentLawfulBasisSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:consentrecordfingerprint001",
            new DateTimeOffset(2026, 6, 3, 4, 0, 0, TimeSpan.Zero));

        ConsentLawfulBasisRecord record = new(
            request.RecordId,
            request.SubjectKind,
            request.SubjectLocator,
            request.ProjectScopeRef,
            request.LawfulBasis,
            request.RecordStatus,
            request.BasisSource,
            request.RedactionSensitivity,
            request.EffectiveAtUtc,
            request.RecordFingerprint);

        // The redacted (unauthorized) projection drops the locator + project ref entirely.
        ConsentLawfulBasisRecord redacted = ConsentLawfulBasisRedactionPolicy.Redact(
            record,
            new ConsentLawfulBasisAuthorityView(true, new HashSet<string>(StringComparer.Ordinal)));

        string rendered = JsonSerializer.Serialize(
            new { request, record, redacted, snapshot },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Should.NotThrow(() =>
            CrossTenantLeakageScanner.ScanAll("compliance-admin", "tenant-consent", rendered));
    }
}
