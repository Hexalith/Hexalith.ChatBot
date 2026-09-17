using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.ServiceClient;

/// <summary>
/// The activated FR74 control-state event recorded when a distinct second human tenant-admin approves the
/// quarantine. Carries the actor (approver), scope (tenant-admin), subject (safe service-client ref), reason,
/// old/new state, policy-snapshot id, and timestamp. Quarantine affects only future admission; existing records
/// stay auditable.
/// </summary>
public sealed record ServiceClientQuarantined(
    string QuarantineChangeId,
    string TenantId,
    string ServiceClientRef,
    string RequesterRef,
    string ApproverRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    DateTimeOffset QuarantinedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.service-client-quarantined.v1") : IEventPayload;
