using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The NFR35 policy-snapshot metadata for an inventory change. Mirrors <see cref="RetentionSnapshotMetadata"/>
/// field-for-field, replacing <c>ChangedRetentionClassIds</c> with <see cref="ChangedDataClassIds"/> and using
/// <see cref="AdminScope.Compliance"/> for <see cref="ScopeUsed"/>. Old/new values are <c>sha256:</c> fingerprints,
/// never raw inventory values.
/// </summary>
public sealed record DataClassInventorySnapshotMetadata(
    string SnapshotId,
    string SchemaVersion,
    string SupersedesSnapshotId,
    string SupersededBySnapshotId,
    string SourceChangeId,
    string ActorRef,
    AdminScope ScopeUsed,
    IReadOnlyList<string> ChangedDataClassIds,
    long SourceVersion,
    DateTimeOffset EffectiveAtUtc,
    string CorrelationId,
    string ReasonCode,
    string PolicySnapshotId,
    string OldSnapshotFingerprint,
    string NewSnapshotFingerprint);
