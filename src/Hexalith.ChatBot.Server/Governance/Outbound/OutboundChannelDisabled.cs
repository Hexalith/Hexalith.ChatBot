using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

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
