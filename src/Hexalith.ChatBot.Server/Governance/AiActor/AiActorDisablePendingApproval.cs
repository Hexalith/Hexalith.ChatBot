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
