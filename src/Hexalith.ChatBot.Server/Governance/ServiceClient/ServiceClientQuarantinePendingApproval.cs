using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.ServiceClient;

/// <summary>
/// FR74/FR75d service-client quarantine events. Mirrors the service-client disable triplet (Story 7.15) and the
/// Story 7.13 mailbox-source quarantine substitution: a first-person proposal records a pending approval keyed by
/// the quarantine-change id; a distinct second human approver activates the durable
/// <see cref="ServiceClientQuarantined"/> control-state event (Active→Quarantined). Carries safe, metadata-only
/// tokens only — never service-client credentials, OAuth grant fingerprints, or delegated-user PII.
/// </summary>
public sealed record ServiceClientQuarantinePendingApproval(
    string QuarantineChangeId,
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
    string SchemaVersion = "chatbot.service-client-quarantine-pending-approval.v1") : IEventPayload;
