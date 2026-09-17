using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Mailbox;

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
