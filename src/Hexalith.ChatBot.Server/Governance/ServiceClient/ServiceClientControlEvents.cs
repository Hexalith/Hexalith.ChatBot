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

/// <summary>
/// The activated FR74 control-state event recorded when a distinct second human tenant-admin approves the
/// disable. Carries the actor (approver), scope (tenant-admin), subject (safe service-client ref), reason,
/// old/new state, policy-snapshot id, and timestamp. Disable affects only future admission; existing records
/// stay auditable.
/// </summary>
public sealed record ServiceClientDisabled(
    string DisableChangeId,
    string TenantId,
    string ServiceClientRef,
    string RequesterRef,
    string ApproverRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    DateTimeOffset DisabledAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.service-client-disabled.v1") : IEventPayload;

/// <summary>
/// Structured rejection for an invalid or unauthorized service-client disable submission/approval. Carries
/// only safe tokens; a single-actor approval and a same-person approver both resolve here.
/// </summary>
public sealed record ServiceClientDisableRejected(
    string DisableChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;

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

/// <summary>
/// The activated FR74 control-state event recorded when a distinct second human tenant-admin approves the
/// quarantine. Carries the actor (approver), scope (tenant-admin), subject (safe service-client ref), reason,
/// old/new state, policy-snapshot id, and timestamp. Quarantine affects only future admission; existing records
/// stay auditable.
/// </summary>
public sealed record ServiceClientQuarantined(
    string QuarantineChangeId,
    string TenantId,
    string ServiceClientRef,
    string RequesterRef,
    string ApproverRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    DateTimeOffset QuarantinedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.service-client-quarantined.v1") : IEventPayload;

/// <summary>
/// Structured rejection for an invalid or unauthorized service-client quarantine submission/approval. Carries
/// only safe tokens; a single-actor approval and a same-person approver both resolve here.
/// </summary>
public sealed record ServiceClientQuarantineRejected(
    string QuarantineChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;

/// <summary>
/// FR74/FR75 single-actor service-client rate-limit configured event (Story 7.17). Unlike the disable/quarantine
/// two-person control events, rate-limit is a standard policy mutation that activates immediately on a single
/// authorized human tenant-admin submission — there is no pending-approval event and no second handler. Records the
/// actor (requester), scope (tenant-admin), subject (safe service-client ref), reason, old/new per-window command
/// budget, the window dimension, policy-snapshot id, and timestamp. Rate-limit is a bounded parameter, not a
/// control-state transition: it never changes <c>ServiceClientControlState</c> and affects only future command
/// admission throttling; existing records stay auditable. Carries safe, metadata-only tokens only.
/// </summary>
public sealed record ServiceClientRateLimitConfigured(
    string RateLimitChangeId,
    string TenantId,
    string ServiceClientRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    int OldBudget,
    int NewBudget,
    ServiceClientRateLimitWindow Window,
    DateTimeOffset ConfiguredAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.service-client-rate-limit-configured.v1") : IEventPayload;

/// <summary>
/// Structured rejection for an invalid or unauthorized service-client rate-limit submission (including an
/// out-of-bounds budget). Carries only safe tokens.
/// </summary>
public sealed record ServiceClientRateLimitRejected(
    string RateLimitChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
