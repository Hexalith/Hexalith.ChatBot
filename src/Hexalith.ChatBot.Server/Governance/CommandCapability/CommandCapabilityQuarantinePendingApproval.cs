using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.CommandCapability;

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
