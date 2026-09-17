using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

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
