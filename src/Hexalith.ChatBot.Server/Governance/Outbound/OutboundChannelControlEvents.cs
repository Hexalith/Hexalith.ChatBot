using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

/// <summary>
/// FR74/FR75d outbound-channel disable events. Mirrors the Story 7.21 command-capability disable pattern
/// (<c>CommandCapabilityDisablePendingApproval</c> → <c>CommandCapabilityDisabled</c>), retargeted from a
/// command-<em>type</em> subject to an outbound-<em>channel</em> subject: a first-person proposal records a pending
/// approval keyed by the disable-change id; a distinct second human policy-admin activates the durable
/// <see cref="OutboundChannelDisabled"/> control-state event. Carries safe, metadata-only tokens only — never
/// recipient/sender addresses, message content, credentials, OAuth grant fingerprints, model prompts/completions, or
/// delegated-user PII. The subject is the safe outbound-channel ref (<c>OutboundChannelRef</c> — the
/// <c>AdapterRef</c> token).
/// </summary>
public sealed record OutboundChannelDisablePendingApproval(
    string DisableChangeId,
    string TenantId,
    string OutboundChannelRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    OutboundChannelControlState OldState,
    OutboundChannelControlState NewState,
    DateTimeOffset RequestedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.outbound-channel-disable-pending-approval.v1") : IEventPayload;

/// <summary>
/// The activated FR74 control-state event recorded when a distinct second human policy-admin approves the disable.
/// Carries the actor (approver), scope (policy), subject (safe outbound-channel ref), reason, old/new state,
/// policy-snapshot id, and timestamp. Disable affects only future sends; existing drafts/approvals/send outcomes and
/// their audit trails stay inspectable.
/// </summary>
public sealed record OutboundChannelDisabled(
    string DisableChangeId,
    string TenantId,
    string OutboundChannelRef,
    string RequesterRef,
    string ApproverRef,
    string ReasonCode,
    string PolicySnapshotId,
    OutboundChannelControlState OldState,
    OutboundChannelControlState NewState,
    DateTimeOffset DisabledAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.outbound-channel-disabled.v1") : IEventPayload;

/// <summary>
/// Structured rejection for an invalid or unauthorized outbound-channel disable submission/approval. Carries only
/// safe tokens; a single-actor approval and a same-person approver both resolve here.
/// </summary>
public sealed record OutboundChannelDisableRejected(
    string DisableChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
