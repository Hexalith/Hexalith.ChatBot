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
