using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Mailbox;

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
