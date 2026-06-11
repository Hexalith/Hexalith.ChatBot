using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Policy;

public sealed record NotificationRoutingSnapshotActivated(
    string RoutingChangeId,
    string TenantId,
    string SupersededRoutingSnapshotId,
    string ActivatedRoutingSnapshotId,
    NotificationRoutingChangeSet ChangeSet,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string OldRoutingFingerprint,
    string NewRoutingFingerprint,
    DateTimeOffset ActivatedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.notification-routing-snapshot-activated.v1") : IEventPayload;

public sealed record NotificationRoutingChangeRejected(
    string RoutingChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
