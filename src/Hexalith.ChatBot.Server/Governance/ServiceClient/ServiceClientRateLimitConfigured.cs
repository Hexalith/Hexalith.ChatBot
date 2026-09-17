using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.ServiceClient;

/// <summary>
/// FR74/FR75 single-actor service-client rate-limit configured event (Story 7.17). Unlike the disable/quarantine
/// two-person control events, rate-limit is a standard policy mutation that activates immediately on a single
/// authorized human tenant-admin submission — there is no pending-approval event and no second handler. Records the
/// actor (requester), scope (tenant-admin), subject (safe service-client ref), reason, old/new per-window command
/// budget, the window dimension, policy-snapshot id, and timestamp. Rate-limit is a bounded parameter, not a
/// control-state transition: it never changes <c>ServiceClientControlState</c> and affects only future command
/// admission throttling; existing records stay auditable. Carries safe, metadata-only tokens only.
/// </summary>
public sealed record ServiceClientRateLimitConfigured(
    string RateLimitChangeId,
    string TenantId,
    string ServiceClientRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    int OldBudget,
    int NewBudget,
    ServiceClientRateLimitWindow Window,
    DateTimeOffset ConfiguredAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.service-client-rate-limit-configured.v1") : IEventPayload;
