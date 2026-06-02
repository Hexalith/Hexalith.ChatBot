using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiActor;

/// <summary>
/// FR74/FR75d AI-actor disable events. Mirrors the Story 7.15 service-client disable pattern
/// (<c>ServiceClientDisablePendingApproval</c> → <c>ServiceClientDisabled</c>): a first-person proposal records
/// a pending approval keyed by the disable-change id; a distinct second human policy-admin activates the
/// durable <see cref="AiActorDisabled"/> control-state event. Carries safe, metadata-only tokens only — never
/// service-client/AI credentials, OAuth grant fingerprints, model prompts/completions, or delegated-user PII.
/// </summary>
public sealed record AiActorDisablePendingApproval(
    string DisableChangeId,
    string TenantId,
    string AiActorRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    AiActorControlState OldState,
    AiActorControlState NewState,
    DateTimeOffset RequestedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.ai-actor-disable-pending-approval.v1") : IEventPayload;

/// <summary>
/// The activated FR74 control-state event recorded when a distinct second human policy-admin approves the
/// disable. Carries the actor (approver), scope (policy), subject (safe AI-actor ref), reason, old/new state,
/// policy-snapshot id, and timestamp. Disable affects only future admission; existing records stay auditable.
/// </summary>
public sealed record AiActorDisabled(
    string DisableChangeId,
    string TenantId,
    string AiActorRef,
    string RequesterRef,
    string ApproverRef,
    string ReasonCode,
    string PolicySnapshotId,
    AiActorControlState OldState,
    AiActorControlState NewState,
    DateTimeOffset DisabledAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.ai-actor-disabled.v1") : IEventPayload;

/// <summary>
/// Structured rejection for an invalid or unauthorized AI-actor disable submission/approval. Carries only safe
/// tokens; a single-actor approval and a same-person approver both resolve here.
/// </summary>
public sealed record AiActorDisableRejected(
    string DisableChangeId,
    string ReasonCode,
    long? ExpectedSourceVersion,
    string CorrelationId) : IRejectionEvent;
