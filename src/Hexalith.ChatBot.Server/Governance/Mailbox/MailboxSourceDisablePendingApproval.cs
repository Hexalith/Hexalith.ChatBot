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
