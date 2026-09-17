using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiActor;

/// <summary>
/// FR74/FR75 single-actor AI-actor rate-limit configured event (Story 7.20). Unlike the disable/quarantine
/// two-person control events, rate-limit is a standard policy mutation that activates immediately on a single
/// authorized human policy-admin submission — there is no pending-approval event and no second handler. Records the
/// actor (requester), scope (policy), subject (safe AI-actor ref), reason, old/new per-window proposal budget, the
/// window dimension, policy-snapshot id, and timestamp. Rate-limit is a bounded parameter, not a control-state
/// transition: it never changes <c>AiActorControlState</c> and affects only future command/proposal admission
/// throttling; existing records stay auditable. Carries safe, metadata-only tokens only. Mirrors
/// <c>ServiceClientRateLimitConfigured</c>.
/// </summary>
public sealed record AiActorRateLimitConfigured(
    string RateLimitChangeId,
    string TenantId,
    string AiActorRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    int OldBudget,
    int NewBudget,
    AiActorRateLimitWindow Window,
    DateTimeOffset ConfiguredAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.ai-actor-rate-limit-configured.v1") : IEventPayload;
