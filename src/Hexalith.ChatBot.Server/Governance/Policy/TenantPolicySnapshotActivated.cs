using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Policy;

public sealed record TenantPolicySnapshotActivated(
    string PolicyChangeId,
    string TenantId,
    string SupersededPolicySnapshotId,
    string ActivatedPolicySnapshotId,
    IReadOnlyList<string> ChangedKnobIds,
    TenantPolicyChangeSet? ChangeSet,
    TenantPolicyApprovalStatus ApprovalStatus,
    string RequesterRef,
    string? ApproverRef,
    string ReasonCode,
    DateTimeOffset ActivatedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.tenant-policy-snapshot-activated.v1") : IEventPayload;
