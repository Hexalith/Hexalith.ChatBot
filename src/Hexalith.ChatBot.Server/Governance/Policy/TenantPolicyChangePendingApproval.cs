using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Policy;

public sealed record TenantPolicyChangePendingApproval(
    string PolicyChangeId,
    string TenantId,
    string SourcePolicySnapshotId,
    string ProposedPolicySnapshotId,
    IReadOnlyList<string> ChangedKnobIds,
    TenantPolicyChangeSet ChangeSet,
    string RequesterActorId,
    string RequesterRef,
    string ReasonCode,
    string OldValueFingerprint,
    string NewValueFingerprint,
    DateTimeOffset RequestedAtUtc,
    long SourceVersion,
    string CorrelationId,
    string SchemaVersion = "chatbot.tenant-policy-change-pending-approval.v1") : IEventPayload;
