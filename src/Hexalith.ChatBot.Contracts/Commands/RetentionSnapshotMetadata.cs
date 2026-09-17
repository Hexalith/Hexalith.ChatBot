using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record RetentionSnapshotMetadata(
    string SnapshotId,
    string SchemaVersion,
    string SupersedesSnapshotId,
    string SupersededBySnapshotId,
    string SourceChangeId,
    string ActorRef,
    AdminScope ScopeUsed,
    IReadOnlyList<string> ChangedRetentionClassIds,
    long SourceVersion,
    DateTimeOffset EffectiveAtUtc,
    string CorrelationId,
    string ReasonCode,
    string PolicySnapshotId,
    string OldSnapshotFingerprint,
    string NewSnapshotFingerprint);
