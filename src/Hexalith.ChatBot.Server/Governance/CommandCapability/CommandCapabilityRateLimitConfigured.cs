using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.CommandCapability;

/// <summary>
/// FR74/FR75 single-actor command-capability rate-limit configured event (Story 7.23). Unlike the
/// disable/quarantine two-person control events, rate-limit is a standard policy mutation that activates
/// immediately on a single authorized human policy-admin submission — there is no pending-approval event and no
/// second handler. Records the actor (requester), scope (policy), subject (safe command-capability ref — the
/// command type name), reason, old/new per-window command budget, the window dimension, policy-snapshot id, and
/// timestamp. Rate-limit is a bounded parameter, not a control-state transition: it never changes
/// <c>CommandCapabilityControlState</c> and affects only future command-admission throttling; existing records
/// stay auditable. Carries safe, metadata-only tokens only. Mirrors <c>AiActorRateLimitConfigured</c>.
/// </summary>
public sealed record CommandCapabilityRateLimitConfigured(
    string RateLimitChangeId,
    string TenantId,
    string CommandCapabilityRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    int OldBudget,
    int NewBudget,
    CommandCapabilityRateLimitWindow Window,
    DateTimeOffset ConfiguredAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.command-capability-rate-limit-configured.v1") : IEventPayload;
