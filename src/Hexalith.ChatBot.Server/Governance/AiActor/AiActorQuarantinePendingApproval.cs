using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.AiActor;

/// <summary>
/// FR74/FR75d AI-actor quarantine events. Mirrors the disable triplet above (and the Story 7.16 service-client
/// quarantine pattern): a first-person proposal records a pending approval keyed by the quarantine-change id; a
/// distinct second human policy-admin activates the durable <see cref="AiActorQuarantined"/> control-state
/// event. Carries safe, metadata-only tokens only — never service-client/AI credentials, OAuth grant
/// fingerprints, model prompts/completions, or delegated-user PII.
/// </summary>
public sealed record AiActorQuarantinePendingApproval(
    string QuarantineChangeId,
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
    string SchemaVersion = "chatbot.ai-actor-quarantine-pending-approval.v1") : IEventPayload;
