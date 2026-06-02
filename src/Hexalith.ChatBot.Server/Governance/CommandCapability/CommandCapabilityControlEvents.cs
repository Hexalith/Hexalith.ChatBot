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
