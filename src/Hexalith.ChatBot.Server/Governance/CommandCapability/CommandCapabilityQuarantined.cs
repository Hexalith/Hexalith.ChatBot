using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.CommandCapability;

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
