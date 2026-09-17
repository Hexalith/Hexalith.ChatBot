using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.ServiceClient;

/// <summary>
/// The activated FR74 control-state event recorded when a distinct second human tenant-admin approves the
/// disable. Carries the actor (approver), scope (tenant-admin), subject (safe service-client ref), reason,
/// old/new state, policy-snapshot id, and timestamp. Disable affects only future admission; existing records
/// stay auditable.
/// </summary>
public sealed record ServiceClientDisabled(
    string DisableChangeId,
    string TenantId,
    string ServiceClientRef,
    string RequesterRef,
    string ApproverRef,
    string ReasonCode,
    string PolicySnapshotId,
    ServiceClientControlState OldState,
    ServiceClientControlState NewState,
    DateTimeOffset DisabledAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.service-client-disabled.v1") : IEventPayload;
