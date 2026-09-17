using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

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
