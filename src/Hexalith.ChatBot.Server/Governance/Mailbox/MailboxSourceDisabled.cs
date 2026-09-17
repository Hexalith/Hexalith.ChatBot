using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Mailbox;

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
