using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

public sealed record OutboundApprovalDecisionRecorded(
    string ApprovalId,
    string DraftId,
    string ProjectId,
    ApprovalDecisionKind DecisionKind,
    string DecisionActorId,
    string DecisionActorType,
    DateTimeOffset DecidedAtUtc,
    long ExpectedApprovalSourceVersion,
    string AuthorityResult,
    string? DisabledReason,
    string DecisionRationaleRedactionState,
    string AuditOperationId,
    string AuditStatus,
    string PolicySnapshotId,
    string SafeNextAction,
    OutboundApprovalContentSnapshot ContentSnapshot,
    long SourceVersion,
    string CorrelationId,
    string RedactionState = "metadata_only",
    string RetentionClass = "collaboration_input",
    string SchemaVersion = "chatbot.outbound-approval-decision-recorded.v1") : IEventPayload;
