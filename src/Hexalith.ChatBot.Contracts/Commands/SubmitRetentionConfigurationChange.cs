using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record SubmitRetentionConfigurationChange(
    string RetentionChangeId,
    string SourceRetentionSnapshotId,
    string ProposedRetentionSnapshotId,
    long SourceVersion,
    RetentionConfigurationChangeSet ChangeSet,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string PolicySnapshotId,
    string OldRetentionSnapshotFingerprint,
    string NewRetentionSnapshotFingerprint,
    DateTimeOffset EffectiveAtUtc) : IChatBotCommand;
