using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiActor;

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
