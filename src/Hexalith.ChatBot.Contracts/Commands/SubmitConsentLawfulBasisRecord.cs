using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

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
