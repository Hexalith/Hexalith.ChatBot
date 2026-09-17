using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record NotificationRoutingSnapshotMetadata(
    string SnapshotId,
    string SchemaVersion,
    string SupersedesSnapshotId,
    string SourceChangeId,
    string ActorRef,
    AdminScope ScopeUsed,
    IReadOnlyList<string> ChangedRouteKeys,
    string SourceVersion,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string ReasonCode,
    string RoutingFingerprint);
