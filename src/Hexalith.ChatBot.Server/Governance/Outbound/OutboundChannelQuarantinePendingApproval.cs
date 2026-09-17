using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

/// <summary>
/// FR74/FR75d outbound-channel quarantine events. Mirrors the <see cref="OutboundChannelDisablePendingApproval"/> →
/// <see cref="OutboundChannelDisabled"/> disable triplet (Story 7.24), substituting <c>Quarantine</c>/<c>Quarantined</c>
/// for <c>Disable</c>/<c>Disabled</c> (the Story 7.22 disable→quarantine substitution): a first-person proposal records
/// a pending approval keyed by the quarantine-change id; a distinct second human policy-admin activates the durable
/// <see cref="OutboundChannelQuarantined"/> control-state event. Carries safe, metadata-only tokens only — never
/// recipient/sender addresses, message content, credentials, OAuth grant fingerprints, model prompts/completions, or
/// delegated-user PII. The subject is the safe outbound-channel ref (<c>OutboundChannelRef</c> — the <c>AdapterRef</c>
/// token).
/// </summary>
public sealed record OutboundChannelQuarantinePendingApproval(
    string QuarantineChangeId,
    string TenantId,
    string OutboundChannelRef,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string PolicySnapshotId,
    OutboundChannelControlState OldState,
    OutboundChannelControlState NewState,
    DateTimeOffset RequestedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.outbound-channel-quarantine-pending-approval.v1") : IEventPayload;
