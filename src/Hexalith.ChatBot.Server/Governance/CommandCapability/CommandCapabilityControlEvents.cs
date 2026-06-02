using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.CommandCapability;

/// <summary>
/// FR74/FR75d command-capability disable events. Mirrors the Story 7.18 AI-actor disable pattern
/// (<c>AiActorDisablePendingApproval</c> → <c>AiActorDisabled</c>), retargeted from an actor subject to a
/// command-<em>type</em> subject: a first-person proposal records a pending approval keyed by the disable-change
/// id; a distinct second human policy-admin activates the durable <see cref="CommandCapabilityDisabled"/>
/// control-state event. Carries safe, metadata-only tokens only — never credentials, OAuth grant fingerprints,
/// model prompts/completions, or delegated-user PII. The subject is the safe command type name
/// (<c>CommandCapabilityRef</c>).
/// </summary>
public sealed record CommandCapabilityDisablePendingApproval(
    string DisableChangeId,
    string TenantId,
    string CommandCapabilityRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    CommandCapabilityControlState OldState,
    CommandCapabilityControlState NewState,
    DateTimeOffset RequestedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.command-capability-disable-pending-approval.v1") : IEventPayload;

/// <summary>
/// The activated FR74 control-state event recorded when a distinct second human policy-admin approves the
/// disable. Carries the actor (approver), scope (policy), subject (safe command-capability ref), reason, old/new
/// state, policy-snapshot id, and timestamp. Disable affects only future admission; existing records stay
/// auditable.
/// </summary>
public sealed record CommandCapabilityDisabled(
    string DisableChangeId,
    string TenantId,
    string CommandCapabilityRef,
    string RequesterRef,
    string ApproverRef,
    string ReasonCode,
    string PolicySnapshotId,
    CommandCapabilityControlState OldState,
    CommandCapabilityControlState NewState,
    DateTimeOffset DisabledAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.command-capability-disabled.v1") : IEventPayload;

/// <summary>
/// Structured rejection for an invalid or unauthorized command-capability disable submission/approval. Carries
/// only safe tokens; a single-actor approval and a same-person approver both resolve here.
/// </summary>
public sealed record CommandCapabilityDisableRejected(
    string DisableChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;

/// <summary>
/// FR74/FR75d command-capability quarantine pending-approval event. Mirrors
/// <see cref="CommandCapabilityDisablePendingApproval"/> (Story 7.21) with the quarantine control-state
/// substituted for disable (the Story 7.19 disable→quarantine precedent): a first-person proposal records a
/// pending approval keyed by the quarantine-change id; a distinct second human policy-admin activates the durable
/// <see cref="CommandCapabilityQuarantined"/> control-state event. Carries safe, metadata-only tokens only — never
/// credentials, OAuth grant fingerprints, model prompts/completions, or delegated-user PII. The subject is the
/// safe command type name (<c>CommandCapabilityRef</c>).
/// </summary>
public sealed record CommandCapabilityQuarantinePendingApproval(
    string QuarantineChangeId,
    string TenantId,
    string CommandCapabilityRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    CommandCapabilityControlState OldState,
    CommandCapabilityControlState NewState,
    DateTimeOffset RequestedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.command-capability-quarantine-pending-approval.v1") : IEventPayload;

/// <summary>
/// The activated FR74 control-state event recorded when a distinct second human policy-admin approves the
/// quarantine. Carries the actor (approver), scope (policy), subject (safe command-capability ref), reason,
/// old/new state, policy-snapshot id, and timestamp. Quarantine affects only future admission; existing records
/// stay auditable.
/// </summary>
public sealed record CommandCapabilityQuarantined(
    string QuarantineChangeId,
    string TenantId,
    string CommandCapabilityRef,
    string RequesterRef,
    string ApproverRef,
    string ReasonCode,
    string PolicySnapshotId,
    CommandCapabilityControlState OldState,
    CommandCapabilityControlState NewState,
    DateTimeOffset QuarantinedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.command-capability-quarantined.v1") : IEventPayload;

/// <summary>
/// Structured rejection for an invalid or unauthorized command-capability quarantine submission/approval. Carries
/// only safe tokens; a single-actor approval and a same-person approver both resolve here.
/// </summary>
public sealed record CommandCapabilityQuarantineRejected(
    string QuarantineChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;

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

/// <summary>
/// Structured rejection for an invalid or unauthorized command-capability rate-limit submission (including an
/// out-of-bounds budget). Carries only safe tokens.
/// </summary>
public sealed record CommandCapabilityRateLimitRejected(
    string RateLimitChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
