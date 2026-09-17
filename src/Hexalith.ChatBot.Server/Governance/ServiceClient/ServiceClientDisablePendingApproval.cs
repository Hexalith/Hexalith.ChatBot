using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.ServiceClient;

/// <summary>
/// FR74/FR75d service-client disable events. Mirrors the Story 7.12 mailbox-source disable pattern
/// (<c>MailboxSourceDisablePendingApproval</c> → <c>MailboxSourceDisabled</c>): a first-person proposal
/// records a pending approval keyed by the disable-change id; a distinct second human approver activates the
/// durable <see cref="ServiceClientDisabled"/> control-state event. Carries safe, metadata-only tokens only —
/// never service-client credentials, OAuth grant fingerprints, or delegated-user PII.
/// </summary>
public sealed record ServiceClientDisablePendingApproval(
    string DisableChangeId,
    string TenantId,
    string ServiceClientRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    DateTimeOffset RequestedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.service-client-disable-pending-approval.v1") : IEventPayload;
