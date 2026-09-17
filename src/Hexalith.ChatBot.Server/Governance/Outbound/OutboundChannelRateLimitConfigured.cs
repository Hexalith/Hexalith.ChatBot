using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

/// <summary>
/// FR74/FR75 single-actor outbound-channel rate-limit configured event (Story 7.26). Unlike the disable/quarantine
/// two-person control events, rate-limit is a standard policy mutation that activates immediately on a single
/// authorized human policy-admin submission — there is no pending-approval event and no second handler. Records the
/// actor (requester), scope (policy), subject (safe outbound-channel ref — the <c>AdapterRef</c> token), reason,
/// old/new per-window send budget, the window dimension, policy-snapshot id, and timestamp. Rate-limit is a bounded
/// parameter, not a control-state transition: it never changes <see cref="Contracts.Enums.OutboundChannelControlState"/>
/// and affects only future send-seam throttling; existing drafts/approvals/send outcomes stay inspectable. Carries
/// safe, metadata-only tokens only. Mirrors <c>CommandCapabilityRateLimitConfigured</c>.
/// </summary>
public sealed record OutboundChannelRateLimitConfigured(
    string RateLimitChangeId,
    string TenantId,
    string OutboundChannelRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    int OldBudget,
    int NewBudget,
    OutboundChannelRateLimitWindow Window,
    DateTimeOffset ConfiguredAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.outbound-channel-rate-limit-configured.v1") : IEventPayload;
