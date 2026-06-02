using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Mailbox;

/// <summary>
/// FR74/FR75d mailbox-source disable events. Mirrors the tenant-policy two-person pattern
/// (<c>TenantPolicyChangePendingApproval</c> → <c>TenantPolicySnapshotActivated</c>): a first-person proposal
/// records a pending approval keyed by the disable-change id; a distinct second human approver activates the
/// durable <see cref="MailboxSourceDisabled"/> control-state event. Carries safe, metadata-only tokens only.
/// </summary>
public sealed record MailboxSourceDisablePendingApproval(
    string DisableChangeId,
    string TenantId,
    string MailboxSourceRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    MailboxSourceControlState OldState,
    MailboxSourceControlState NewState,
    DateTimeOffset RequestedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.mailbox-source-disable-pending-approval.v1") : IEventPayload;

/// <summary>
/// The activated FR74 control-state event recorded when a distinct second human admin approves the disable.
/// Carries the actor (approver), scope (mailbox), subject (safe mailbox-source ref), reason, old/new state,
/// policy-snapshot id, and timestamp. Disable affects only future intake; existing records stay auditable.
/// </summary>
public sealed record MailboxSourceDisabled(
    string DisableChangeId,
    string TenantId,
    string MailboxSourceRef,
    string RequesterRef,
    string ApproverRef,
    string ReasonCode,
    string PolicySnapshotId,
    MailboxSourceControlState OldState,
    MailboxSourceControlState NewState,
    DateTimeOffset DisabledAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.mailbox-source-disabled.v1") : IEventPayload;

/// <summary>
/// Structured rejection for an invalid or unauthorized mailbox-source disable submission/approval. Carries
/// only safe tokens; a single-actor approval and a same-person approver both resolve here.
/// </summary>
public sealed record MailboxSourceDisableRejected(
    string DisableChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;

/// <summary>
/// FR74/FR75d mailbox-source quarantine pending-approval event (Story 7.13). Mirrors the disable triplet: a
/// first-person proposal records a pending approval keyed by the quarantine-change id; a distinct second human
/// approver activates the durable <see cref="MailboxSourceQuarantined"/> control-state event. Quarantine contains
/// new intake for review while existing records stay auditable. Carries safe, metadata-only tokens only.
/// </summary>
public sealed record MailboxSourceQuarantinePendingApproval(
    string QuarantineChangeId,
    string TenantId,
    string MailboxSourceRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    MailboxSourceControlState OldState,
    MailboxSourceControlState NewState,
    DateTimeOffset RequestedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.mailbox-source-quarantine-pending-approval.v1") : IEventPayload;

/// <summary>
/// The activated FR74 control-state event recorded when a distinct second human admin approves the quarantine.
/// Carries the actor (approver), scope (mailbox), subject (safe mailbox-source ref), reason, old/new state,
/// policy-snapshot id, and timestamp. Quarantine affects only future intake; existing records stay auditable.
/// </summary>
public sealed record MailboxSourceQuarantined(
    string QuarantineChangeId,
    string TenantId,
    string MailboxSourceRef,
    string RequesterRef,
    string ApproverRef,
    string ReasonCode,
    string PolicySnapshotId,
    MailboxSourceControlState OldState,
    MailboxSourceControlState NewState,
    DateTimeOffset QuarantinedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.mailbox-source-quarantined.v1") : IEventPayload;

/// <summary>
/// Structured rejection for an invalid or unauthorized mailbox-source quarantine submission/approval. Carries
/// only safe tokens; a single-actor approval and a same-person approver both resolve here.
/// </summary>
public sealed record MailboxSourceQuarantineRejected(
    string QuarantineChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;

/// <summary>
/// FR74/FR75 single-actor mailbox-source rate-limit configured event (Story 7.14). Unlike the disable/quarantine
/// two-person control events, rate-limit is a standard policy mutation that activates immediately on a single
/// authorized human mailbox-admin submission — there is no pending-approval event and no second handler. Records the
/// actor (requester), scope (mailbox), subject (safe mailbox-source ref), reason, old/new per-window budget, the
/// window dimension, policy-snapshot id, and timestamp. Rate-limit is a bounded parameter, not a control-state
/// transition: it never changes <c>MailboxSourceControlState</c> and affects only future intake throttling; existing
/// records stay auditable. Carries safe, metadata-only tokens only.
/// </summary>
public sealed record MailboxSourceRateLimitConfigured(
    string RateLimitChangeId,
    string TenantId,
    string MailboxSourceRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    int OldBudget,
    int NewBudget,
    MailboxRateLimitWindow Window,
    DateTimeOffset ConfiguredAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.mailbox-source-rate-limit-configured.v1") : IEventPayload;

/// <summary>
/// Structured rejection for an invalid or unauthorized mailbox-source rate-limit submission (including an
/// out-of-bounds budget). Carries only safe tokens.
/// </summary>
public sealed record MailboxSourceRateLimitRejected(
    string RateLimitChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
